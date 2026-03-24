using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using System.Globalization;
using MauiILayout = Microsoft.Maui.ILayout;
using MauiIndicatorView = Microsoft.Maui.Controls.IndicatorView;

namespace Mirage.Maui.Handlers;

internal sealed class MirageIndicatorViewHandler : MirageViewHandler<IIndicatorView>
{
    private const double DefaultIndicatorSize = 8d;
    private const string DefaultIndicatorColor = "rgba(148,163,184,0.55)";
    private const string DefaultSelectedIndicatorColor = "rgba(37,99,235,1)";
    private static readonly HashSet<string> UnsupportedPaintWarnings = [];
    private static int s_dotNodeIdCounter;

    private readonly List<IndicatorDotNode> _dotNodes = [];
    private IElementHandler? _templatedLayoutHandler;
    private MiragePlatformView? _templatedLayoutPlatformView;
    private MauiILayout? _templatedLayoutView;

    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(IIndicatorView.Count),
        nameof(IIndicatorView.Position),
        nameof(IIndicatorView.HideSingle),
        nameof(IIndicatorView.MaximumVisible),
        nameof(IIndicatorView.IndicatorSize),
        nameof(IIndicatorView.IndicatorColor),
        nameof(IIndicatorView.SelectedIndicatorColor),
        nameof(IIndicatorView.IndicatorsShape)
    };

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    {
        nameof(IView.IsEnabled),
        nameof(MauiIndicatorView.IndicatorTemplate),
        nameof(MauiIndicatorView.IndicatorLayout),
        nameof(ITemplatedIndicatorView.IndicatorsLayoutOverride)
    };

    public static IPropertyMapper<IIndicatorView, MirageIndicatorViewHandler> Mapper =
        new PropertyMapper<IIndicatorView, MirageIndicatorViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IIndicatorView.Count)] = MapCount,
            [nameof(IIndicatorView.Position)] = MapPosition,
            [nameof(IIndicatorView.HideSingle)] = MapHideSingle,
            [nameof(IIndicatorView.MaximumVisible)] = MapMaximumVisible,
            [nameof(IIndicatorView.IndicatorSize)] = MapIndicatorSize,
            [nameof(IIndicatorView.IndicatorColor)] = MapIndicatorColor,
            [nameof(IIndicatorView.SelectedIndicatorColor)] = MapSelectedIndicatorColor,
            [nameof(IIndicatorView.IndicatorsShape)] = MapIndicatorShape,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            [nameof(MauiIndicatorView.IndicatorTemplate)] = MapTemplate,
            [nameof(MauiIndicatorView.IndicatorLayout)] = MapTemplate,
            [nameof(ITemplatedIndicatorView.IndicatorsLayoutOverride)] = MapTemplate
        };

    public static CommandMapper<IIndicatorView, MirageIndicatorViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    public MirageIndicatorViewHandler()
        : base(ComponentKind.IndicatorView, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "inline-flex");
        changed |= SetStyleIfChanged("alignItems", "center");
        changed |= SetStyleIfChanged("justifyContent", "center");
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= ApplyIndicatorProjection(VirtualView);

        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _ = DisconnectTemplateLayout();
        _ = ClearDotNodes();
        base.DisconnectHandler(platformView);
    }

    public static void MapCount(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapPosition(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapHideSingle(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapMaximumVisible(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapIndicatorSize(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapIndicatorColor(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapSelectedIndicatorColor(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapIndicatorShape(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapIsEnabled(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    public static void MapTemplate(MirageIndicatorViewHandler handler, IIndicatorView indicator)
    {
        handler.MapIndicatorCore(indicator);
    }

    private void MapIndicatorCore(IIndicatorView indicator)
    {
        if (!ApplyIndicatorProjection(indicator))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyIndicatorProjection(IIndicatorView indicator)
    {
        using var _ = BeginRenderBatch();

        var changed = false;
        if (TryGetTemplateLayout(indicator, out var templateLayout))
        {
            changed |= ClearDotNodes();
            changed |= EnsureTemplateLayout(templateLayout!);
            changed |= SetStyleIfChanged("gap", "0px");
            changed |= SetStyleIfChanged("minHeight", null);
            changed |= SetStyleIfChanged("cursor", "default");
            changed |= SetStyleIfChanged("alignItems", "stretch");
            changed |= SetStyleIfChanged("justifyContent", "stretch");
        }
        else
        {
            changed |= DisconnectTemplateLayout();
            changed |= EnsureDefaultDots(indicator);
        }

        return changed;
    }

    private bool EnsureDefaultDots(IIndicatorView indicator)
    {
        var visibleCount = GetVisibleIndicatorCount(indicator);
        var normalizedPosition = visibleCount > 0
            ? Math.Clamp(indicator.Position, 0, visibleCount - 1)
            : -1;
        var indicatorSize = NormalizeIndicatorSize(indicator.IndicatorSize);
        var indicatorColor = ResolvePaintCssColor(indicator.IndicatorColor, DefaultIndicatorColor, "IndicatorColor");
        var selectedColor = ResolvePaintCssColor(indicator.SelectedIndicatorColor, DefaultSelectedIndicatorColor, "SelectedIndicatorColor");
        var isCircleShape = ResolveIsCircleShape(indicator);
        var dotBorderRadius = isCircleShape
            ? "999px"
            : "0px";
        var dotDimension = MirageViewMapper.ToPx(indicatorSize);
        var gapSize = MirageViewMapper.ToPx(Math.Max(0d, indicatorSize * 0.5d));
        var hostMinHeight = visibleCount > 0 ? dotDimension : null;
        var hostCursor = indicator.IsEnabled && visibleCount > 0 ? "pointer" : "default";

        var changed = EnsureDotCount(visibleCount);
        changed |= SetStyleIfChanged("gap", gapSize);
        changed |= SetStyleIfChanged("minHeight", hostMinHeight);
        changed |= SetStyleIfChanged("alignItems", "center");
        changed |= SetStyleIfChanged("justifyContent", "center");
        changed |= SetStyleIfChanged("cursor", hostCursor);

        for (var index = 0; index < _dotNodes.Count; index++)
        {
            var dotNode = _dotNodes[index];
            dotNode.EventTarget.Index = index;

            var selected = index == normalizedPosition;
            changed |= SetAttributeIfChanged(dotNode.PlatformView, "data-mirage-indicator-index", index.ToString(CultureInfo.InvariantCulture));
            changed |= SetAttributeIfChanged(dotNode.PlatformView, "data-mirage-indicator-selected", selected ? "true" : null);
            changed |= SetAttributeIfChanged(dotNode.PlatformView, "aria-current", selected ? "true" : null);
            changed |= SetAttributeIfChanged(dotNode.PlatformView, "aria-label", $"Indicator {index + 1}");
            changed |= SetAttributeIfChanged(dotNode.PlatformView, "disabled", indicator.IsEnabled ? null : "disabled");
            changed |= SetStyleIfChanged(dotNode.PlatformView, "width", dotDimension);
            changed |= SetStyleIfChanged(dotNode.PlatformView, "height", dotDimension);
            changed |= SetStyleIfChanged(dotNode.PlatformView, "minWidth", dotDimension);
            changed |= SetStyleIfChanged(dotNode.PlatformView, "minHeight", dotDimension);
            changed |= SetStyleIfChanged(dotNode.PlatformView, "borderRadius", dotBorderRadius);
            changed |= SetStyleIfChanged(dotNode.PlatformView, "backgroundColor", selected ? selectedColor : indicatorColor);
            var baseOpacity = selected ? 1d : 0.56d;
            var effectiveOpacity = indicator.IsEnabled ? baseOpacity : baseOpacity * 0.55d;
            var opacity = $"{effectiveOpacity:0.###}";
            changed |= SetStyleIfChanged(dotNode.PlatformView, "opacity", opacity);
            changed |= SetStyleIfChanged(dotNode.PlatformView, "cursor", indicator.IsEnabled ? "pointer" : "default");
            changed |= SetStyleIfChanged(dotNode.PlatformView, "pointerEvents", indicator.IsEnabled ? "auto" : "none");
        }

        return changed;
    }

    private bool EnsureTemplateLayout(MauiILayout templateLayout)
    {
        if (!TryGetChild((IView)templateLayout, out var childHandler, out var childPlatformView))
        {
            return false;
        }

        var changed = false;
        if (!ReferenceEquals(_templatedLayoutView, templateLayout))
        {
            changed |= DisconnectTemplateLayout();
            _templatedLayoutView = templateLayout;
            _templatedLayoutHandler = childHandler;
            _templatedLayoutPlatformView = childPlatformView;
        }
        else if (!ReferenceEquals(_templatedLayoutPlatformView, childPlatformView))
        {
            _templatedLayoutHandler = childHandler;
            _templatedLayoutPlatformView = childPlatformView;
        }

        if (_templatedLayoutPlatformView is not null
            && (PlatformView.Children.Count != 1 || !ReferenceEquals(PlatformView.Children[0], _templatedLayoutPlatformView)))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_templatedLayoutPlatformView);
            NormalizeTemplateLayoutStyles(_templatedLayoutPlatformView);
            changed = true;
        }

        return changed;
    }

    private bool DisconnectTemplateLayout()
    {
        var changed = false;
        if (_templatedLayoutPlatformView is not null)
        {
            if (PlatformView.Children.Count > 0)
            {
                PlatformView.ClearChildren();
                changed = true;
            }

            _templatedLayoutPlatformView = null;
        }

        if (_templatedLayoutHandler is not null)
        {
            MirageHandlerDisconnect.SafeDisconnect(_templatedLayoutHandler);
            _templatedLayoutHandler = null;
        }

        _templatedLayoutView = null;
        return changed;
    }

    private bool EnsureDotCount(int targetCount)
    {
        var changed = false;
        while (_dotNodes.Count > targetCount)
        {
            changed |= RemoveLastDotNode();
        }

        while (_dotNodes.Count < targetCount)
        {
            changed |= AddDotNode(_dotNodes.Count);
        }

        return changed;
    }

    private bool ClearDotNodes()
    {
        var changed = false;
        while (_dotNodes.Count > 0)
        {
            changed |= RemoveLastDotNode();
        }

        return changed;
    }

    private bool AddDotNode(int index)
    {
        var dotNode = MiragePlatformView.CreateWeb("button");
        dotNode.SetAttribute("type", "button");
        dotNode.SetAttribute("data-mirage-indicator-dot", "true");
        dotNode.AddClass("mirage-indicator-dot");
        dotNode.SetStyle("boxSizing", "border-box");
        dotNode.SetStyle("padding", "0px");
        dotNode.SetStyle("margin", "0px");
        dotNode.SetStyle("border", "0");
        dotNode.SetStyle("display", "inline-block");
        dotNode.SetStyle("transition", "background-color 120ms ease, opacity 120ms ease");
        dotNode.SetEvent("click", enabled: true);

        var eventTarget = new IndicatorDotRuntimeEventTarget(this, index);
        RegisterDotNode(dotNode, eventTarget);
        PlatformView.AddChild(dotNode);
        _dotNodes.Add(new IndicatorDotNode(dotNode, eventTarget));
        return true;
    }

    private bool RemoveLastDotNode()
    {
        if (_dotNodes.Count == 0)
        {
            return false;
        }

        var dot = _dotNodes[^1];
        _dotNodes.RemoveAt(_dotNodes.Count - 1);
        PlatformView.RemoveChild(dot.PlatformView);
        if (Coordinator is { } coordinator)
        {
            coordinator.Unregister(dot.PlatformView);
        }

        return true;
    }

    private void RegisterDotNode(MiragePlatformView dotNode, IMirageRuntimeEventTarget eventTarget)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(dotNode, eventTarget);
            return;
        }

        if (!string.IsNullOrWhiteSpace(dotNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_dotNodeIdCounter);
        dotNode.AssignNodeId($"indicator-dot-{localId:D4}");
    }

    private static bool TryGetTemplateLayout(IIndicatorView indicator, out MauiILayout? templateLayout)
    {
        templateLayout = null;
        if (indicator is not ITemplatedIndicatorView templatedIndicatorView)
        {
            return false;
        }

        templateLayout = templatedIndicatorView.IndicatorsLayoutOverride;
        return templateLayout is not null;
    }

    private static int GetVisibleIndicatorCount(IIndicatorView indicator)
    {
        var count = Math.Max(0, indicator.Count);
        var maximumVisible = Math.Max(0, indicator.MaximumVisible);
        var visibleCount = Math.Min(count, maximumVisible);
        if (visibleCount == 1 && indicator.HideSingle)
        {
            return 0;
        }

        return visibleCount;
    }

    private static bool ResolveIsCircleShape(IIndicatorView indicator)
    {
        var shape = indicator.IndicatorsShape;
        if (shape is null)
        {
            return true;
        }

        return shape is Ellipse;
    }

    private static double NormalizeIndicatorSize(double indicatorSize)
    {
        if (!double.IsFinite(indicatorSize) || indicatorSize <= 0)
        {
            return DefaultIndicatorSize;
        }

        return Math.Clamp(indicatorSize, 2d, 96d);
    }

    private static string ResolvePaintCssColor(Paint? paint, string fallbackColor, string propertyName)
    {
        if (paint is SolidPaint solidPaint
            && MirageCommonViewProjection.ToCssColor(solidPaint.Color) is { } cssColor)
        {
            return cssColor;
        }

        if (paint is not null)
        {
            LogUnsupportedPaint(propertyName, paint);
        }

        return fallbackColor;
    }

    private static void LogUnsupportedPaint(string propertyName, Paint paint)
    {
        var paintType = paint.GetType().Name;
        var warningKey = $"{propertyName}:{paintType}";
        lock (UnsupportedPaintWarnings)
        {
            if (!UnsupportedPaintWarnings.Add(warningKey))
            {
                return;
            }
        }

        MirageDiagnostics.Log(
            "indicator.paint",
            $"property={propertyName} paintType={paintType} unsupported=true fallback=solid-default");
    }

    private void OnDotClicked(int dotIndex)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        var visibleCount = GetVisibleIndicatorCount(VirtualView);
        if (visibleCount <= 0)
        {
            return;
        }

        var nextPosition = Math.Clamp(dotIndex, 0, visibleCount - 1);
        if (VirtualView.Position == nextPosition)
        {
            return;
        }

        VirtualView.Position = nextPosition;
    }

    private static void NormalizeTemplateLayoutStyles(MiragePlatformView childPlatformView)
    {
        childPlatformView.SetStyle("position", "relative");
        childPlatformView.SetStyle("left", null);
        childPlatformView.SetStyle("top", null);
        childPlatformView.SetStyle("right", null);
        childPlatformView.SetStyle("bottom", null);
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        return SetStyleIfChanged(PlatformView, name, value);
    }

    private static bool SetStyleIfChanged(MiragePlatformView platformView, string name, string? value)
    {
        if (string.Equals(platformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        platformView.SetStyle(name, value);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView platformView, string name, string? value)
    {
        if (platformView.Attributes.TryGetValue(name, out var existing)
            && string.Equals(existing, value, StringComparison.Ordinal))
        {
            return false;
        }

        if (value is null && !platformView.Attributes.ContainsKey(name))
        {
            return false;
        }

        platformView.SetAttribute(name, value);
        return true;
    }

    private sealed class IndicatorDotNode
    {
        public IndicatorDotNode(MiragePlatformView platformView, IndicatorDotRuntimeEventTarget eventTarget)
        {
            PlatformView = platformView;
            EventTarget = eventTarget;
        }

        public MiragePlatformView PlatformView { get; }

        public IndicatorDotRuntimeEventTarget EventTarget { get; }
    }

    private sealed class IndicatorDotRuntimeEventTarget : IMirageRuntimeEventTarget
    {
        private readonly MirageIndicatorViewHandler _owner;

        public IndicatorDotRuntimeEventTarget(MirageIndicatorViewHandler owner, int index)
        {
            _owner = owner;
            Index = index;
        }

        public int Index { get; set; }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = payload;
            if (!string.Equals(eventName, "click", StringComparison.Ordinal))
            {
                return;
            }

            _owner.OnDotClicked(Index);
        }
    }
}
