using Mirage.Core.Diff;
using Mirage.Core.Diagnostics;
using Mirage.Core.Providers;
using Mirage.Core.Semantics;
using Mirage.Web.Dom;
using Mirage.Web.Maps;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mirage.Web.Runtime;

public sealed class MirageRuntime : IMirageRuntime
{
    private const string ValueElementMarker = "data-mirage-value-element";
    private const int MapProviderInitializationRetryLimit = 3;
    private const int MapProviderInitializationRetryDelayMilliseconds = 250;

    private readonly DomElement _mountRoot;
    private readonly IMirageComponentProvider _provider;
    private readonly IMirageMapProvider? _mapProvider;
    private readonly SemanticDiffEngine _diffEngine = new();
    private readonly Dictionary<string, RealizedNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<(string NodeId, string EventName), int> _eventSubscriptions = new();
    private readonly Dictionary<string, int> _mapProviderInitializationRetries = new(StringComparer.Ordinal);

    private NodeBase? _currentRoot;
    private IMirageEventSink? _eventSink;

    public MirageRuntime(DomElement mountRoot, IMirageComponentProvider provider, IMirageMapProvider? mapProvider = null)
    {
        _mountRoot = mountRoot;
        _provider = provider;
        _mapProvider = mapProvider;

        _mountRoot.Style("position", "relative");
        _mountRoot.Style("overflow", "hidden");
        _mountRoot.Style("width", "100%");
        _mountRoot.Style("height", "100%");
    }

    public void SetEventSink(IMirageEventSink? eventSink)
    {
        _eventSink = eventSink;
    }

    public void Render(NodeBase nextRoot)
    {
        ArgumentNullException.ThrowIfNull(nextRoot);

        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var renderStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var diffStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var operations = _diffEngine.Diff(_currentRoot, nextRoot);
        var diffEndTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var applyStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        foreach (var operation in operations)
        {
            Apply(operation);
        }
        var applyEndTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;

        _currentRoot = nextRoot;

        if (diagnosticsEnabled)
        {
            var totalMs = MirageDiagnostics.GetElapsedMilliseconds(renderStartTimestamp, MirageDiagnostics.GetTimestamp());
            var diffMs = MirageDiagnostics.GetElapsedMilliseconds(diffStartTimestamp, diffEndTimestamp);
            var applyMs = MirageDiagnostics.GetElapsedMilliseconds(applyStartTimestamp, applyEndTimestamp);
            MirageDiagnostics.Log(
                "runtime.render",
                $"root={nextRoot.NodeId} operations={operations.Count} diffMs={diffMs:0.###} applyMs={applyMs:0.###} totalMs={totalMs:0.###}");
        }
    }

    public void SetViewport(double width, double height)
    {
        if (_currentRoot is null || !_nodes.TryGetValue(_currentRoot.NodeId, out var root))
        {
            return;
        }

        root.Element.Style("width", $"{Math.Max(0, width):0.###}px");
        root.Element.Style("height", $"{Math.Max(0, height):0.###}px");
    }

    public string? TryGetNodeInputValue(string nodeId)
    {
        return _nodes.TryGetValue(nodeId, out var node)
            ? node.GetValueElement().GetInputValue()
            : null;
    }

    public bool TryGetNodeScrollPosition(string nodeId, out double left, out double top)
    {
        left = 0;
        top = 0;

        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            return false;
        }

