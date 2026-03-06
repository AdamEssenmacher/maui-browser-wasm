using System.Globalization;
using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserDatePickerHandler : BrowserViewHandler<IDatePicker>
{
    public static IPropertyMapper<IDatePicker, BrowserDatePickerHandler> Mapper = new PropertyMapper<IDatePicker, BrowserDatePickerHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IDatePicker.Date)] = MapDate,
        [nameof(IDatePicker.MinimumDate)] = MapMinimumDate,
        [nameof(IDatePicker.MaximumDate)] = MapMaximumDate,
        [nameof(IDatePicker.Format)] = MapFormat,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IDatePicker.IsOpen)] = MapIsOpen
    };

    private Action? _changeCallback;
    private int _changeSubscriptionId;

    public BrowserDatePickerHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("input");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "date");
        platformView.Style("boxSizing", "border-box");

        _changeCallback = OnDateChanged;
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

    private void OnDateChanged()
    {
        var rawValue = PlatformView.GetInputValue();
        var parsed = ParseDate(rawValue);

        if (Nullable.Equals(VirtualView.Date, parsed))
        {
            return;
        }

        VirtualView.Date = parsed;
    }

    public static void MapDate(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        handler.PlatformView.SetInputValue(FormatDate(datePicker.Date));
    }

    public static void MapMinimumDate(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        handler.PlatformView.Attr("min", FormatDate(datePicker.MinimumDate));
    }

    public static void MapMaximumDate(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        handler.PlatformView.Attr("max", FormatDate(datePicker.MaximumDate));
    }

    public static void MapFormat(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        handler.PlatformView.Attr("data-maui-date-format", string.IsNullOrWhiteSpace(datePicker.Format) ? null : datePicker.Format);
    }

    public static void MapTextColor(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        BrowserViewMapper.MapTextColor(datePicker, handler.PlatformView);
    }

    public static void MapFont(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        BrowserViewMapper.MapFont(datePicker, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        BrowserViewMapper.MapCharacterSpacing(datePicker, handler.PlatformView);
    }

    public static void MapIsOpen(BrowserDatePickerHandler handler, IDatePicker datePicker)
    {
        if (!datePicker.IsOpen)
        {
            return;
        }

        handler.PlatformView.OpenPicker();
    }

    private static DateTime? ParseDate(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (DateTime.TryParseExact(rawValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.Date;
        }

        return null;
    }

    private static string? FormatDate(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
