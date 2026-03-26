using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;
using System.ComponentModel;
using System.Threading;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRadioButtonHandler : MirageViewHandler<IRadioButton>, IMirageRuntimeEventTarget
{
    private const string HostClickEventName = "click";
    private const string CheckedChangedEventName = "checkedchange";
    private const string InputNodeClass = "mirage-radiobutton-input";
    private const string ContentSlotNodeClass = "mirage-radiobutton-content";
    private const string TextNodeClass = "mirage-radiobutton-text";
    private static int s_childNodeIdCounter;

    private MiragePlatformView? _inputNode;
    private MiragePlatformView? _contentSlotNode;
    private MiragePlatformView? _textNode;
    private RadioInputRuntimeEventTarget? _inputEventTarget;
    private IView? _presentedContent;
    private IElementHandler? _presentedContentHandler;
    private Element? _observedElement;
    private readonly List<BindableObject> _observedAncestors = [];
    private IView? _observedContentRoot;
    private readonly List<BindableObject> _observedContentBindables = [];
    private readonly List<Element> _observedContentElements = [];

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IRadioButton.Background),
        nameof(IRadioButton.IsChecked),
        nameof(ITextStyle.CharacterSpacing),
        nameof(ITextStyle.Font),
        nameof(ITextStyle.TextColor),
        nameof(IRadioButton.Content),
        nameof(IRadioButton.StrokeColor),
        nameof(IRadioButton.StrokeThickness),
        nameof(IRadioButton.CornerRadius)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<IRadioButton, MirageRadioButtonHandler> Mapper =
        new PropertyMapper<IRadioButton, MirageRadioButtonHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IRadioButton.Background)] = MapBackground,
            ["BackgroundColor"] = MapBackground,
            [nameof(IRadioButton.IsChecked)] = MapIsChecked,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(ITextStyle.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            ["TextColor"] = MapTextColor,
            [nameof(IRadioButton.Content)] = MapContent,
            [nameof(RadioButton.TextTransform)] = MapContent,
            [nameof(RadioButton.GroupName)] = MapContent,
            [nameof(IRadioButton.StrokeColor)] = MapStrokeColor,
            [nameof(IRadioButton.StrokeThickness)] = MapStrokeThickness,
            [nameof(IRadioButton.CornerRadius)] = MapCornerRadius,
            ["BorderColor"] = MapStrokeColor,
            ["BorderWidth"] = MapStrokeThickness,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<IRadioButton, MirageRadioButtonHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    public MirageRadioButtonHandler()
        : base(ComponentKind.RadioButton, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    public override void SetVirtualView(IView view)
    {
        DetachGroupObservation();
        DetachContentObservation();
        base.SetVirtualView(view);
        AttachGroupObservation((IRadioButton)view);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        AttachGroupObservation(VirtualView);
        platformView.SetEvent(HostClickEventName, enabled: true);
        var changed = EnsureStructure();
        changed |= ApplyRadioButtonProjectionCore(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DetachGroupObservation();
        _ = DisconnectPresentedContent();
        DisconnectStructure();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;
        if (!string.Equals(eventName, HostClickEventName, StringComparison.Ordinal))
        {
            return;
        }

        if (!VirtualView.IsEnabled || VirtualView.IsChecked)
        {
            return;
        }

        VirtualView.IsChecked = true;
    }

    public static void MapBackground(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapIsChecked(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapCharacterSpacing(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapFont(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapTextColor(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapContent(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapStrokeColor(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapStrokeThickness(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapCornerRadius(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    public static void MapIsEnabled(MirageRadioButtonHandler handler, IRadioButton radioButton) => handler.MapRadioButton(radioButton);

    private void MapRadioButton(IRadioButton radioButton)
    {
        if (!ApplyRadioButtonProjectionCore(radioButton))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyRadioButtonProjectionCore(IRadioButton radioButton)
    {
        var changed = EnsureStructure();

        using var _ = BeginRenderBatch();

        changed |= ApplyContainerStyles(radioButton);
        changed |= UpdateBackgroundProjection(radioButton);
        changed |= UpdateStrokeProjection(radioButton);
        changed |= UpdateIsCheckedProjection(radioButton);
        changed |= UpdateEnabledProjection(radioButton);
        changed |= UpdateGroupNameProjection(radioButton);
        changed |= UpdateContentProjection(radioButton);
        return changed;
    }

    private bool EnsureStructure()
    {
        var changed = false;
        if (_inputNode is null)
        {
            _inputNode = MiragePlatformView.CreateWeb("input");
            _inputNode.AddClass(InputNodeClass);
            _inputNode.SetAttribute("type", "radio");
            _inputNode.SetEvent(CheckedChangedEventName, enabled: true);
            _inputEventTarget = new RadioInputRuntimeEventTarget(this);
            RegisterChildNode(_inputNode, _inputEventTarget);
            changed = true;
        }

        if (_contentSlotNode is null)
        {
            _contentSlotNode = MiragePlatformView.CreateWeb("div");
            _contentSlotNode.AddClass(ContentSlotNodeClass);
            RegisterChildNode(_contentSlotNode);
            changed = true;
        }

        if (_textNode is null)
        {
            _textNode = MiragePlatformView.CreateWeb("span");
            _textNode.AddClass(TextNodeClass);
            RegisterChildNode(_textNode);
            changed = true;
        }

        if (_contentSlotNode is not null
            && _textNode is not null
            && _contentSlotNode.Children.Count == 0
            && _presentedContentHandler is null)
        {
            _contentSlotNode.AddChild(_textNode);
            changed = true;
        }

        if (_inputNode is null || _contentSlotNode is null)
        {
            return changed;
        }

        if (PlatformView.Children.Count != 2
            || !ReferenceEquals(PlatformView.Children[0], _inputNode)
            || !ReferenceEquals(PlatformView.Children[1], _contentSlotNode))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_inputNode);
            PlatformView.AddChild(_contentSlotNode);
            changed = true;
        }

        return changed;
    }

    private void DisconnectStructure()
    {
        PlatformView.ClearChildren();
        UnregisterChildNode(_inputNode);
        UnregisterChildNode(_contentSlotNode);
        UnregisterChildNode(_textNode);
        _inputNode = null;
        _contentSlotNode = null;
        _textNode = null;
        _inputEventTarget = null;
    }

    private bool ApplyContainerStyles(IRadioButton radioButton)
    {
        var changed = SetStyleIfChanged(PlatformView, "display", "inline-flex");
        changed |= SetStyleIfChanged(PlatformView, "alignItems", "center");
        changed |= SetStyleIfChanged(PlatformView, "justifyContent", "flex-start");
        changed |= SetStyleIfChanged(PlatformView, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(PlatformView, "gap", "8px");
        changed |= SetStyleIfChanged(PlatformView, "minWidth", "0");

        if (_inputNode is not null)
        {
            changed |= SetStyleIfChanged(_inputNode, "margin", "0");
            changed |= SetStyleIfChanged(_inputNode, "flex", "0 0 auto");
            changed |= SetStyleIfChanged(_inputNode, "width", "16px");
            changed |= SetStyleIfChanged(_inputNode, "height", "16px");
            changed |= SetStyleIfChanged(_inputNode, "boxSizing", "border-box");
            changed |= SetStyleIfChanged(_inputNode, "accentColor", MirageCommonViewProjection.ToCssColor(radioButton.StrokeColor));
        }

        if (_contentSlotNode is not null)
        {
            changed |= SetStyleIfChanged(_contentSlotNode, "display", "inline-flex");
            changed |= SetStyleIfChanged(_contentSlotNode, "alignItems", "center");
            changed |= SetStyleIfChanged(_contentSlotNode, "minWidth", "0");
            changed |= SetStyleIfChanged(_contentSlotNode, "flex", "1 1 auto");
        }

        if (_textNode is not null)
        {
            changed |= SetStyleIfChanged(_textNode, "display", "inline");
            changed |= SetStyleIfChanged(_textNode, "lineHeight", "1.3");
            changed |= SetStyleIfChanged(_textNode, "whiteSpace", "normal");
        }

        return changed;
    }

    private bool UpdateIsCheckedProjection(IRadioButton radioButton)
    {
        var changed = SetBoolPropertyIfChanged(_inputNode, "checked", radioButton.IsChecked);
        changed |= SetAttributeIfChanged(_inputNode, "aria-checked", radioButton.IsChecked ? "true" : "false");
        return changed;
    }

    private bool UpdateEnabledProjection(IRadioButton radioButton)
    {
        var isEnabled = radioButton.IsEnabled;
        var changed = SetStyleIfChanged(PlatformView, "cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged(PlatformView, "pointerEvents", isEnabled ? "auto" : "none");
        changed |= SetAttributeIfChanged(_inputNode, "disabled", isEnabled ? null : "disabled");
        changed |= SetStyleIfChanged(_inputNode, "cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged(_contentSlotNode, "cursor", isEnabled ? "pointer" : "default");
        return changed;
    }

    private bool UpdateGroupNameProjection(IRadioButton radioButton)
    {
        var effectiveGroupName = ResolveEffectiveGroupName(radioButton);
        return SetAttributeIfChanged(_inputNode, "name", string.IsNullOrWhiteSpace(effectiveGroupName) ? null : effectiveGroupName);
    }

    private bool UpdateBackgroundProjection(IRadioButton radioButton)
    {
        return MirageCommonViewProjection.MapBackground(this, radioButton);
    }

    private bool UpdateStrokeProjection(IRadioButton radioButton)
    {
        var strokeThickness = radioButton.StrokeThickness;
        var normalizedThickness = double.IsFinite(strokeThickness) && strokeThickness > 0 ? strokeThickness : 0;

        var changed = SetStyleIfChanged(PlatformView, "borderStyle", normalizedThickness > 0 ? "solid" : null);
        changed |= SetStyleIfChanged(PlatformView, "borderWidth", normalizedThickness > 0 ? MirageViewMapper.ToPx(normalizedThickness) : null);
        var strokeColor = normalizedThickness > 0
            ? MirageCommonViewProjection.ToCssColor(radioButton.StrokeColor) ?? "currentColor"
            : null;
        changed |= SetStyleIfChanged(PlatformView, "borderColor", strokeColor);

        var cornerRadius = radioButton.CornerRadius;
        changed |= SetStyleIfChanged(
            PlatformView,
            "borderRadius",
            cornerRadius >= 0 ? MirageViewMapper.ToPx(cornerRadius) : null);

        return changed;
    }

    private bool UpdateContentProjection(IRadioButton radioButton)
    {
        if (radioButton.Content is IView viewContent)
        {
            var changed = SetViewContent(viewContent);
            changed |= SetPropertyIfChanged(_textNode, "textContent", null);
            changed |= UpdateContentAriaLabel(viewContent);
            return changed;
        }

        var textContent = ResolveProjectedTextContent(radioButton);
        var textChanged = SetTextContent(textContent);
        textChanged |= UpdateTextProjection(radioButton);
        textChanged |= SetAttributeIfChanged(_inputNode, "aria-label", string.IsNullOrWhiteSpace(textContent) ? "Radio button" : textContent);
        return textChanged;
    }

    private bool SetTextContent(string textContent)
    {
        var changed = DisconnectPresentedContent();
        if (_contentSlotNode is null || _textNode is null)
        {
            return changed;
        }

        if (_contentSlotNode.Children.Count != 1 || !ReferenceEquals(_contentSlotNode.Children[0], _textNode))
        {
            _contentSlotNode.ClearChildren();
            _contentSlotNode.AddChild(_textNode);
            changed = true;
        }

        changed |= SetPropertyIfChanged(_textNode, "textContent", textContent);
        return changed;
    }

    private bool SetViewContent(IView contentView)
    {
        var changed = false;
        if (!ReferenceEquals(_presentedContent, contentView))
        {
            changed |= DisconnectPresentedContent();
            _presentedContent = contentView;
        }

        if (_presentedContentHandler is null || _presentedContentHandler.PlatformView is not MiragePlatformView)
        {
            if (!TryGetChild(contentView, out var contentHandler, out var contentPlatformView))
            {
                return changed;
            }

            _presentedContent = contentView;
            _presentedContentHandler = contentHandler;
            if (contentHandler is IViewHandler viewHandler)
            {
                MirageViewMapper.MapLayoutAlignment(viewHandler, contentView);
            }

            AttachContentObservation(contentView);

            NormalizeHostedContentFlowStyles(contentPlatformView);
            if (_contentSlotNode is not null)
            {
                _contentSlotNode.ClearChildren();
                _contentSlotNode.AddChild(contentPlatformView);
                changed = true;
            }

            return changed;
        }

        var existingPlatformView = (MiragePlatformView)_presentedContentHandler.PlatformView;
        AttachContentObservation(contentView);
        NormalizeHostedContentFlowStyles(existingPlatformView);
        if (_contentSlotNode is null)
        {
            return changed;
        }

        if (_contentSlotNode.Children.Count != 1 || !ReferenceEquals(_contentSlotNode.Children[0], existingPlatformView))
        {
            _contentSlotNode.ClearChildren();
            _contentSlotNode.AddChild(existingPlatformView);
            changed = true;
        }

        return changed;
    }

    private bool DisconnectPresentedContent()
    {
        if (_presentedContentHandler is null && _presentedContent is null)
        {
            return false;
        }

        DetachContentObservation();
        MirageHandlerDisconnect.SafeDisconnect(_presentedContentHandler);
        _presentedContentHandler = null;
        _presentedContent = null;
        return true;
    }

    private bool UpdateContentAriaLabel(IView contentView)
    {
        var resolved = ResolveAriaLabel(contentView);
        return SetAttributeIfChanged(_inputNode, "aria-label", string.IsNullOrWhiteSpace(resolved) ? "Radio button" : resolved);
    }

    private bool UpdateTextProjection(IRadioButton radioButton)
    {
        var changed = MirageCommonViewProjection.MapTextColor(this, radioButton);
        changed |= MirageCommonViewProjection.MapFont(this, radioButton);
        changed |= MirageCommonViewProjection.MapCharacterSpacing(this, radioButton);

        changed |= SetStyleIfChanged(_textNode, "color", PlatformView.GetStyleValue("color"));
        changed |= SetStyleIfChanged(_textNode, "fontFamily", PlatformView.GetStyleValue("fontFamily"));
        changed |= SetStyleIfChanged(_textNode, "fontSize", PlatformView.GetStyleValue("fontSize"));
        changed |= SetStyleIfChanged(_textNode, "fontWeight", PlatformView.GetStyleValue("fontWeight"));
        changed |= SetStyleIfChanged(_textNode, "fontStyle", PlatformView.GetStyleValue("fontStyle"));
        changed |= SetStyleIfChanged(_textNode, "letterSpacing", PlatformView.GetStyleValue("letterSpacing"));
        return changed;
    }

    private void OnInputEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, CheckedChangedEventName, StringComparison.Ordinal))
        {
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        var nextIsChecked = string.Equals(payload, "true", StringComparison.OrdinalIgnoreCase);
        if (VirtualView.IsChecked == nextIsChecked)
        {
            return;
        }

        VirtualView.IsChecked = nextIsChecked;
    }

    private void RegisterChildNode(MiragePlatformView childNode, IMirageRuntimeEventTarget? eventTarget = null)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(childNode, eventTarget);
            return;
        }

        if (!string.IsNullOrWhiteSpace(childNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_childNodeIdCounter);
        childNode.AssignNodeId($"radiobutton-node-{localId:D4}");
    }

    private void UnregisterChildNode(MiragePlatformView? childNode)
    {
        if (childNode is null || Coordinator is not { } coordinator)
        {
            return;
        }

        coordinator.Unregister(childNode);
    }

    private static string ResolveProjectedTextContent(IRadioButton radioButton)
    {
        var source = radioButton.Content?.ToString() ?? string.Empty;
        if (radioButton is not RadioButton controlsRadio)
        {
            return source;
        }

        var transform = controlsRadio.TextTransform == TextTransform.Default
            ? TextTransform.None
            : controlsRadio.TextTransform;
        return TextTransformUtilities.GetTransformedText(source, transform);
    }

    private static string? ResolveEffectiveGroupName(IRadioButton radioButton)
    {
        if (radioButton is not Element element)
        {
            return null;
        }

        if (radioButton is RadioButton controlsRadio && !string.IsNullOrWhiteSpace(controlsRadio.GroupName))
        {
            return controlsRadio.GroupName;
        }

        for (Element? current = element.Parent; current is not null; current = current.Parent)
        {
            if (current is not BindableObject bindable)
            {
                continue;
            }

            var groupName = RadioButtonGroup.GetGroupName(bindable);
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                return groupName;
            }
        }

        return null;
    }

    private static string? ResolveAriaLabel(IView contentView)
    {
        if (TryResolveContentLabelText(contentView, out var contentLabelText))
        {
            return contentLabelText;
        }

        if (!string.IsNullOrWhiteSpace(contentView.AutomationId))
        {
            return contentView.AutomationId;
        }

        return null;
    }

    private void AttachGroupObservation(IRadioButton radioButton)
    {
        if (radioButton is not Element element)
        {
            return;
        }

        if (!ReferenceEquals(_observedElement, element))
        {
            DetachGroupObservation();
            _observedElement = element;
            _observedElement.ParentChanged += OnObservedElementParentChanged;
        }

        RebuildAncestorObservation();
    }

    private void DetachGroupObservation()
    {
        if (_observedElement is not null)
        {
            _observedElement.ParentChanged -= OnObservedElementParentChanged;
            _observedElement = null;
        }

        foreach (var ancestor in _observedAncestors)
        {
            ancestor.PropertyChanged -= OnObservedAncestorPropertyChanged;
        }

        _observedAncestors.Clear();
    }

    private void OnObservedElementParentChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        RebuildAncestorObservation();
        RequestGroupNameProjectionRefresh();
    }

    private void AttachContentObservation(IView contentRoot)
    {
        if (!ReferenceEquals(_observedContentRoot, contentRoot))
        {
            DetachContentObservation();
            _observedContentRoot = contentRoot;
        }

        RebuildContentObservation();
    }

    private void DetachContentObservation()
    {
        foreach (var bindable in _observedContentBindables)
        {
            bindable.PropertyChanged -= OnObservedContentPropertyChanged;
        }

        foreach (var element in _observedContentElements)
        {
            element.ChildAdded -= OnObservedContentChildChanged;
            element.ChildRemoved -= OnObservedContentChildChanged;
        }

        _observedContentBindables.Clear();
        _observedContentElements.Clear();
        _observedContentRoot = null;
    }

    private void RebuildContentObservation()
    {
        if (_observedContentRoot is null)
        {
            return;
        }

        var nextBindables = new List<BindableObject>();
        var nextElements = new List<Element>();
        var visited = new HashSet<IView>();
        CollectObservedContentNodes(_observedContentRoot, nextBindables, nextElements, visited);

        foreach (var bindable in _observedContentBindables)
        {
            if (ContainsReference(nextBindables, bindable))
            {
                continue;
            }

            bindable.PropertyChanged -= OnObservedContentPropertyChanged;
        }

        foreach (var bindable in nextBindables)
        {
            if (ContainsReference(_observedContentBindables, bindable))
            {
                continue;
            }

            bindable.PropertyChanged += OnObservedContentPropertyChanged;
        }

        foreach (var element in _observedContentElements)
        {
            if (ContainsReference(nextElements, element))
            {
                continue;
            }

            element.ChildAdded -= OnObservedContentChildChanged;
            element.ChildRemoved -= OnObservedContentChildChanged;
        }

        foreach (var element in nextElements)
        {
            if (ContainsReference(_observedContentElements, element))
            {
                continue;
            }

            element.ChildAdded += OnObservedContentChildChanged;
            element.ChildRemoved += OnObservedContentChildChanged;
        }

        _observedContentBindables.Clear();
        _observedContentBindables.AddRange(nextBindables);
        _observedContentElements.Clear();
        _observedContentElements.AddRange(nextElements);
    }

    private void OnObservedContentChildChanged(object? sender, ElementEventArgs e)
    {
        _ = sender;
        _ = e;
        RebuildContentObservation();
        RequestContentAriaLabelRefresh();
    }

    private void OnObservedContentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (!IsContentLabelRelevantProperty(e.PropertyName))
        {
            return;
        }

        RebuildContentObservation();
        RequestContentAriaLabelRefresh();
    }

    private void RequestContentAriaLabelRefresh()
    {
        if (_presentedContent is null)
        {
            return;
        }

        if (!UpdateContentAriaLabel(_presentedContent))
        {
            return;
        }

        RequestRender();
    }

    private void OnObservedAncestorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (!IsGroupNamePropertyChange(e.PropertyName))
        {
            return;
        }

        RequestGroupNameProjectionRefresh();
    }

    private void RequestGroupNameProjectionRefresh()
    {
        if (!UpdateGroupNameProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    private void RebuildAncestorObservation()
    {
        if (_observedElement is null)
        {
            return;
        }

        var nextAncestors = new List<BindableObject>();
        for (Element? current = _observedElement.Parent; current is not null; current = current.Parent)
        {
            if (current is BindableObject bindable)
            {
                nextAncestors.Add(bindable);
            }
        }

        foreach (var ancestor in _observedAncestors)
        {
            if (nextAncestors.Contains(ancestor))
            {
                continue;
            }

            ancestor.PropertyChanged -= OnObservedAncestorPropertyChanged;
        }

        foreach (var ancestor in nextAncestors)
        {
            if (_observedAncestors.Contains(ancestor))
            {
                continue;
            }

            ancestor.PropertyChanged += OnObservedAncestorPropertyChanged;
        }

        _observedAncestors.Clear();
        _observedAncestors.AddRange(nextAncestors);
    }

    private static bool IsGroupNamePropertyChange(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return true;
        }

        return string.Equals(propertyName, RadioButtonGroup.GroupNameProperty.PropertyName, StringComparison.Ordinal)
               || string.Equals(propertyName, nameof(RadioButton.GroupName), StringComparison.Ordinal);
    }

    private static bool IsContentLabelRelevantProperty(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return true;
        }

        return string.Equals(propertyName, nameof(IText.Text), StringComparison.Ordinal)
               || string.Equals(propertyName, nameof(IView.AutomationId), StringComparison.Ordinal)
               || string.Equals(propertyName, nameof(ContentView.Content), StringComparison.Ordinal)
               || string.Equals(propertyName, nameof(IContentView.PresentedContent), StringComparison.Ordinal)
               || string.Equals(propertyName, nameof(Layout.Children), StringComparison.Ordinal);
    }

    private static bool ContainsReference<T>(List<T> items, T target)
        where T : class
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (ReferenceEquals(items[index], target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveContentLabelText(IView view, out string? labelText)
    {
        labelText = null;
        var segments = new List<string>();
        var visited = new HashSet<IView>();
        CollectContentText(view, segments, visited);
        if (segments.Count == 0)
        {
            return false;
        }

        labelText = string.Join(" ", segments);
        return true;
    }

    private static void CollectContentText(IView view, List<string> segments, HashSet<IView> visited)
    {
        if (!visited.Add(view))
        {
            return;
        }

        if (view is IText textView && !string.IsNullOrWhiteSpace(textView.Text))
        {
            segments.Add(textView.Text.Trim());
        }

        if (view is IContentView contentView && contentView.PresentedContent is IView presented)
        {
            CollectContentText(presented, segments, visited);
            return;
        }

        if (view is Layout layout)
        {
            for (var index = 0; index < layout.Children.Count; index++)
            {
                CollectContentText(layout.Children[index], segments, visited);
            }
        }
    }

    private static void CollectObservedContentNodes(
        IView view,
        List<BindableObject> bindables,
        List<Element> elements,
        HashSet<IView> visited)
    {
        if (!visited.Add(view))
        {
            return;
        }

        if (view is BindableObject bindable)
        {
            bindables.Add(bindable);
        }

        if (view is Element element)
        {
            elements.Add(element);
        }

        if (view is IContentView contentView && contentView.PresentedContent is IView presented)
        {
            CollectObservedContentNodes(presented, bindables, elements, visited);
            return;
        }

        if (view is Layout layout)
        {
            for (var index = 0; index < layout.Children.Count; index++)
            {
                CollectObservedContentNodes(layout.Children[index], bindables, elements, visited);
            }
        }
    }

    private static void NormalizeHostedContentFlowStyles(MiragePlatformView contentPlatformView)
    {
        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
    }

    private static bool SetStyleIfChanged(MiragePlatformView? node, string styleName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (string.Equals(node.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetStyle(styleName, value);
        return true;
    }

    private static bool SetPropertyIfChanged(MiragePlatformView? node, string propertyName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (string.Equals(node.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetProperty(propertyName, value);
        return true;
    }

    private static bool SetBoolPropertyIfChanged(MiragePlatformView? node, string propertyName, bool value)
    {
        if (node is null)
        {
            return false;
        }

        var projected = MirageValue.Bool(value);
        if (node.Properties.TryGetValue(propertyName, out var current) && current.Equals(projected))
        {
            return false;
        }

        node.SetProperty(propertyName, projected);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView? node, string attributeName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        var current = node.Attributes.TryGetValue(attributeName, out var existing)
            ? existing
            : null;
        if (string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetAttribute(attributeName, value);
        return true;
    }

    private sealed class RadioInputRuntimeEventTarget(MirageRadioButtonHandler owner) : IMirageRuntimeEventTarget
    {
        public void OnRuntimeEvent(string eventName, string? payload)
        {
            owner.OnInputEvent(eventName, payload);
        }
    }
}
