using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageProgressBarHandler : MirageViewHandler<IProgress>
{
    private const double DefaultWidth = 220;
    private const double DefaultHeight = 18;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IProgress.Progress),
        nameof(IProgress.ProgressColor)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<IProgress, MirageProgressBarHandler> Mapper =
        new PropertyMapper<IProgress, MirageProgressBarHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IProgress.Progress)] = MapProgress,
            [nameof(IProgress.ProgressColor)] = MapProgressColor
        };

    public static CommandMapper<IProgress, MirageProgressBarHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    public MirageProgressBarHandler()
        : base(ComponentKind.ProgressBar, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-block";
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, DefaultWidth, DefaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", DefaultDisplayStyle);
        platformView.SetStyle("boxSizing", "border-box");

        var changed = ApplyProgressBarProjectionCore(VirtualView);

        base.ConnectHandler(platformView);

        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapProgress(MirageProgressBarHandler handler, IProgress progressBar)
    {
        if (!handler.UpdateProgressProjection(progressBar))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapProgressColor(MirageProgressBarHandler handler, IProgress progressBar)
    {
        if (!handler.UpdateProgressColorProjection(progressBar))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool ApplyProgressBarProjectionCore(IProgress progressBar)
    {
        using var _ = BeginRenderBatch();

        var changed = UpdateProgressProjection(progressBar);
        changed |= UpdateProgressColorProjection(progressBar);
        return changed;
    }

    private bool UpdateProgressProjection(IProgress progressBar)
    {
        var clamped = NormalizeProgress(progressBar.Progress);
        var projected = clamped.ToString("0.###", CultureInfo.InvariantCulture);
        return SetPropertyIfChanged("Value", projected);
    }

    private bool UpdateProgressColorProjection(IProgress progressBar)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(progressBar.ProgressColor);
        return SetStyleIfChanged("accentColor", cssColor);
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

    private static Size ResolveMeasuredSizeWithConstraints(
        IProgress progressBar,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(progressBar, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(progressBar, isWidth: false);
        return new Size(
            ResolveConstraints(clampedWidth, requestedWidth, requestedMinimumWidth, requestedMaximumWidth),
            ResolveConstraints(clampedHeight, requestedHeight, requestedMinimumHeight, requestedMaximumHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, fallback);
        }

        return Math.Max(0, Math.Min(constraint, fallback));
    }

    private static double ResolveConstraints(double measured, double exact, double min, double max)
    {
        var resolved = measured;
        var resolvedMin = ResolveMinimum(min);

        if (IsExplicitLengthSet(exact))
        {
            resolved = exact;
        }

        if (IsExplicitLengthSet(max) && resolved > max)
        {
            resolved = max;
        }

        if (resolved < resolvedMin)
        {
            resolved = resolvedMin;
        }

        return resolved;
    }

    private static double ResolveMinimum(double value)
    {
        return IsExplicitLengthSet(value)
            ? value
            : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static double NormalizeProgress(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(IProgress progressBar, bool isWidth)
    {
        if (progressBar is not VisualElement element)
        {
            return (-1, -1, -1);
        }

        return isWidth
            ? (element.WidthRequest, element.MinimumWidthRequest, element.MaximumWidthRequest)
            : (element.HeightRequest, element.MinimumHeightRequest, element.MaximumHeightRequest);
    }
}
