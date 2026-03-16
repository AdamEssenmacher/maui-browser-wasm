using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageButtonHandler : MirageViewHandler<Button>, IMirageRuntimeEventTarget
{
    private const int MinimumTruncationBudget = 6;
    private const int DefaultTruncationBudget = 56;
    private const double DefaultImageExtent = 20;
    private IMirageImageSourceResolver? _imageSourceResolver;

    public static IPropertyMapper<Button, MirageButtonHandler> Mapper =
        new PropertyMapper<Button, MirageButtonHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Button.Text)] = MapText,
            [nameof(Button.ImageSource)] = MapImageSource,
            [nameof(Button.ContentLayout)] = MapContentLayout,
            [nameof(Button.LineBreakMode)] = MapLineBreakMode,
            [nameof(Button.Padding)] = MapContentPadding,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(IView.Background)] = MapBackground,
            ["TextColor"] = MapTextColor,
            ["BackgroundColor"] = MapBackground,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests,
            [nameof(IButtonStroke.StrokeColor)] = MapStrokeColor,
            [nameof(IButtonStroke.StrokeThickness)] = MapStrokeThickness,
            [nameof(IButtonStroke.CornerRadius)] = MapCornerRadius,
            ["BorderColor"] = MapStrokeColor,
            ["BorderWidth"] = MapStrokeThickness,
            ["ToolTipProperties.Text"] = MapToolTipText
        };

    public MirageButtonHandler()
        : base(ComponentKind.Button, Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "inline-flex");
        PlatformView.SetStyle("alignItems", "center");
        PlatformView.SetStyle("justifyContent", "center");
        PlatformView.SetStyle("position", "relative");
        PlatformView.SetStyle("boxSizing", "border-box");
        PlatformView.SetStyle("cursor", VirtualView.IsEnabled ? "pointer" : "default");
        PlatformView.SetEvent("click", enabled: true);
        UpdateProjectedText(VirtualView);
        UpdateImageProjection(VirtualView);
        UpdateContentLayoutProjection(VirtualView);
        UpdatePaddingProjection(VirtualView);
        UpdateBackgroundStyles(VirtualView);
        UpdateTextColorStyles(VirtualView);
        UpdateStrokeStyles(VirtualView);
        UpdateCornerRadius(VirtualView);
        UpdateToolTipProjection(VirtualView);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        if (!IsTruncationMode(VirtualView.LineBreakMode))
        {
            return;
        }

        if (!UpdateProjectedText(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;
        if (!string.Equals(eventName, "click", StringComparison.Ordinal))
        {
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (VirtualView is IButton buttonView)
        {
            buttonView.Clicked();
        }
    }

    public static void MapText(MirageButtonHandler handler, Button button)
    {
        var changed = handler.UpdateProjectedText(button);
        changed |= handler.UpdateImageProjection(button);
        changed |= handler.UpdateContentLayoutProjection(button);
        changed |= handler.UpdateToolTipProjection(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapImageSource(MirageButtonHandler handler, Button button)
    {
        var changed = handler.UpdateImageProjection(button);
        changed |= handler.UpdateProjectedText(button);
        changed |= handler.UpdateContentLayoutProjection(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapContentLayout(MirageButtonHandler handler, Button button)
    {
        var changed = handler.UpdateContentLayoutProjection(button);
        changed |= handler.UpdateProjectedText(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapLineBreakMode(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateProjectedText(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapContentPadding(MirageButtonHandler handler, Button button)
    {
        var changed = handler.UpdatePaddingProjection(button);
        changed |= handler.UpdateProjectedText(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapFont(MirageButtonHandler handler, Button button)
    {
        var changed = MirageCommonViewProjection.MapFont(handler, button);
        changed |= handler.UpdateProjectedText(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCharacterSpacing(MirageButtonHandler handler, Button button)
    {
        var changed = MirageCommonViewProjection.MapCharacterSpacing(handler, button);
        changed |= handler.UpdateProjectedText(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageButtonHandler handler, Button button)
    {
        var changed = handler.SetStyleIfChanged("cursor", button.IsEnabled ? "pointer" : "default");
        changed |= handler.SetStyleIfChanged("pointerEvents", button.IsEnabled ? "auto" : "none");
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequests(MirageButtonHandler handler, Button button)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, button);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, button);
        changed |= handler.UpdateProjectedText(button);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapBackground(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateBackgroundStyles(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTextColor(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateTextColorStyles(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeColor(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateStrokeStyles(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeThickness(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateStrokeStyles(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCornerRadius(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateCornerRadius(button))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapToolTipText(MirageButtonHandler handler, Button button)
    {
        if (!handler.UpdateToolTipProjection(button))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateStrokeStyles(Button button)
    {
        if (button is not IButtonStroke strokedButton)
        {
            var cleared = SetStyleIfChanged("borderStyle", null);
            cleared |= SetStyleIfChanged("borderWidth", null);
            cleared |= SetStyleIfChanged("borderColor", null);
            return cleared;
        }

        var strokeThickness = strokedButton.StrokeThickness;
        var normalizedThickness = double.IsFinite(strokeThickness) && strokeThickness > 0
            ? strokeThickness
            : 0;

        if (normalizedThickness <= 0)
        {
            var cleared = SetStyleIfChanged("borderStyle", null);
            cleared |= SetStyleIfChanged("borderWidth", null);
            cleared |= SetStyleIfChanged("borderColor", null);
            return cleared;
        }

        var strokeColor = MirageCommonViewProjection.ToCssColor(strokedButton.StrokeColor) ?? "currentColor";
        var changed = SetStyleIfChanged("borderStyle", "solid");
        changed |= SetStyleIfChanged("borderWidth", MirageViewMapper.ToPx(normalizedThickness));
        changed |= SetStyleIfChanged("borderColor", strokeColor);
        return changed;
    }

    private bool UpdateBackgroundStyles(Button button)
    {
        return MirageCommonViewProjection.MapBackground(this, button);
    }

    private bool UpdateTextColorStyles(Button button)
    {
        var textColor = MirageCommonViewProjection.ToCssColor(button.TextColor);

        return SetStyleIfChanged("color", textColor);
    }

    private bool UpdateCornerRadius(Button button)
    {
        if (button is not IButtonStroke strokedButton)
        {
            return SetStyleIfChanged("borderRadius", null);
        }

        var cornerRadius = strokedButton.CornerRadius;
        if (!double.IsFinite(cornerRadius) || cornerRadius < 0)
        {
            return SetStyleIfChanged("borderRadius", null);
        }

        var radius = MirageViewMapper.ToPx(cornerRadius);
        return SetStyleIfChanged("borderRadius", radius);
    }

    private bool UpdatePaddingProjection(Button button)
    {
        return SetStyleIfChanged("padding", MirageCommonViewProjection.ToPaddingThickness(button.Padding));
    }

    private bool UpdateToolTipProjection(Button button)
    {
        var tooltipText = ToolTipProperties.GetText(button) as string;
        return SetPropertyIfChanged("TooltipText", string.IsNullOrWhiteSpace(tooltipText) ? null : tooltipText);
    }

    private bool UpdateImageProjection(Button button)
    {
        var projection = MirageImageSourceProjection.BuildProjection(button.ImageSource, GetImageSourceResolver(), button);
        var hasImage = !string.IsNullOrWhiteSpace(projection.SourceValue);
        var changed = SetPropertyIfChanged("ImageSource", projection.SourceValue);
        changed |= SetPropertyIfChanged("ImageDisplay", hasImage ? "block" : "none");
        changed |= SetPropertyIfChanged("ImageAlt", string.IsNullOrWhiteSpace(button.Text) ? "button image" : button.Text);
        changed |= SetPropertyIfChanged("ImageWidth", hasImage ? MirageViewMapper.ToPx(DefaultImageExtent) : null);
        changed |= SetPropertyIfChanged("ImageHeight", hasImage ? MirageViewMapper.ToPx(DefaultImageExtent) : null);
        changed |= SetPropertyIfChanged("ImageObjectFit", hasImage ? "contain" : null);
        changed |= SetPropertyIfChanged("ImagePointerEvents", hasImage ? "none" : null);
        return changed;
    }

    private bool UpdateContentLayoutProjection(Button button)
    {
        var hasImage = !string.IsNullOrWhiteSpace(PlatformView.GetPropertyString("ImageSource"));
        var hasText = !string.IsNullOrWhiteSpace(PlatformView.GetPropertyString("Text"));
        var contentLayout = button.ContentLayout ?? new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 10);

        var flexDirection = contentLayout.Position switch
        {
            Button.ButtonContentLayout.ImagePosition.Right => "row-reverse",
            Button.ButtonContentLayout.ImagePosition.Top => "column",
            Button.ButtonContentLayout.ImagePosition.Bottom => "column-reverse",
            _ => "row"
        };

        var spacing = double.IsFinite(contentLayout.Spacing)
            ? contentLayout.Spacing
            : 0;

        var changed = SetStyleIfChanged("flexDirection", flexDirection);
        changed |= SetStyleIfChanged("alignItems", "center");
        changed |= SetStyleIfChanged("justifyContent", "center");
        changed |= SetPropertyIfChanged("TextDisplay", hasText ? "inline" : "none");

        var textMarginTop = "0px";
        var textMarginRight = "0px";
        var textMarginBottom = "0px";
        var textMarginLeft = "0px";
        if (hasImage && hasText && Math.Abs(spacing) > 0)
        {
            var spacingValue = MirageCommonViewProjection.ToPxNoClamp(spacing);
            switch (contentLayout.Position)
            {
                case Button.ButtonContentLayout.ImagePosition.Right:
                    textMarginRight = spacingValue;
                    break;
                case Button.ButtonContentLayout.ImagePosition.Top:
                    textMarginTop = spacingValue;
                    break;
                case Button.ButtonContentLayout.ImagePosition.Bottom:
                    textMarginBottom = spacingValue;
                    break;
                default:
                    textMarginLeft = spacingValue;
                    break;
            }
        }

        changed |= SetPropertyIfChanged("TextMarginTop", textMarginTop);
        changed |= SetPropertyIfChanged("TextMarginRight", textMarginRight);
        changed |= SetPropertyIfChanged("TextMarginBottom", textMarginBottom);
        changed |= SetPropertyIfChanged("TextMarginLeft", textMarginLeft);
        return changed;
    }

    private bool UpdateProjectedText(Button button)
    {
        var text = button.Text ?? string.Empty;
        var hasImage = button.ImageSource is not null;
        var projectedText = text;

        if (text.Length > 0 && IsTruncationMode(button.LineBreakMode))
        {
            var budget = ResolveTruncationBudget(button, PlatformView, text.Length, hasImage);
            projectedText = ApplyTruncation(text, button.LineBreakMode, budget);
        }

        var changed = SetPropertyIfChanged("Text", projectedText);
        changed |= SetPropertyIfChanged("TextDisplay", text.Length == 0 ? "none" : "inline");
        changed |= SetPropertyIfChanged("TextWhiteSpace", ResolveTextWhiteSpace(button.LineBreakMode));
        changed |= SetPropertyIfChanged("TextOverflow", ResolveTextOverflow(button.LineBreakMode));
        changed |= SetPropertyIfChanged("TextOverflowStyle", ResolveTextOverflowStyle(button.LineBreakMode));
        changed |= SetPropertyIfChanged("TextWordBreak", ResolveTextWordBreak(button.LineBreakMode));
        changed |= SetPropertyIfChanged("TextOverflowWrap", ResolveTextOverflowWrap(button.LineBreakMode));
        return changed;
    }

    private static bool IsTruncationMode(LineBreakMode mode)
    {
        return mode is LineBreakMode.HeadTruncation or LineBreakMode.MiddleTruncation or LineBreakMode.TailTruncation;
    }

    private static int ResolveTruncationBudget(Button button, MiragePlatformView platformView, int textLength, bool hasImage)
    {
        var contentLayout = button.ContentLayout ?? new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 10);
        var width = ResolveProjectedWidth(button, platformView);
        if (width <= 0)
        {
            return textLength;
        }

        var horizontalPadding = 0d;
        if (double.IsFinite(button.Padding.Left))
        {
            horizontalPadding += Math.Max(0, button.Padding.Left);
        }

        if (double.IsFinite(button.Padding.Right))
        {
            horizontalPadding += Math.Max(0, button.Padding.Right);
        }

        width = Math.Max(0, width - horizontalPadding);
        if (hasImage && contentLayout.Position is Button.ButtonContentLayout.ImagePosition.Left or Button.ButtonContentLayout.ImagePosition.Right)
        {
            var spacing = double.IsFinite(contentLayout.Spacing) ? contentLayout.Spacing : 0;
            width -= DefaultImageExtent + Math.Max(0, spacing);
        }

        var textStyle = (ITextStyle)button;
        var fontSize = double.IsFinite(textStyle.Font.Size) && textStyle.Font.Size > 0
            ? textStyle.Font.Size
            : 14;
        var charWidth = Math.Max(5d, (fontSize * 0.56) + Math.Max(0, button.CharacterSpacing) * 0.15);
        var estimated = (int)Math.Floor(width / charWidth);
        var clamped = Math.Clamp(estimated, MinimumTruncationBudget, textLength);
        return clamped;
    }

    private static double ResolveProjectedWidth(Button button, MiragePlatformView platformView)
    {
        if (double.IsFinite(button.WidthRequest) && button.WidthRequest > 0)
        {
            return button.WidthRequest;
        }

        if (TryParsePx(platformView.GetStyleValue("width"), out var projectedWidth) && projectedWidth > 0)
        {
            return projectedWidth;
        }

        if (TryParsePx(platformView.GetStyleValue("maxWidth"), out var projectedMaxWidth) && projectedMaxWidth > 0)
        {
            return projectedMaxWidth;
        }

        return 0;
    }

    private static bool TryParsePx(string? styleValue, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(styleValue))
        {
            return false;
        }

        var trimmed = styleValue.Trim();
        if (!trimmed.EndsWith("px", StringComparison.Ordinal))
        {
            return false;
        }

        var numeric = trimmed[..^2];
        return double.TryParse(numeric, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static string ApplyTruncation(string text, LineBreakMode mode, int budget)
    {
        if (text.Length <= budget || budget <= 0)
        {
            return text;
        }

        if (budget == 1)
        {
            return "…";
        }

        return mode switch
        {
            LineBreakMode.HeadTruncation => $"…{text[^Math.Min(text.Length, budget - 1)..]}",
            LineBreakMode.MiddleTruncation => ApplyMiddleTruncation(text, budget),
            _ => $"{text[..Math.Min(text.Length, budget - 1)]}…"
        };
    }

    private static string ApplyMiddleTruncation(string text, int budget)
    {
        if (budget <= 1)
        {
            return "…";
        }

        var available = budget - 1;
        var leftCount = available / 2;
        var rightCount = available - leftCount;
        var left = text[..Math.Min(text.Length, leftCount)];
        var right = rightCount <= 0 ? string.Empty : text[^Math.Min(text.Length - left.Length, rightCount)..];
        return $"{left}…{right}";
    }

    private static string ResolveTextWhiteSpace(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.WordWrap or LineBreakMode.CharacterWrap => "normal",
            _ => "nowrap"
        };
    }

    private static string ResolveTextOverflow(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.WordWrap or LineBreakMode.CharacterWrap => "visible",
            _ => "hidden"
        };
    }

    private static string ResolveTextOverflowStyle(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.TailTruncation => "ellipsis",
            _ => "clip"
        };
    }

    private static string ResolveTextWordBreak(LineBreakMode mode)
    {
        return mode == LineBreakMode.CharacterWrap ? "break-all" : "normal";
    }

    private static string ResolveTextOverflowWrap(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.CharacterWrap => "anywhere",
            LineBreakMode.WordWrap => "break-word",
            _ => "normal"
        };
    }

    private IMirageImageSourceResolver? GetImageSourceResolver()
    {
        if (_imageSourceResolver is not null)
        {
            return _imageSourceResolver;
        }

        _imageSourceResolver = MauiContext?.Services.GetService<IMirageImageSourceResolver>();
        return _imageSourceResolver;
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

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }
}
