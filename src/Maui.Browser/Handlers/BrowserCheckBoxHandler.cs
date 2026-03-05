using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserCheckBoxHandler : BrowserViewHandler<ICheckBox>
{
    public static IPropertyMapper<ICheckBox, BrowserCheckBoxHandler> Mapper = new PropertyMapper<ICheckBox, BrowserCheckBoxHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(ICheckBox.IsChecked)] = MapIsChecked,
        [nameof(ICheckBox.Foreground)] = MapForeground
    };

    private Action? _changeCallback;
    private int _changeSubscriptionId;

    public BrowserCheckBoxHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("input");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "checkbox");
        platformView.Style("cursor", "pointer");

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
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultSize = 22;

        var width = ResolveDesiredDimension(widthConstraint, defaultSize);
        var height = ResolveDesiredDimension(heightConstraint, defaultSize);

        return new Size(width, height);
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
        VirtualView.IsChecked = PlatformView.GetInputChecked();
    }

    public static void MapIsChecked(BrowserCheckBoxHandler handler, ICheckBox checkBox)
    {
        handler.PlatformView.SetInputChecked(checkBox.IsChecked);
    }

    public static void MapForeground(BrowserCheckBoxHandler handler, ICheckBox checkBox)
    {
        handler.PlatformView.Style("accentColor", ToCssPaintColor(checkBox.Foreground));
    }

    private static string? ToCssPaintColor(Paint? paint)
    {
        return paint switch
        {
            SolidPaint solidPaint => CssUtil.ToCssColor(solidPaint.Color),
            _ => null
        };
    }
}
