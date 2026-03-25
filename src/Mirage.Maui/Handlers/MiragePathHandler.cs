using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePathHandler : MirageShapeViewHandler
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
        nameof(MauiPath.Data),
        nameof(MauiPath.RenderTransform)
    };

    public static new IPropertyMapper<MauiPath, MiragePathHandler> Mapper =
        new PropertyMapper<MauiPath, MiragePathHandler>(MirageShapeViewHandler.Mapper)
        {
            [nameof(IShapeView.Shape)] = MapShape,
            [nameof(MauiPath.Data)] = MapData,
            [nameof(MauiPath.RenderTransform)] = MapRenderTransform
        };

    public MiragePathHandler()
        : base(Mapper)
    {
    }

    public static void MapShape(MiragePathHandler handler, MauiPath path)
    {
        handler.MapShapeFromDerived(path);
    }

    public static void MapData(MiragePathHandler handler, MauiPath path)
    {
        handler.MapShapeFromDerived(path);
    }

    public static void MapRenderTransform(MiragePathHandler handler, MauiPath path)
    {
        handler.MapShapeFromDerived(path);
    }
}
