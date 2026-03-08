using Mirage.Web.Dom;

namespace Mirage.Web.Runtime;

public interface IBrowserTextMeasurer
{
    (double Width, double Height) Measure(string? text, string? fontCss);
}

public sealed class BrowserTextMeasurer : IBrowserTextMeasurer
{
    public (double Width, double Height) Measure(string? text, string? fontCss)
    {
        return DomElement.MeasureText(text, fontCss);
    }
}
