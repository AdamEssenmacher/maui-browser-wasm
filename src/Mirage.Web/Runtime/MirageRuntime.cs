using Mirage.Core.Diff;
using Mirage.Core.Diagnostics;
using Mirage.Core.Providers;
using Mirage.Core.Semantics;
using Mirage.Web.Dom;

namespace Mirage.Web.Runtime;

public sealed class MirageRuntime : IMirageRuntime
{
    private const string ValueElementMarker = "data-mirage-value-element";

    private readonly DomElement _mountRoot;
    private readonly IMirageComponentProvider _provider;
    private readonly SemanticDiffEngine _diffEngine = new();
    private readonly Dictionary<string, RealizedNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<(string NodeId, string EventName), int> _eventSubscriptions = new();

    private NodeBase? _currentRoot;
    private IMirageEventSink? _eventSink;

    public MirageRuntime(DomElement mountRoot, IMirageComponentProvider provider)
    {
        _mountRoot = mountRoot;
        _provider = provider;

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

        foreach (var node in _nodes.Values.ToArray())
        {
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
            return;
        }

        if (!_nodes.TryGetValue(operation.ParentId, out var parent))
        {
            throw new InvalidOperationException($"Cannot create node '{operation.NodeId}' because parent '{operation.ParentId}' was not found.");
        }

        parent.Element.Insert(operation.Index, realized.Element);
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
            valueElement.Style("boxSizing", "border-box");
            valueElement.Style("width", "100%");
            element.Append(valueElement);
        }

        return RealizedNode.ForControl(element, descriptor, valueElement);
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
        node.Element.Release();
        _nodes.Remove(operation.NodeId);
    }

    private void ApplySetProperty(SetPropertyOperation operation)
    {
        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        if (node.TryGetControlDescriptor(out var controlDescriptor))
        {
            if (!controlDescriptor.PropertyMappings.TryGetValue(operation.PropertyName, out var mapping))
            {
                return;
            }

            var propertyTargetElement = mapping.ElementTarget == PropertyElementTarget.ValueElement
                ? node.GetValueElement()
                : node.Element;

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
                    node.GetValueElement().SetInputValue(operation.Value.GetStringOrNull());
                    break;
                case PropertyTarget.InputChecked:
                {
                    var value = operation.Value.GetStringOrNull();
                    propertyTargetElement.SetInputChecked(string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
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

        if (node.TryGetControlDescriptor(out var controlDescriptor))
        {
            if (!controlDescriptor.PropertyMappings.TryGetValue(operation.PropertyName, out var mapping))
            {
                return;
            }

            var propertyTargetElement = mapping.ElementTarget == PropertyElementTarget.ValueElement
                ? node.GetValueElement()
                : node.Element;

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
                    node.GetValueElement().SetInputValue(null);
                    break;
                case PropertyTarget.InputChecked:
                    propertyTargetElement.SetInputChecked(false);
                    break;
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
        if (_eventSubscriptions.ContainsKey((operation.NodeId, operation.EventName)))
        {
            return;
        }

        if (!_nodes.TryGetValue(operation.NodeId, out var node))
        {
            return;
        }

        var eventDescriptor = ResolveEventDescriptor(node, operation.EventName);
        var targetElement = eventDescriptor.Target == EventTarget.ValueElement
            ? node.GetValueElement()
            : node.Element;

        int subscriptionId;
        if (eventDescriptor.PayloadKind == EventPayloadKind.InputValue)
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, () =>
            {
                var value = targetElement.GetInputValue();
                _eventSink?.OnEvent(operation.NodeId, operation.EventName, value);
            });
        }
        else if (eventDescriptor.PayloadKind == EventPayloadKind.InputChecked)
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, () =>
            {
                _eventSink?.OnEvent(
                    operation.NodeId,
                    operation.EventName,
                    targetElement.GetInputChecked() ? "true" : "false");
            });
        }
        else
        {
            subscriptionId = targetElement.On(eventDescriptor.DomEventName, () =>
            {
                _eventSink?.OnEvent(operation.NodeId, operation.EventName, null);
            });
        }

        _eventSubscriptions[(operation.NodeId, operation.EventName)] = subscriptionId;
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
        private RealizedNode(
            string nodeId,
            DomElement element,
            ControlComponentDescriptor? controlDescriptor,
            WebComponentDescriptor? webDescriptor,
            DomElement? valueElement)
        {
            NodeId = nodeId;
            Element = element;
            ControlDescriptor = controlDescriptor;
            WebDescriptor = webDescriptor;
            ValueElement = valueElement;
        }

        public string NodeId { get; private set; } = string.Empty;

        public DomElement Element { get; }

        public DomElement? ValueElement { get; }

        private ControlComponentDescriptor? ControlDescriptor { get; }

        private WebComponentDescriptor? WebDescriptor { get; }

        public static RealizedNode ForControl(DomElement element, ControlComponentDescriptor descriptor, DomElement? valueElement)
            => new(nodeId: string.Empty, element, descriptor, webDescriptor: null, valueElement);

        public static RealizedNode ForWeb(DomElement element, WebComponentDescriptor descriptor)
            => new(nodeId: string.Empty, element, controlDescriptor: null, descriptor, valueElement: null);

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

        public DomElement GetValueElement()
        {
            return ValueElement ?? Element;
        }

        public RealizedNode WithNodeId(string nodeId)
        {
            NodeId = nodeId;
            return this;
        }
    }
}
