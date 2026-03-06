using System.Globalization;
using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserTimePickerHandler : BrowserViewHandler<ITimePicker>
{
    public static IPropertyMapper<ITimePicker, BrowserTimePickerHandler> Mapper = new PropertyMapper<ITimePicker, BrowserTimePickerHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(ITimePicker.Time)] = MapTime,
        [nameof(ITimePicker.Format)] = MapFormat,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(ITimePicker.IsOpen)] = MapIsOpen
    };

    private Action? _changeCallback;
    private int _changeSubscriptionId;

    public BrowserTimePickerHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("input");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "time");
        platformView.Attr("step", "60");
        platformView.Style("boxSizing", "border-box");

        _changeCallback = OnTimeChanged;
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
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 160;
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

    private void OnTimeChanged()
    {
        var rawValue = PlatformView.GetInputValue();
        var parsed = ParseTime(rawValue);

        if (Nullable.Equals(VirtualView.Time, parsed))
        {
            return;
        }

        VirtualView.Time = parsed;
    }

    public static void MapTime(BrowserTimePickerHandler handler, ITimePicker timePicker)
    {
        handler.PlatformView.SetInputValue(FormatTime(timePicker.Time, timePicker.Format));
    }

    public static void MapFormat(BrowserTimePickerHandler handler, ITimePicker timePicker)
    {
        var format = string.IsNullOrWhiteSpace(timePicker.Format) ? null : timePicker.Format;
        handler.PlatformView.Attr("data-maui-time-format", format);

        var includeSeconds = format?.Contains('s', StringComparison.OrdinalIgnoreCase) == true;
        handler.PlatformView.Attr("step", includeSeconds ? "1" : "60");
        handler.PlatformView.SetInputValue(FormatTime(timePicker.Time, format));
    }

    public static void MapTextColor(BrowserTimePickerHandler handler, ITimePicker timePicker)
    {
        BrowserViewMapper.MapTextColor(timePicker, handler.PlatformView);
    }

    public static void MapFont(BrowserTimePickerHandler handler, ITimePicker timePicker)
    {
        BrowserViewMapper.MapFont(timePicker, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserTimePickerHandler handler, ITimePicker timePicker)
    {
        BrowserViewMapper.MapCharacterSpacing(timePicker, handler.PlatformView);
    }

    public static void MapIsOpen(BrowserTimePickerHandler handler, ITimePicker timePicker)
    {
        if (timePicker.IsOpen)
        {
            handler.PlatformView.OpenPicker();
            return;
        }

        handler.PlatformView.Blur();
    }

    private static TimeSpan? ParseTime(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (TimeSpan.TryParseExact(rawValue, ["hh\\:mm", "hh\\:mm\\:ss"], CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        if (TimeSpan.TryParse(rawValue, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? FormatTime(TimeSpan? value, string? format)
    {
        if (value is null)
        {
            return null;
        }

        var includeSeconds = format?.Contains('s', StringComparison.OrdinalIgnoreCase) == true;
        return includeSeconds
            ? value.Value.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture)
            : value.Value.ToString("hh\\:mm", CultureInfo.InvariantCulture);
    }
}
