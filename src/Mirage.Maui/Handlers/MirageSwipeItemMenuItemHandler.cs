using Mirage.Maui.Images;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Mirage.Maui.Handlers;

internal sealed class MirageSwipeItemMenuItemHandler
{
    private const string DefaultBackgroundCss = "rgba(30,41,59,0.94)";
    private const string DefaultTextCss = "rgba(255,255,255,1)";

    private readonly SwipeItem _item;
    private readonly MiragePlatformView _rootNode;
    private readonly MiragePlatformView _iconNode;
    private readonly MiragePlatformView _textNode;

    public MirageSwipeItemMenuItemHandler(SwipeItem item, string side, int index)
    {
        _item = item;
        _rootNode = MiragePlatformView.CreateWeb("div");
        _iconNode = MiragePlatformView.CreateWeb("img");
        _textNode = MiragePlatformView.CreateWeb("span");

        _rootNode.SetAttribute("data-maui-sv-action-wrapper", "true");
        _rootNode.SetAttribute("data-maui-sv-action-kind", "menu");
        _rootNode.SetAttribute("data-maui-sv-action-side", side);
        _rootNode.SetAttribute("data-maui-sv-action-index", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _rootNode.SetAttribute("role", "button");
        _rootNode.SetAttribute("tabindex", "-1");

        _iconNode.SetAttribute("alt", string.Empty);
        _iconNode.SetAttribute("aria-hidden", "true");
        _iconNode.SetStyle("pointerEvents", "none");
        _textNode.SetStyle("pointerEvents", "none");

        _rootNode.AddChild(_iconNode);
        _rootNode.AddChild(_textNode);
    }

    public SwipeItem Item => _item;

    public MiragePlatformView RootNode => _rootNode;

    public IReadOnlyList<MiragePlatformView> AllNodes => [_rootNode, _iconNode, _textNode];

    public bool IsVisible => _item.IsVisible;

    public bool IsEnabled => _item.IsEnabled;

    public bool Update(IMirageImageSourceResolver? imageSourceResolver, bool isHorizontal)
    {
        var changed = SetAttributeIfChanged(_rootNode, "data-maui-sv-action-visible", IsVisible ? "true" : "false");
        changed |= SetAttributeIfChanged(_rootNode, "data-maui-sv-action-enabled", IsEnabled ? "true" : "false");
        changed |= SetStyleIfChanged(_rootNode, "display", IsVisible ? "flex" : "none");
        changed |= SetStyleIfChanged(_rootNode, "flexDirection", isHorizontal ? "column" : "row");
        changed |= SetStyleIfChanged(_rootNode, "alignItems", "center");
        changed |= SetStyleIfChanged(_rootNode, "justifyContent", "center");
        changed |= SetStyleIfChanged(_rootNode, "gap", "6px");
        changed |= SetStyleIfChanged(_rootNode, "padding", isHorizontal ? "10px 8px" : "8px 12px");
        changed |= SetStyleIfChanged(_rootNode, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(_rootNode, "overflow", "hidden");
        changed |= SetStyleIfChanged(_rootNode, "userSelect", "none");
        changed |= SetStyleIfChanged(_rootNode, "cursor", IsEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged(_rootNode, "opacity", IsEnabled ? "1" : "0.55");
        changed |= SetStyleIfChanged(_rootNode, "backgroundColor", ResolveBackgroundColorCss(_item.BackgroundColor));
        changed |= SetStyleIfChanged(_rootNode, "color", DefaultTextCss);

        var projection = MirageImageSourceProjection.BuildProjection(_item.IconImageSource, imageSourceResolver);
        changed |= SetAttributeIfChanged(_iconNode, "src", projection.SourceValue);
        changed |= SetStyleIfChanged(_iconNode, "display", projection.SourceValue is null ? "none" : "block");
        changed |= SetStyleIfChanged(_iconNode, "width", isHorizontal ? "20px" : "18px");
        changed |= SetStyleIfChanged(_iconNode, "height", isHorizontal ? "20px" : "18px");
        changed |= SetStyleIfChanged(_iconNode, "objectFit", "contain");

        changed |= SetPropertyIfChanged(_textNode, "textContent", _item.Text ?? string.Empty);
        changed |= SetStyleIfChanged(_textNode, "display", string.IsNullOrWhiteSpace(_item.Text) ? "none" : "block");
        changed |= SetStyleIfChanged(_textNode, "fontSize", "12px");
        changed |= SetStyleIfChanged(_textNode, "lineHeight", "1.2");
        changed |= SetStyleIfChanged(_textNode, "textAlign", "center");
        changed |= SetStyleIfChanged(_textNode, "whiteSpace", "normal");
        return changed;
    }

    private static string ResolveBackgroundColorCss(Color? color)
    {
        return color is null ? DefaultBackgroundCss : MirageCommonViewProjection.ToCssColor(color);
    }

    private static bool SetPropertyIfChanged(MiragePlatformView node, string propertyName, string? value)
    {
        if (string.Equals(node.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetProperty(propertyName, value);
        return true;
    }

    private static bool SetStyleIfChanged(MiragePlatformView node, string styleName, string? value)
    {
        if (string.Equals(node.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetStyle(styleName, value);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView node, string attributeName, string? value)
    {
        node.Attributes.TryGetValue(attributeName, out var currentValue);
        if (string.Equals(currentValue, value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetAttribute(attributeName, value);
        return true;
    }
}
