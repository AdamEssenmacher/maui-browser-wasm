using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePickerHandler : MirageViewHandler<IPicker>, IMirageRuntimeEventTarget
{
    private static int s_optionNodeIdCounter;

    public static IPropertyMapper<IPicker, MiragePickerHandler> Mapper =
        new PropertyMapper<IPicker, MiragePickerHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IPicker.Items)] = MapItems,
            [nameof(IPicker.SelectedIndex)] = MapSelectedIndex,
            [nameof(IPicker.Title)] = MapTitle,
            [nameof(IPicker.TitleColor)] = MapTitleColor,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests
        };

    private readonly List<MiragePlatformView> _optionNodes = [];

    public MiragePickerHandler()
        : base(ComponentKind.Picker, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", "block");
        platformView.SetStyle("width", "100%");
        platformView.SetStyle("minHeight", "36px");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetEvent("change", enabled: true);

        base.ConnectHandler(platformView);

        var changed = false;
        changed |= RebuildOptions(VirtualView);
        changed |= UpdateTitleProjection(VirtualView);
        changed |= UpdateEnabledProjection(VirtualView);
        changed |= UpdateDisplayedTextColor(VirtualView);

        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        ClearOptions();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, "change", StringComparison.Ordinal) || !VirtualView.IsEnabled)
        {
            return;
        }

        var selectedIndex = ParseSelectedIndex(VirtualView, payload);
        if (VirtualView.SelectedIndex == selectedIndex)
        {
            return;
        }

        VirtualView.SelectedIndex = selectedIndex;
    }

    public static void MapItems(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.RebuildOptions(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSelectedIndex(MiragePickerHandler handler, IPicker picker)
    {
        var changed = handler.UpdateSelectedValueProjection(picker);
        changed |= handler.UpdateSelectedOptionProjection(picker);
        changed |= handler.UpdateDisplayedTextColor(picker);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTitle(MiragePickerHandler handler, IPicker picker)
    {
        var changed = handler.UpdateTitleProjection(picker);
        changed |= handler.RebuildOptions(picker);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTitleColor(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateDisplayedTextColor(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTextColor(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateDisplayedTextColor(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapFont(MiragePickerHandler handler, IPicker picker)
    {
        if (!MirageCommonViewProjection.MapFont(handler, picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCharacterSpacing(MiragePickerHandler handler, IPicker picker)
    {
        if (!MirageCommonViewProjection.MapCharacterSpacing(handler, picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateEnabledProjection(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequests(MiragePickerHandler handler, IPicker picker)
    {
        if (!MirageCommonViewProjection.MapSizeRequests(handler, picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequestsAndLayoutAlignment(MiragePickerHandler handler, IPicker picker)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, picker);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, picker);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    private bool RebuildOptions(IPicker picker)
    {
        var changed = ClearOptions();

        if (HasPlaceholder(picker))
        {
            var placeholderNode = CreateOptionNode(string.Empty, picker.Title ?? string.Empty, disabled: true);
            PlatformView.AddChild(placeholderNode);
            _optionNodes.Add(placeholderNode);
            changed = true;
        }

        for (var index = 0; index < picker.Items.Count; index++)
        {
            var optionNode = CreateOptionNode(
                index.ToString(CultureInfo.InvariantCulture),
                picker.Items[index] ?? string.Empty,
                disabled: false);
            PlatformView.AddChild(optionNode);
            _optionNodes.Add(optionNode);
            changed = true;
        }

        changed |= UpdateSelectedValueProjection(picker);
        changed |= UpdateSelectedOptionProjection(picker);
        changed |= UpdateDisplayedTextColor(picker);

        return changed;
    }

    private MiragePlatformView CreateOptionNode(string value, string text, bool disabled)
    {
        var optionNode = MiragePlatformView.CreateWeb("option");
        RegisterOptionNode(optionNode);
        optionNode.SetAttribute("value", value);
        optionNode.SetProperty("textContent", text);
        optionNode.SetAttribute("disabled", disabled ? "disabled" : null);
        return optionNode;
    }

    private void RegisterOptionNode(MiragePlatformView optionNode)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(optionNode, eventTarget: null);
            return;
        }

        if (!string.IsNullOrWhiteSpace(optionNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_optionNodeIdCounter);
        optionNode.AssignNodeId($"picker-option-{localId:D4}");
    }

    private bool ClearOptions()
    {
        var changed = _optionNodes.Count > 0 || PlatformView.Children.Count > 0;
        if (changed)
        {
            PlatformView.ClearChildren();
        }

        if (Coordinator is { } coordinator)
        {
            foreach (var optionNode in _optionNodes)
            {
                coordinator.Unregister(optionNode);
            }
        }

        _optionNodes.Clear();
        return changed;
    }

    private bool UpdateTitleProjection(IPicker picker)
    {
        var title = string.IsNullOrWhiteSpace(picker.Title) ? null : picker.Title;
        return SetPropertyIfChanged("Title", title);
    }

    private bool UpdateSelectedValueProjection(IPicker picker)
    {
        var selectedIndex = NormalizeSelectedIndex(picker, picker.SelectedIndex);
        var selectedValue = selectedIndex >= 0
            ? selectedIndex.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        return SetPropertyIfChanged("SelectedValue", selectedValue);
    }

    private bool UpdateSelectedOptionProjection(IPicker picker)
    {
        if (_optionNodes.Count == 0)
        {
            return false;
        }

        var selectedIndex = NormalizeSelectedIndex(picker, picker.SelectedIndex);
        var hasPlaceholder = HasPlaceholder(picker);
        var changed = false;

        for (var optionIndex = 0; optionIndex < _optionNodes.Count; optionIndex++)
        {
            var selected = hasPlaceholder
                ? (selectedIndex < 0 ? optionIndex == 0 : optionIndex == selectedIndex + 1)
                : optionIndex == selectedIndex;

            var selectedAttribute = selected ? "selected" : null;
            var optionNode = _optionNodes[optionIndex];
            optionNode.Attributes.TryGetValue("selected", out var currentAttribute);
            if (string.Equals(currentAttribute, selectedAttribute, StringComparison.Ordinal))
            {
                continue;
            }

            optionNode.SetAttribute("selected", selectedAttribute);
            changed = true;
        }

        return changed;
    }

    private bool UpdateDisplayedTextColor(IPicker picker)
    {
        var selectedIndex = NormalizeSelectedIndex(picker, picker.SelectedIndex);
        var useTitleColor = HasPlaceholder(picker) && selectedIndex < 0;

        var cssColor = useTitleColor
            ? MirageCommonViewProjection.ToCssColor(picker.TitleColor)
            : ResolvePickerTextColor(picker);

        if (cssColor is null && useTitleColor)
        {
            cssColor = ResolvePickerTextColor(picker);
        }

        return SetStyleIfChanged("color", cssColor);
    }

    private bool UpdateEnabledProjection(IPicker picker)
    {
        var isEnabled = picker.IsEnabled;
        var changed = SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        changed |= SetStyleIfChanged("cursor", isEnabled ? "default" : "not-allowed");
        changed |= SetStyleIfChanged("pointerEvents", isEnabled ? "auto" : "none");
        return changed;
    }

    private bool SetPropertyIfChanged(string propertyName, string? value)
    {
        if (string.Equals(PlatformView.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(propertyName, value);
        return true;
    }

    private bool SetStyleIfChanged(string styleName, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, value);
        return true;
    }

    private static string? ResolvePickerTextColor(IPicker picker)
    {
        if (picker is not ITextStyle textStyle)
        {
            return null;
        }

        return MirageCommonViewProjection.ToCssColor(textStyle.TextColor);
    }

    private static int NormalizeSelectedIndex(IPicker picker, int selectedIndex)
    {
        return selectedIndex >= 0 && selectedIndex < picker.Items.Count
            ? selectedIndex
            : -1;
    }

    private static int ParseSelectedIndex(IPicker picker, string? payload)
    {
        if (HasPlaceholder(picker) && string.IsNullOrWhiteSpace(payload))
        {
            return -1;
        }

        return int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? NormalizeSelectedIndex(picker, parsed)
            : -1;
    }

    private static bool HasPlaceholder(IPicker picker)
    {
        return !string.IsNullOrWhiteSpace(picker.Title);
    }
}
