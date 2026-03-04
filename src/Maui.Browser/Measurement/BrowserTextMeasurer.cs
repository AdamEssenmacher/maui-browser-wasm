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
        var textValue = text ?? string.Empty;
        var (measuredWidth, measuredHeight) = DomElement.MeasureText(textValue, fontSpec);
        var lineHeight = Math.Max(1, measuredHeight);

        var textWidthConstraint = GetTextWidthConstraint(widthConstraint, padding);
        if (textWidthConstraint.HasValue && textWidthConstraint.Value > 0)
        {
            measuredWidth = Math.Min(measuredWidth, textWidthConstraint.Value);
            measuredHeight = EstimateWrappedTextHeight(
                textValue,
                fontSpec,
                textWidthConstraint.Value,
                lineHeight);
        }

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

    private static double? GetTextWidthConstraint(double widthConstraint, Thickness padding)
    {
        if (double.IsNaN(widthConstraint) || double.IsPositiveInfinity(widthConstraint))
        {
            return null;
        }

        var paddedConstraint = Math.Max(0, widthConstraint - Math.Max(0, padding.Left) - Math.Max(0, padding.Right));
        return paddedConstraint;
    }

    private static double EstimateWrappedTextHeight(
        string text,
        string fontSpec,
        double maxTextWidth,
        double lineHeight)
    {
        if (maxTextWidth <= 0)
        {
            return lineHeight;
        }

        if (string.IsNullOrEmpty(text))
        {
            return lineHeight;
        }

        var widthCache = new Dictionary<string, double>(StringComparer.Ordinal);

        double MeasureWidth(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            if (widthCache.TryGetValue(value, out var cached))
            {
                return cached;
            }

            var (width, _) = DomElement.MeasureText(value, fontSpec);
            widthCache[value] = width;
            return width;
        }

        var totalLines = 0;
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var paragraphs = normalized.Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length == 0)
            {
                totalLines++;
                continue;
            }

            var currentLineWidth = 0d;
            var words = paragraph.Split(' ');

            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var candidate = i == 0 ? word : $" {word}";
                var candidateWidth = MeasureWidth(candidate);

                if (currentLineWidth + candidateWidth <= maxTextWidth)
                {
                    currentLineWidth += candidateWidth;
                    continue;
                }

                if (currentLineWidth > 0)
                {
                    totalLines++;
                }

                var wordWidth = MeasureWidth(word);
                if (wordWidth <= maxTextWidth)
                {
                    currentLineWidth = wordWidth;
                    continue;
                }

                var wrappedLineCount = Math.Ceiling(wordWidth / maxTextWidth);
                totalLines += (int)Math.Max(0, wrappedLineCount - 1);
                currentLineWidth = wordWidth - Math.Max(0, wrappedLineCount - 1) * maxTextWidth;
                currentLineWidth = Math.Min(maxTextWidth, Math.Max(0, currentLineWidth));
            }

            totalLines++;
        }

        return Math.Max(lineHeight, totalLines * lineHeight);
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
