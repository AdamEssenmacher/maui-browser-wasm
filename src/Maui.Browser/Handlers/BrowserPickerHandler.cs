using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserPickerHandler : BrowserViewHandler<IPicker>
{
    public static IPropertyMapper<IPicker, BrowserPickerHandler> Mapper = new PropertyMapper<IPicker, BrowserPickerHandler>(ViewHandler.ViewMapper)
    {
        [nameof(IPicker.Items)] = MapItems,
        [nameof(IPicker.SelectedIndex)] = MapSelectedIndex,
        [nameof(IPicker.Title)] = MapTitle,
        [nameof(IPicker.TitleColor)] = MapTitleColor,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.IsEnabled)] = MapIsEnabled
    };

    private readonly List<DomElement> _optionElements = [];
    private Action? _changeCallback;
    private int _changeSubscriptionId;

    public BrowserPickerHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("select");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("boxSizing", "border-box");

        _changeCallback = OnChange;
        _changeSubscriptionId = platformView.On("change", _changeCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_changeSubscriptionId != 0)
        {
            DomElement.Off(_changeSubscriptionId);
            _changeSubscriptionId = 0;
        }

        _changeCallback = null;
        ClearOptions();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 180;
        const double defaultHeight = 36;

        return new Size(
            ResolveDesiredDimension(widthConstraint, defaultWidth),
            ResolveDesiredDimension(heightConstraint, defaultHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return fallback;
        }

        return Math.Max(0, Math.Min(fallback, constraint));
    }

    private void OnChange()
    {
        var selectedIndex = PlatformView.GetSelectSelectedIndex();
        if (VirtualView.SelectedIndex != selectedIndex)
        {
            VirtualView.SelectedIndex = selectedIndex;
        }
    }

    public static void MapItems(BrowserPickerHandler handler, IPicker picker)
    {
        handler.RebuildOptions(picker);
    }

    public static void MapSelectedIndex(BrowserPickerHandler handler, IPicker picker)
    {
        var selectedIndex = picker.SelectedIndex;
        if (selectedIndex < -1 || selectedIndex >= picker.Items.Count)
        {
            selectedIndex = -1;
        }

        handler.PlatformView.SetSelectSelectedIndex(selectedIndex);
    }

    public static void MapTitle(BrowserPickerHandler handler, IPicker picker)
    {
        handler.PlatformView.Attr("aria-label", string.IsNullOrWhiteSpace(picker.Title) ? null : picker.Title);
    }

    public static void MapTitleColor(BrowserPickerHandler handler, IPicker picker)
    {
        handler.PlatformView.Style("color", CssUtil.ToCssColor(picker.TitleColor));
    }

    public static void MapTextColor(BrowserPickerHandler handler, IPicker picker)
    {
        if (picker is ITextStyle textStyle)
        {
            BrowserViewMapper.MapTextColor(textStyle, handler.PlatformView);
        }
    }

    public static void MapFont(BrowserPickerHandler handler, IPicker picker)
    {
        if (picker is ITextStyle textStyle)
        {
            BrowserViewMapper.MapFont(textStyle, handler.PlatformView);
        }
    }

    public static void MapCharacterSpacing(BrowserPickerHandler handler, IPicker picker)
    {
        if (picker is ITextStyle textStyle)
        {
            BrowserViewMapper.MapCharacterSpacing(textStyle, handler.PlatformView);
        }
    }

    public static void MapVisibility(BrowserPickerHandler handler, IPicker picker)
    {
        BrowserViewMapper.MapVisibility(picker, handler.PlatformView);
    }

    public static void MapOpacity(BrowserPickerHandler handler, IPicker picker)
    {
        BrowserViewMapper.MapOpacity(picker, handler.PlatformView);
    }

    public static void MapIsEnabled(BrowserPickerHandler handler, IPicker picker)
    {
        BrowserViewMapper.MapEnabled(picker, handler.PlatformView);
    }

    private void RebuildOptions(IPicker picker)
    {
        ClearOptions();

        for (var i = 0; i < picker.Items.Count; i++)
        {
            var optionElement = DomElement.Create("option");
            optionElement.Attr("value", i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            optionElement.Text(picker.Items[i] ?? string.Empty);
            PlatformView.Append(optionElement);
            _optionElements.Add(optionElement);
        }

        MapSelectedIndex(this, picker);
    }

    private void ClearOptions()
    {
        PlatformView.ClearChildren();

        foreach (var optionElement in _optionElements)
        {
            optionElement.Release();
        }

        _optionElements.Clear();
    }
}
