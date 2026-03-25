using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRoundRectangleHandler : MirageShapeViewHandler
{
    internal new static readonly string[] ParityPropertyKeys =
    {
        nameof(IShapeView.Background),
        nameof(IShapeView.Shape),
        nameof(IShapeView.Aspect),
        nameof(IShapeView.Fill),
        nameof(IShapeView.Stroke),
        nameof(IShapeView.StrokeThickness),
        nameof(IShapeView.StrokeDashPattern),
        nameof(IShapeView.StrokeDashOffset),
        nameof(IShapeView.StrokeLineCap),
        nameof(IShapeView.StrokeLineJoin),
        nameof(IShapeView.StrokeMiterLimit),
        nameof(RoundRectangle.CornerRadius)
    };

    public static new IPropertyMapper<RoundRectangle, MirageRoundRectangleHandler> Mapper =
        new PropertyMapper<RoundRectangle, MirageRoundRectangleHandler>(MirageShapeViewHandler.Mapper)
        {
            [nameof(RoundRectangle.CornerRadius)] = MapCornerRadius
        };

    public MirageRoundRectangleHandler()
        : base(Mapper)
    {
    }

    public static void MapCornerRadius(MirageRoundRectangleHandler handler, RoundRectangle roundRectangle)
    {
        handler.MapShapeFromDerived(roundRectangle);
    }
}
