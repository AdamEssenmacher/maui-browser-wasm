using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRectangleHandler : MirageShapeViewHandler
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
        nameof(Rectangle.RadiusX),
        nameof(Rectangle.RadiusY)
    };

    public static new IPropertyMapper<Rectangle, MirageRectangleHandler> Mapper =
        new PropertyMapper<Rectangle, MirageRectangleHandler>(MirageShapeViewHandler.Mapper)
        {
            [nameof(Rectangle.RadiusX)] = MapRadiusX,
            [nameof(Rectangle.RadiusY)] = MapRadiusY
        };

    public MirageRectangleHandler()
        : base(Mapper)
    {
    }

    public static void MapRadiusX(MirageRectangleHandler handler, Rectangle rectangle)
    {
        handler.MapShapeFromDerived(rectangle);
    }

    public static void MapRadiusY(MirageRectangleHandler handler, Rectangle rectangle)
    {
        handler.MapShapeFromDerived(rectangle);
    }
}
