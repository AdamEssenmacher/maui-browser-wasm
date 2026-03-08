using System.Globalization;

namespace Mirage.Core.Semantics;

public readonly record struct MirageThickness(double Left, double Top, double Right, double Bottom)
{
    public static MirageThickness Uniform(double value) => new(value, value, value, value);

    public string ToCssString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Format(Top)} {Format(Right)} {Format(Bottom)} {Format(Left)}");
    }

    private static string Format(double value)
    {
        var normalized = double.IsFinite(value) ? Math.Max(0, value) : 0;
        return string.Create(CultureInfo.InvariantCulture, $"{normalized:0.###}px");
    }
}
