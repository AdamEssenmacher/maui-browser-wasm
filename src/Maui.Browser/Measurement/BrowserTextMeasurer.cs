using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Measurement;

internal sealed class BrowserTextMeasurer : IBrowserTextMeasurer
{
    private const double TextHeightSafetyPadding = 1;

    public Size MeasureText(
        string? text,
        Microsoft.Maui.Font font,
        double widthConstraint,
        double heightConstraint,
        Thickness padding,
        double minWidth = 0,
        double minHeight = 0)
    {
        var fontSpec = CssUtil.ToCssFont(font);
        var (measuredWidth, measuredHeight) = DomElement.MeasureText(text ?? string.Empty, fontSpec);

        var paddedWidth = measuredWidth + Math.Max(0, padding.Left) + Math.Max(0, padding.Right);
        var paddedHeight = measuredHeight + Math.Max(0, padding.Top) + Math.Max(0, padding.Bottom) + TextHeightSafetyPadding;

        var width = Math.Max(minWidth, paddedWidth);
        var height = Math.Max(minHeight, paddedHeight);

        width = Math.Ceiling(width);
        height = Math.Ceiling(height);

        width = ClampToConstraint(width, widthConstraint);
        height = ClampToConstraint(height, heightConstraint);

        return new Size(width, height);
    }

    private static double ClampToConstraint(double measured, double constraint)
    {
        if (double.IsNaN(measured))
        {
            return 0;
        }

        if (double.IsPositiveInfinity(constraint) || double.IsNaN(constraint))
        {
            return Math.Max(0, measured);
        }

        return Math.Max(0, Math.Min(measured, Math.Max(0, constraint)));
    }
}
