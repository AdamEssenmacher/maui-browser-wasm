using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLineHandler : MirageShapeViewHandler
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
        nameof(Line.X1),
        nameof(Line.Y1),
        nameof(Line.X2),
        nameof(Line.Y2)
    };

    public static new IPropertyMapper<Line, MirageLineHandler> Mapper =
        new PropertyMapper<Line, MirageLineHandler>(MirageShapeViewHandler.Mapper)
        {
            [nameof(Line.X1)] = MapX1,
            [nameof(Line.Y1)] = MapY1,
            [nameof(Line.X2)] = MapX2,
            [nameof(Line.Y2)] = MapY2
        };

    public MirageLineHandler()
        : base(Mapper)
    {
    }

    public static void MapX1(MirageLineHandler handler, Line line)
    {
        handler.MapShapeFromDerived(line);
    }

    public static void MapY1(MirageLineHandler handler, Line line)
    {
        handler.MapShapeFromDerived(line);
    }

    public static void MapX2(MirageLineHandler handler, Line line)
    {
        handler.MapShapeFromDerived(line);
    }

    public static void MapY2(MirageLineHandler handler, Line line)
    {
        handler.MapShapeFromDerived(line);
    }
}