        var position = node.Element.GetScrollPosition();
        left = position.Left;
        top = position.Top;
        return true;
    }

    public bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop)
    {
        actualLeft = 0;
        actualTop = 0;

        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            return false;
        }

        node.Element.SetScrollPosition(left, top);
        var position = node.Element.GetScrollPosition();
        actualLeft = position.Left;
        actualTop = position.Top;
        return true;
    }

    public void Dispose()
    {
        foreach (var subscriptionId in _eventSubscriptions.Values.ToArray())
        {
            DomElement.Off(subscriptionId);
        }

        _eventSubscriptions.Clear();
        _mapProviderInitializationRetries.Clear();

        foreach (var node in _nodes.Values.ToArray())
        {
            node.DisposeMapAdapter();
            node.Element.Release();
        }

        _nodes.Clear();
        _currentRoot = null;
        _eventSink = null;

        _mountRoot.ClearChildren();
    }

    private void Apply(PatchOperation operation)
    {
        switch (operation)
        {
            case CreateNodeOperation create:
                ApplyCreate(create);
                break;
            case MoveNodeOperation move:
                ApplyMove(move);
                break;
            case RemoveNodeOperation remove:
                ApplyRemove(remove);
                break;
            case SetPropertyOperation setProperty:
                ApplySetProperty(setProperty);
                break;
            case RemovePropertyOperation removeProperty:
                ApplyRemoveProperty(removeProperty);
                break;
            case SetAttributeOperation setAttribute:
                ApplySetAttribute(setAttribute);
                break;
            case RemoveAttributeOperation removeAttribute:
                ApplyRemoveAttribute(removeAttribute);
                break;
            case AddClassOperation addClass:
                ApplyAddClass(addClass);
                break;
            case RemoveClassOperation removeClass:
                ApplyRemoveClass(removeClass);
                break;
            case SetStyleOperation setStyle:
                ApplySetStyle(setStyle);
                break;
            case AttachEventOperation attachEvent:
                ApplyAttachEvent(attachEvent);
                break;
            case DetachEventOperation detachEvent:
                ApplyDetachEvent(detachEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported patch operation: {operation.GetType().Name}");
        }
    }

    private void ApplyCreate(CreateNodeOperation operation)
    {
        var realized = (operation.CreationKind switch
        {
            NodeCreationKind.Control => CreateControlNode(operation),
            NodeCreationKind.Web => CreateWebNode(operation),
            _ => throw new InvalidOperationException($"Unsupported creation kind: {operation.CreationKind}")
        }).WithNodeId(operation.NodeId);

        _nodes[operation.NodeId] = realized;

        if (operation.ParentId is null)
        {
            _mountRoot.ClearChildren();
            _mountRoot.Append(realized.Element);
        }
        else
        {
            if (!_nodes.TryGetValue(operation.ParentId, out var parent))
            {
                throw new InvalidOperationException($"Cannot create node '{operation.NodeId}' because parent '{operation.ParentId}' was not found.");
            }

            parent.Element.Insert(operation.Index, realized.Element);
        }

        if (realized.TryGetControlDescriptor(out var controlDescriptor)
            && controlDescriptor.Kind == ComponentKind.Map)
        {
            ConfigureMapNode(operation.NodeId, realized);
        }
    }

    private RealizedNode CreateControlNode(CreateNodeOperation operation)
    {
        if (operation.ControlKind is not ComponentKind kind)
        {
            throw new InvalidOperationException($"CreateNode '{operation.NodeId}' is control node but missing ComponentKind.");
        }

        if (!_provider.Capabilities.SupportsControlNodes)
        {
            throw new InvalidOperationException("Provider capabilities do not support control-node realization.");
        }

        var descriptor = _provider.ResolveControl(kind);
        var element = DomElement.Create(descriptor.ElementTagName);
        element.Attr("data-mirage-node-id", operation.NodeId);
        element.Attr("data-mirage-node-type", "control");
        element.Attr("data-mirage-kind", kind.ToString());
        element.Style("boxSizing", "border-box");

        foreach (var (name, value) in descriptor.DefaultAttributes)
        {
            element.Attr(name, value);
        }

        DomElement? valueElement = null;
        if (!string.IsNullOrWhiteSpace(descriptor.ValueElementTagName))
        {
            valueElement = DomElement.Create(descriptor.ValueElementTagName!);
            valueElement.Attr(ValueElementMarker, "true");
            if (string.Equals(descriptor.ValueElementTagName, "button", StringComparison.OrdinalIgnoreCase))
            {
                valueElement.Attr("type", "button");
            }
            valueElement.Style("boxSizing", "border-box");
            if (descriptor.StretchValueElementWidth)
            {
                valueElement.Style("width", "100%");
            }
        }

        DomElement? secondaryElement = null;
        if (!string.IsNullOrWhiteSpace(descriptor.SecondaryElementTagName))
        {
            secondaryElement = DomElement.Create(descriptor.SecondaryElementTagName!);
            secondaryElement.Attr("data-mirage-secondary-element", "true");
            if (string.Equals(descriptor.SecondaryElementTagName, "button", StringComparison.OrdinalIgnoreCase))
            {
                secondaryElement.Attr("type", "button");
            }
            secondaryElement.Style("boxSizing", "border-box");
        }

        if (secondaryElement is not null)
        {
            element.Append(secondaryElement);
        }

        if (valueElement is not null)
        {
            element.Append(valueElement);
        }

        return RealizedNode.ForControl(element, descriptor, valueElement, secondaryElement);
    }

    private RealizedNode CreateWebNode(CreateNodeOperation operation)
    {
        if (string.IsNullOrWhiteSpace(operation.ElementName))
        {
            throw new InvalidOperationException($"CreateNode '{operation.NodeId}' is web node but missing ElementName.");
        }

        if (!_provider.Capabilities.SupportsWebNodes)
        {
            throw new InvalidOperationException("Provider capabilities do not support web-node realization.");
        }

        var requestedElementName = operation.ElementName!;
        var descriptor = _provider.ResolveWebNode(requestedElementName);
        if (!string.Equals(descriptor.ElementName, requestedElementName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Provider returned element descriptor '{descriptor.ElementName}' for requested '{requestedElementName}'. Providers must preserve web-node element identity.");
        }

        var element = DomElement.Create(descriptor.ElementName);
        element.Attr("data-mirage-node-id", operation.NodeId);
        element.Attr("data-mirage-node-type", "web");
        element.Attr("data-mirage-element-name", descriptor.ElementName);
        element.Style("boxSizing", "border-box");

        foreach (var (name, value) in descriptor.DefaultAttributes)
        {
            element.Attr(name, value);
        }

        return RealizedNode.ForWeb(element, descriptor);
    }

    private void ApplyMove(MoveNodeOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node)
            || !_nodes.TryGetValue(operation.ParentId, out var parent))
        {
            return;
        }

        parent.Element.Insert(operation.Index, node.Element);
    }

    private void ApplyRemove(RemoveNodeOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        if (operation.ParentId is null)
        {
            _mountRoot.ClearChildren();
        }
        else if (_nodes.TryGetValue(operation.ParentId, out var parent))
        {
            try
            {
                parent.Element.Remove(node.Element);
            }
            catch
            {
                // Ignore detached-node removal races.
            }
        }

        RemoveEventSubscriptionsForNode(operation.NodeId);
        _mapProviderInitializationRetries.Remove(operation.NodeId);
        node.DisposeMapAdapter();
        node.Element.Release();
        _nodes.Remove(operation.NodeId);
    }

    private void ApplySetProperty(SetPropertyOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        if (node.ApplyMapProperty(operation.PropertyName, operation.Value))
        {
            ApplyMapState(node);
        }

        if (node.TryGetControlDescriptor(out var controlDescriptor))
        {
            if (!controlDescriptor.PropertyMappings.TryGetValue(operation.PropertyName, out var mapping))
            {
                return;
            }

            var propertyTargetElement = node.GetElement(mapping.ElementTarget);

            switch (mapping.Target)
            {
                case PropertyTarget.TextContent:
                    propertyTargetElement.Text(operation.Value.GetStringOrNull());
                    break;
                case PropertyTarget.Attribute:
                {
                    var attributeName = string.IsNullOrWhiteSpace(mapping.Name) ? operation.PropertyName : mapping.Name;
                    propertyTargetElement.Attr(attributeName!, operation.Value.GetStringOrNull());
                    break;
                }
                case PropertyTarget.InputValue:
                    propertyTargetElement.SetInputValue(operation.Value.GetStringOrNull());
                    break;
                case PropertyTarget.InputChecked:
                {
                    var value = operation.Value.GetStringOrNull();
                    propertyTargetElement.SetInputChecked(string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
                    break;
                }
                case PropertyTarget.Style:
                {
                    var styleName = string.IsNullOrWhiteSpace(mapping.Name) ? operation.PropertyName : mapping.Name;
                    propertyTargetElement.Style(styleName!, operation.Value.GetStringOrNull());
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unsupported property target: {mapping.Target}");
            }

            return;
        }

        if (!node.TryGetWebDescriptor(out var webDescriptor))
        {
            return;
        }

        if (string.Equals(operation.PropertyName, "AutomationId", StringComparison.Ordinal))
        {
            node.Element.Attr("data-automation-id", operation.Value.GetStringOrNull());
            return;
        }

        if (!webDescriptor.SupportsProperties)
        {
            throw new InvalidOperationException($"Provider does not support web-node properties for node '{operation.NodeId}'.");
        }

        node.Element.SetProperty(operation.PropertyName, operation.Value);
    }

    private void ApplyRemoveProperty(RemovePropertyOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        if (node.RemoveMapProperty(operation.PropertyName))
        {
            ApplyMapState(node);
        }

        if (node.TryGetControlDescriptor(out var controlDescriptor))
        {
            if (!controlDescriptor.PropertyMappings.TryGetValue(operation.PropertyName, out var mapping))
            {
                return;
            }

            var propertyTargetElement = node.GetElement(mapping.ElementTarget);

            switch (mapping.Target)
            {
                case PropertyTarget.TextContent:
                    propertyTargetElement.Text(null);
                    break;
                case PropertyTarget.Attribute:
                {
                    var attributeName = string.IsNullOrWhiteSpace(mapping.Name) ? operation.PropertyName : mapping.Name;
                    propertyTargetElement.Attr(attributeName!, null);
                    break;
                }
                case PropertyTarget.InputValue:
                    propertyTargetElement.SetInputValue(null);
                    break;
                case PropertyTarget.InputChecked:
                    propertyTargetElement.SetInputChecked(false);
                    break;
                case PropertyTarget.Style:
                {
                    var styleName = string.IsNullOrWhiteSpace(mapping.Name) ? operation.PropertyName : mapping.Name;
                    propertyTargetElement.Style(styleName!, null);
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unsupported property target: {mapping.Target}");
            }

            return;
        }

        if (!node.TryGetWebDescriptor(out var webDescriptor))
        {
            return;
        }

        if (string.Equals(operation.PropertyName, "AutomationId", StringComparison.Ordinal))
        {
            node.Element.Attr("data-automation-id", null);
            return;
        }

        if (!webDescriptor.SupportsProperties)
        {
            throw new InvalidOperationException($"Provider does not support web-node properties for node '{operation.NodeId}'.");
        }

        node.Element.RemoveProperty(operation.PropertyName);
    }

    private void ApplySetAttribute(SetAttributeOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node)
            || !node.TryGetWebDescriptor(out var descriptor))
        {
            return;
        }

        if (!descriptor.SupportsAttributes)
        {
            throw new InvalidOperationException($"Provider does not support web-node attributes for node '{operation.NodeId}'.");
        }

        node.Element.Attr(operation.AttributeName, operation.Value);
    }

    private void ApplyRemoveAttribute(RemoveAttributeOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node)
            || !node.TryGetWebDescriptor(out var descriptor))
        {
            return;
        }

        if (!descriptor.SupportsAttributes)
        {
            throw new InvalidOperationException($"Provider does not support web-node attributes for node '{operation.NodeId}'.");
        }

        node.Element.Attr(operation.AttributeName, null);
    }

    private void ApplyAddClass(AddClassOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node)
            || !node.TryGetWebDescriptor(out var descriptor))
        {
            return;
        }

        if (!descriptor.SupportsClasses)
        {
            throw new InvalidOperationException($"Provider does not support web-node class mutation for node '{operation.NodeId}'.");
        }

        node.Element.AddClass(operation.ClassName);
    }

    private void ApplyRemoveClass(RemoveClassOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node)
            || !node.TryGetWebDescriptor(out var descriptor))
        {
            return;
        }

        if (!descriptor.SupportsClasses)
        {
            throw new InvalidOperationException($"Provider does not support web-node class mutation for node '{operation.NodeId}'.");
        }

        node.Element.RemoveClass(operation.ClassName);
    }

    private void ApplySetStyle(SetStyleOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        if (node.TryGetWebDescriptor(out var descriptor) && !descriptor.SupportsStyles)
        {
            throw new InvalidOperationException($"Provider does not support web-node styles for node '{operation.NodeId}'.");
        }

        node.Element.Style(operation.StyleName, operation.Value);
    }

    private void ApplyAttachEvent(AttachEventOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        EnsureEventSubscription(operation.NodeId, operation.EventName, node);
    }

    private void EnsureEventSubscription(string nodeId, string eventName, RealizedNode node)
    {
        if (_eventSubscriptions.ContainsKey((nodeId, eventName)))
        {
            return;
        }

        var eventDescriptor = ResolveEventDescriptor(node, eventName);
        var targetElement = eventDescriptor.Target switch
        {
            EventTarget.ValueElement => node.GetValueElement(),
            EventTarget.SecondaryElement => node.GetSecondaryElement(),
            _ => node.Element
        };

        int subscriptionId;
        if (eventDescriptor.PayloadKind == EventPayloadKind.InputValue)
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, () =>
            {
                var value = targetElement.GetInputValue();
                _eventSink?.OnEvent(nodeId, eventName, value);
            });
        }
        else if (eventDescriptor.PayloadKind == EventPayloadKind.InputChecked)
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, () =>
            {
                _eventSink?.OnEvent(
                    nodeId,
                    eventName,
                    targetElement.GetInputChecked() ? "true" : "false");
            });
        }
        else if (eventDescriptor.PayloadKind == EventPayloadKind.SourceAttribute)
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, "source-attribute", payload =>
            {
                _eventSink?.OnEvent(nodeId, eventName, payload);
            });
        }
        else
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, () =>
            {
                _eventSink?.OnEvent(nodeId, eventName, null);
            });
        }

        _eventSubscriptions[(nodeId, eventName)] = subscriptionId;
    }

    private void ConfigureMapNode(string nodeId, RealizedNode node)
    {
        if (_mapProvider is null)
        {
            var message = "Mirage map provider not registered. Add builder.UseMirageLeafletMaps(...).";
            ShowMapFailure(node, "provider-not-registered", message);
            MirageDiagnostics.Log("map.provider", $"node={nodeId} code=provider-not-registered message={message}");
            return;
        }

        var initialization = _mapProvider.Initialization;
        if (!initialization.IsReady)
        {
            // Always re-check provider readiness here so transient startup failures can recover
            // when maps are realized after the first warm-up attempt.
            node.Element.Attr("data-mirage-map-status", "pending");
            _ = ConfigureMapNodeWhenProviderReadyAsync(nodeId, node);
            return;
        }

        _mapProviderInitializationRetries.Remove(nodeId);
        ConfigureMapNodeCore(nodeId, node);
    }

    private async Task ConfigureMapNodeWhenProviderReadyAsync(string nodeId, RealizedNode expectedNode)
    {
        var mapProvider = _mapProvider;
        if (mapProvider is null)
        {
            return;
        }

        try
        {
            var initialization = await mapProvider.EnsureInitializedAsync();
            if (!_nodes.TryGetValue(nodeId, out var currentNode)
                || !ReferenceEquals(currentNode, expectedNode)
                || currentNode.TryGetMapAdapter(out _))
            {
                return;
            }

            if (!initialization.IsReady)
            {
                var code = string.IsNullOrWhiteSpace(initialization.ErrorCode)
                    ? "provider-not-ready"
                    : initialization.ErrorCode!;
                var message = string.IsNullOrWhiteSpace(initialization.ErrorMessage)
                    ? "Map provider initialization failed."
                    : initialization.ErrorMessage!;

                if (ShouldRetryMapProviderInitializationFailure(code)
                    && TryScheduleMapProviderInitializationRetry(nodeId, currentNode, mapProvider.ProviderName, code, message))
                {
                    return;
                }

                ShowMapFailure(currentNode, code, message);
                MirageDiagnostics.Log("map.provider", $"node={nodeId} provider={mapProvider.ProviderName} code={code} message={message}");
                return;
            }

            _mapProviderInitializationRetries.Remove(nodeId);
            ConfigureMapNodeCore(nodeId, currentNode);
        }
        catch (Exception error)
        {
            if (!_nodes.TryGetValue(nodeId, out var currentNode)
                || !ReferenceEquals(currentNode, expectedNode)
                || currentNode.TryGetMapAdapter(out _))
            {
                return;
            }

            var message = string.IsNullOrWhiteSpace(error.Message)
                ? "Map provider initialization failed."
                : error.Message;
            _mapProviderInitializationRetries.Remove(nodeId);
            ShowMapFailure(currentNode, "provider-init-exception", message);
            MirageDiagnostics.Log(
                "map.provider",
                $"node={nodeId} provider={mapProvider.ProviderName} code=provider-init-exception message={message}");
        }
    }

    private bool TryScheduleMapProviderInitializationRetry(
        string nodeId,
        RealizedNode node,
        string providerName,
        string errorCode,
        string message)
    {
        var retryAttempt = _mapProviderInitializationRetries.TryGetValue(nodeId, out var existingAttempt)
            ? existingAttempt + 1
            : 1;
        _mapProviderInitializationRetries[nodeId] = retryAttempt;

        if (retryAttempt > MapProviderInitializationRetryLimit)
        {
            _mapProviderInitializationRetries.Remove(nodeId);
            return false;
        }

        var delayMs = MapProviderInitializationRetryDelayMilliseconds * retryAttempt;
        node.Element.Attr("data-mirage-map-status", "pending");
        MirageDiagnostics.Log(
            "map.provider",
            $"node={nodeId} provider={providerName} code={errorCode} retryAttempt={retryAttempt} retryDelayMs={delayMs} message={message}");
        _ = ConfigureMapNodeWhenProviderReadyAfterDelayAsync(nodeId, node, delayMs);
        return true;
    }

    private static bool ShouldRetryMapProviderInitializationFailure(string errorCode)
    {
        return errorCode switch
        {
            "provider-not-ready" => true,
            "not-initialized" => true,
            "css-timeout" => true,
            "js-timeout" => true,
            "leaflet-global-missing" => true,
            "readiness-exception" => true,
            "initialization-exception" => true,
            _ => false
        };
    }

    private async Task ConfigureMapNodeWhenProviderReadyAfterDelayAsync(string nodeId, RealizedNode expectedNode, int delayMs)
    {
        await Task.Delay(delayMs);
        if (!_nodes.TryGetValue(nodeId, out var currentNode)
            || !ReferenceEquals(currentNode, expectedNode)
            || currentNode.TryGetMapAdapter(out _))
        {
            return;
        }

        currentNode.Element.Attr("data-mirage-map-status", "pending");
        await ConfigureMapNodeWhenProviderReadyAsync(nodeId, currentNode);
    }

    private void ConfigureMapNodeCore(string nodeId, RealizedNode node)
    {
        if (_mapProvider is null)
        {
            return;
        }

        try
        {
            if (node.TryGetControlDescriptor(out var descriptor))
            {
                // Map providers can emit initial runtime events (for example the first region) during adapter initialization.
                // Ensure subscriptions are active before provider state is applied so those events are not dropped.
                foreach (var eventName in descriptor.EventMappings.Keys)
                {
                    EnsureEventSubscription(nodeId, eventName, node);
                }
            }

            var adapter = _mapProvider.CreateAdapter(nodeId, node.Element);
            node.AttachMapAdapter(adapter);
            node.Element.Attr("data-mirage-map-provider", _mapProvider.ProviderName);
            // Ensure map adapters initialize even when first projected state is all-default.
            ApplyMapState(node);
        }
        catch (Exception error)
        {
            ShowMapFailure(node, "adapter-create-failed", error.Message);
            MirageDiagnostics.Log(
                "map.provider",
                $"node={nodeId} provider={_mapProvider.ProviderName} code=adapter-create-failed message={error.Message}");
        }
    }

    private static void ApplyMapState(RealizedNode node)
    {
        if (!node.TryGetMapAdapter(out var adapter))
        {
            return;
        }

        try
        {
            adapter.ApplyState(node.MapState);
        }
        catch (Exception error)
        {
            var message = $"Map apply failed: {error.Message}";
            ShowMapFailure(node, "adapter-apply-failed", message);
            try
            {
                adapter.ShowError("adapter-apply-failed", message);
            }
            catch (Exception showErrorException)
            {
                MirageDiagnostics.Log(
                    "map.provider",
                    $"node={node.NodeId} code=adapter-show-error-failed message={showErrorException.Message}");
            }

            MirageDiagnostics.Log("map.provider", $"node={node.NodeId} code=adapter-apply-failed message={error.Message}");
        }
    }

    private static void ShowMapFailure(RealizedNode node, string errorCode, string message)
    {
        node.Element.Attr("data-mirage-map-status", "error");
        node.Element.Attr("data-mirage-map-error-code", errorCode);
        node.Element.Attr("data-mirage-map-error", message);
    }

    private ProviderEventDescriptor ResolveEventDescriptor(RealizedNode node, string eventName)
    {
        if (node.TryGetControlDescriptor(out var controlDescriptor))
        {
            return controlDescriptor.EventMappings.TryGetValue(eventName, out var mapped)
                ? mapped
                : new ProviderEventDescriptor(eventName);
        }

        if (node.TryGetWebDescriptor(out var webDescriptor))
        {
            if (!webDescriptor.SupportsEvents)
            {
                throw new InvalidOperationException($"Provider does not support web-node events for node '{node.NodeId}'.");
            }

            return webDescriptor.EventMappings.TryGetValue(eventName, out var mapped)
                ? mapped
                : new ProviderEventDescriptor(eventName);
        }

        return new ProviderEventDescriptor(eventName);
    }

    private void ApplyDetachEvent(DetachEventOperation operation)
    {
        if (_eventSubscriptions.Remove((operation.NodeId, operation.EventName), out var subscriptionId))
        {
            DomElement.Off(subscriptionId);
        }
    }

    private void RemoveEventSubscriptionsForNode(string nodeId)
    {
        var keys = _eventSubscriptions.Keys
            .Where(key => string.Equals(key.NodeId, nodeId, StringComparison.Ordinal))
            .ToArray();

        foreach (var key in keys)
        {
            if (_eventSubscriptions.Remove(key, out var subscriptionId))
            {
                DomElement.Off(subscriptionId);
            }
        }
    }

    private sealed class RealizedNode
    {
        private readonly MapSemanticProjection _mapProjection = new();
        private IMirageMapAdapter? _mapAdapter;

        private RealizedNode(
            string nodeId,
            DomElement element,
            ControlComponentDescriptor? controlDescriptor,
            WebComponentDescriptor? webDescriptor,
            DomElement? valueElement,
            DomElement? secondaryElement)
        {
            NodeId = nodeId;
            Element = element;
            ControlDescriptor = controlDescriptor;
            WebDescriptor = webDescriptor;
            ValueElement = valueElement;
            SecondaryElement = secondaryElement;
        }

        public string NodeId { get; private set; } = string.Empty;

        public DomElement Element { get; }

        public DomElement? ValueElement { get; }

        public DomElement? SecondaryElement { get; }

        private ControlComponentDescriptor? ControlDescriptor { get; }

        private WebComponentDescriptor? WebDescriptor { get; }

        public MirageMapSemanticState MapState => _mapProjection.State;

        public static RealizedNode ForControl(
            DomElement element,
            ControlComponentDescriptor descriptor,
            DomElement? valueElement,
            DomElement? secondaryElement)
            => new(nodeId: string.Empty, element, descriptor, webDescriptor: null, valueElement, secondaryElement);

        public static RealizedNode ForWeb(DomElement element, WebComponentDescriptor descriptor)
            => new(nodeId: string.Empty, element, controlDescriptor: null, descriptor, valueElement: null, secondaryElement: null);

        public bool TryGetControlDescriptor(out ControlComponentDescriptor descriptor)
        {
            if (ControlDescriptor is not null)
            {
                descriptor = ControlDescriptor;
                return true;
            }

            descriptor = null!;
            return false;
        }

        public bool TryGetWebDescriptor(out WebComponentDescriptor descriptor)
        {
            if (WebDescriptor is not null)
            {
                descriptor = WebDescriptor;
                return true;
            }

            descriptor = null!;
            return false;
        }

        public void AttachMapAdapter(IMirageMapAdapter adapter)
        {
            _mapAdapter = adapter;
        }

        public bool TryGetMapAdapter(out IMirageMapAdapter adapter)
        {
            if (_mapAdapter is not null)
            {
                adapter = _mapAdapter;
                return true;
            }

            adapter = null!;
            return false;
        }

        public void DisposeMapAdapter()
        {
            try
            {
                _mapAdapter?.Dispose();
            }
            catch
            {
                // best effort cleanup
            }
            finally
            {
                _mapAdapter = null;
            }
        }

        public bool ApplyMapProperty(string propertyName, MirageValue value)
        {
            return TryGetControlDescriptor(out var descriptor)
                && descriptor.Kind == ComponentKind.Map
                && _mapProjection.ApplySet(propertyName, value);
        }

        public bool RemoveMapProperty(string propertyName)
        {
            return TryGetControlDescriptor(out var descriptor)
                && descriptor.Kind == ComponentKind.Map
                && _mapProjection.ApplyRemove(propertyName);
        }

        public DomElement GetValueElement()
        {
            return ValueElement ?? Element;
        }

        public DomElement GetSecondaryElement()
        {
            return SecondaryElement ?? Element;
        }

        public DomElement GetElement(PropertyElementTarget target)
        {
            return target switch
            {
                PropertyElementTarget.ValueElement => GetValueElement(),
                PropertyElementTarget.SecondaryElement => GetSecondaryElement(),
                _ => Element
            };
        }

        public RealizedNode WithNodeId(string nodeId)
        {
            NodeId = nodeId;
            return this;
        }
    }

    private sealed class MapSemanticProjection
    {
        private MirageMapSemanticState _state = MirageMapSemanticState.Empty;

        public MirageMapSemanticState State => _state;

        public bool ApplySet(string propertyName, MirageValue value)
        {
            return propertyName switch
            {
                MirageMapPropertyNames.MapType => ApplyMapType(value.GetStringOrNull()),
                MirageMapPropertyNames.IsShowingUser => ApplyIsShowingUser(value),
                MirageMapPropertyNames.IsScrollEnabled => ApplyIsScrollEnabled(value),
                MirageMapPropertyNames.IsZoomEnabled => ApplyIsZoomEnabled(value),
                MirageMapPropertyNames.IsTrafficEnabled => ApplyIsTrafficEnabled(value),
                MirageMapPropertyNames.RegionRequestToken => ApplyRegionRequestToken(value.GetStringOrNull()),
                MirageMapPropertyNames.Region => ApplyRegion(value),
                MirageMapPropertyNames.Pins => ApplyPins(value),
                MirageMapPropertyNames.Elements => ApplyElements(value),
                MirageMapPropertyNames.DismissPopupKey => ApplyDismissPopupKey(value.GetStringOrNull()),
                MirageMapPropertyNames.DismissPopupRequestToken => ApplyDismissPopupRequestToken(value.GetStringOrNull()),
                _ => false
            };
        }

        public bool ApplyRemove(string propertyName)
        {
            return propertyName switch
            {
                MirageMapPropertyNames.MapType => ReplaceState(_state with { MapType = MirageMapType.Street }),
                MirageMapPropertyNames.IsShowingUser => ReplaceState(_state with { IsShowingUser = false }),
                MirageMapPropertyNames.IsScrollEnabled => ReplaceState(_state with { IsScrollEnabled = true }),
                MirageMapPropertyNames.IsZoomEnabled => ReplaceState(_state with { IsZoomEnabled = true }),
                MirageMapPropertyNames.IsTrafficEnabled => ReplaceState(_state with { IsTrafficEnabled = false }),
                MirageMapPropertyNames.RegionRequestToken => ReplaceState(_state with { RegionRequestToken = 0 }),
                MirageMapPropertyNames.Region => ReplaceState(_state with { Region = null }),
                MirageMapPropertyNames.Pins => ReplaceState(_state with { Pins = Array.Empty<MirageMapPin>() }),
                MirageMapPropertyNames.Elements => ReplaceState(_state with { Elements = Array.Empty<MirageMapElement>() }),
                MirageMapPropertyNames.DismissPopupKey => ReplaceState(_state with { DismissPopupKey = null }),
                MirageMapPropertyNames.DismissPopupRequestToken => ReplaceState(_state with { DismissPopupRequestToken = 0 }),
                _ => false
            };
        }

        private bool ApplyMapType(string? rawValue)
        {
            var next = MirageMapSemanticState.ParseMapType(rawValue);
            return ReplaceState(_state with { MapType = next });
        }

        private bool ApplyIsShowingUser(MirageValue value)
        {
            return ReplaceState(_state with { IsShowingUser = ReadBoolean(value, defaultValue: false) });
        }

        private bool ApplyIsScrollEnabled(MirageValue value)
        {
            return ReplaceState(_state with { IsScrollEnabled = ReadBoolean(value, defaultValue: true) });
        }

        private bool ApplyIsZoomEnabled(MirageValue value)
        {
            return ReplaceState(_state with { IsZoomEnabled = ReadBoolean(value, defaultValue: true) });
        }

        private bool ApplyIsTrafficEnabled(MirageValue value)
        {
            return ReplaceState(_state with { IsTrafficEnabled = ReadBoolean(value, defaultValue: false) });
        }

        private bool ApplyRegionRequestToken(string? rawValue)
        {
            var next = 0L;
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                _ = long.TryParse(rawValue, out next);
            }

            return ReplaceState(_state with { RegionRequestToken = next });
        }

        private bool ApplyRegion(MirageValue value)
        {
            JsonObject? regionJson = null;
            if (value.TryGetJson(out var regionNode) && regionNode is JsonObject jsonObject)
            {
                regionJson = jsonObject;
            }
            else
            {
                var raw = value.GetStringOrNull();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        regionJson = JsonNode.Parse(raw) as JsonObject;
                    }
                    catch
                    {
                        regionJson = null;
                    }
                }
            }

            if (regionJson is null)
            {
                return ReplaceState(_state with { Region = null });
            }

            var centerLatitude = TryGetNumber(regionJson, "centerLatitude");
            var centerLongitude = TryGetNumber(regionJson, "centerLongitude");
            var latitudeSpan = TryGetNumber(regionJson, "latitudeSpan");
            var longitudeSpan = TryGetNumber(regionJson, "longitudeSpan");
            if (!MirageMapRegion.TryCreate(
                    centerLatitude,
                    centerLongitude,
                    latitudeSpan,
                    longitudeSpan,
                    out var region))
            {
                return false;
            }

            return ReplaceState(_state with { Region = region });
        }

        private bool ApplyPins(MirageValue value)
        {
            JsonArray? pinsJson = null;
            if (value.TryGetJson(out var pinNode) && pinNode is JsonArray jsonArray)
            {
                pinsJson = jsonArray;
            }
            else
            {
                var raw = value.GetStringOrNull();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        pinsJson = JsonNode.Parse(raw) as JsonArray;
                    }
                    catch
                    {
                        pinsJson = null;
                    }
                }
            }

            if (pinsJson is null)
            {
                return ReplaceState(_state with { Pins = Array.Empty<MirageMapPin>() });
            }

            var pins = new List<MirageMapPin>(pinsJson.Count);
            foreach (var item in pinsJson)
            {
                if (item is not JsonObject pinJson)
                {
                    continue;
                }

                var key = TryGetString(pinJson, "key");
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var latitude = TryGetNumber(pinJson, "latitude");
                var longitude = TryGetNumber(pinJson, "longitude");
                if (!double.IsFinite(latitude) || !double.IsFinite(longitude))
                {
                    continue;
                }

                var label = TryGetString(pinJson, "label");
                var address = TryGetString(pinJson, "address");
                var type = TryGetString(pinJson, "type");
                pins.Add(new MirageMapPin(key, latitude, longitude, label, address, type));
            }

            var nextPins = pins.ToArray();
            return ReplaceState(_state with { Pins = nextPins });
        }

        private bool ApplyElements(MirageValue value)
        {
            JsonArray? elementsJson = null;
            if (value.TryGetJson(out var elementNode) && elementNode is JsonArray jsonArray)
            {
                elementsJson = jsonArray;
            }
            else
            {
                var raw = value.GetStringOrNull();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        elementsJson = JsonNode.Parse(raw) as JsonArray;
                    }
                    catch
                    {
                        elementsJson = null;
                    }
                }
            }

            if (elementsJson is null)
            {
                return ReplaceState(_state with { Elements = Array.Empty<MirageMapElement>() });
            }

            var elements = new List<MirageMapElement>(elementsJson.Count);
            foreach (var item in elementsJson)
            {
                if (item is not JsonObject elementJson)
                {
                    continue;
                }

                var key = TryGetString(elementJson, "key");
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var kind = ParseElementKind(TryGetString(elementJson, "kind"));
                if (kind == MirageMapElementKind.Unknown)
                {
                    continue;
                }

                var coordinates = ParseCoordinates(elementJson["coordinates"] as JsonArray);
                if (coordinates.Count == 0)
                {
                    continue;
                }

                var radiusMeters = TryGetOptionalNumber(elementJson, "radiusMeters");
                var strokeThickness = TryGetNumber(elementJson, "strokeThickness");
                if (!double.IsFinite(strokeThickness))
                {
                    strokeThickness = 0;
                }

                elements.Add(new MirageMapElement(
                    key,
                    kind,
                    coordinates.ToArray(),
                    radiusMeters,
                    TryGetString(elementJson, "strokeColor"),
                    strokeThickness,
                    TryGetString(elementJson, "fillColor")));
            }

            return ReplaceState(_state with { Elements = elements.ToArray() });
        }

        private bool ApplyDismissPopupKey(string? rawValue)
        {
            return ReplaceState(_state with { DismissPopupKey = rawValue });
        }

        private bool ApplyDismissPopupRequestToken(string? rawValue)
        {
            var next = 0L;
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                _ = long.TryParse(rawValue, out next);
            }

            return ReplaceState(_state with { DismissPopupRequestToken = next });
        }

        private bool ReplaceState(MirageMapSemanticState next)
        {
            if (MapStateEquals(_state, next))
            {
                return false;
            }

            _state = next;
            return true;
        }

        private static bool MapStateEquals(MirageMapSemanticState left, MirageMapSemanticState right)
        {
            if (left.MapType != right.MapType
                || left.IsShowingUser != right.IsShowingUser
                || left.IsScrollEnabled != right.IsScrollEnabled
                || left.IsZoomEnabled != right.IsZoomEnabled
                || left.IsTrafficEnabled != right.IsTrafficEnabled
                || left.RegionRequestToken != right.RegionRequestToken
                || left.Region != right.Region
                || left.DismissPopupRequestToken != right.DismissPopupRequestToken
                || !string.Equals(left.DismissPopupKey, right.DismissPopupKey, StringComparison.Ordinal))
            {
                return false;
            }

            if (left.Pins.Count != right.Pins.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Pins.Count; index++)
            {
                if (left.Pins[index] != right.Pins[index])
                {
                    return false;
                }
            }

            if (left.Elements.Count != right.Elements.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Elements.Count; index++)
            {
                if (!MapElementEquals(left.Elements[index], right.Elements[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MapElementEquals(MirageMapElement left, MirageMapElement right)
        {
            if (!string.Equals(left.Key, right.Key, StringComparison.Ordinal)
                || left.Kind != right.Kind
                || left.RadiusMeters != right.RadiusMeters
                || !string.Equals(left.StrokeColor, right.StrokeColor, StringComparison.Ordinal)
                || left.StrokeThickness != right.StrokeThickness
                || !string.Equals(left.FillColor, right.FillColor, StringComparison.Ordinal))
            {
                return false;
            }

            if (left.Coordinates.Count != right.Coordinates.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Coordinates.Count; index++)
            {
                if (left.Coordinates[index] != right.Coordinates[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ReadBoolean(MirageValue value, bool defaultValue)
        {
            if (value.TryGetBool(out var boolValue))
            {
                return boolValue;
            }

            return MirageMapSemanticState.ParseBoolean(value.GetStringOrNull(), defaultValue);
        }

        private static double TryGetNumber(JsonObject source, string propertyName)
        {
            if (source[propertyName] is JsonValue valueNode
                && valueNode.TryGetValue<double>(out var number))
            {
                return number;
            }

            return double.NaN;
        }

        private static double? TryGetOptionalNumber(JsonObject source, string propertyName)
        {
            var value = TryGetNumber(source, propertyName);
            return double.IsFinite(value) ? value : null;
        }

        private static string? TryGetString(JsonObject source, string propertyName)
        {
            if (source[propertyName] is JsonValue valueNode
                && valueNode.TryGetValue<string>(out var text))
            {
                return text;
            }

            return null;
        }

        private static MirageMapElementKind ParseElementKind(string? rawKind)
        {
            if (string.IsNullOrWhiteSpace(rawKind))
            {
                return MirageMapElementKind.Unknown;
            }

            return rawKind.Trim().ToLowerInvariant() switch
            {
                "circle" => MirageMapElementKind.Circle,
                "polyline" => MirageMapElementKind.Polyline,
                "polygon" => MirageMapElementKind.Polygon,
                _ => MirageMapElementKind.Unknown
            };
        }

        private static List<MirageMapCoordinate> ParseCoordinates(JsonArray? coordinatesJson)
        {
            if (coordinatesJson is null)
            {
                return [];
            }

            var coordinates = new List<MirageMapCoordinate>(coordinatesJson.Count);
            foreach (var coordinateNode in coordinatesJson)
            {
                if (coordinateNode is not JsonObject coordinateJson)
                {
                    continue;
                }

                var latitude = TryGetNumber(coordinateJson, "latitude");
                var longitude = TryGetNumber(coordinateJson, "longitude");
                if (!double.IsFinite(latitude) || !double.IsFinite(longitude))
                {
                    continue;
                }

                coordinates.Add(new MirageMapCoordinate(latitude, longitude));
            }

            return coordinates;
        }
    }
}
