using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Measurement;

internal interface IBrowserTextMeasurer
{
    Size MeasureText(
        string? text,
        Microsoft.Maui.Font font,
        double widthConstraint,
        double heightConstraint,
        Thickness padding,
        double minWidth = 0,
        double minHeight = 0);
}
