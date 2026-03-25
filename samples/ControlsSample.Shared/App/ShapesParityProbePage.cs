using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace ControlsSample.Shared.App;

public sealed class ShapesParityProbePage : ContentPage
{
    private const double ProbeWidth = 320;
    private const double ProbeHeight = 220;

    private readonly Grid _shapeHost;
    private readonly Label _shapeKindLabel;
    private readonly Label _styleStateLabel;
    private readonly Label _geometryStateLabel;

    private Shape? _currentShape;
    private int _shapeIndex;
    private int _aspectIndex;
    private int _fillModeIndex;
    private int _strokeModeIndex;
    private int _lineCapIndex;
    private int _lineJoinIndex;
    private bool _useDashPattern;
    private bool _useHighMiterLimit;
    private bool _useAlternatePoints;
    private bool _useAlternatePathData;
    private bool _usePathRenderTransform;

    private readonly Brush? _imageBrush;

    private static readonly ProbeShapeKind[] ShapeKinds =
    [
        ProbeShapeKind.Ellipse,
        ProbeShapeKind.Line,
        ProbeShapeKind.Path,
        ProbeShapeKind.Polygon,
        ProbeShapeKind.Polyline,
        ProbeShapeKind.Rectangle,
        ProbeShapeKind.RoundRectangle
    ];

    private static readonly Stretch[] AspectModes =
    [
        Stretch.Fill,
        Stretch.Uniform,
        Stretch.UniformToFill,
        Stretch.None
    ];

    private static readonly PenLineCap[] LineCaps =
    [
        PenLineCap.Flat,
        PenLineCap.Round,
        PenLineCap.Square
    ];

    private static readonly PenLineJoin[] LineJoins =
    [
        PenLineJoin.Miter,
        PenLineJoin.Bevel,
        PenLineJoin.Round
    ];

    public ShapesParityProbePage()
    {
        Title = "Shapes Parity Probe";
        AutomationId = "controls-sample-page-shapesparitypage";

        _imageBrush = TryCreateImageBrush("dotnet_bot.png");

        _shapeHost = new Grid
        {
            AutomationId = "controls-sample-shapes-parity-host",
            WidthRequest = ProbeWidth,
            HeightRequest = ProbeHeight,
            BackgroundColor = Color.FromArgb("#F3F4F6")
        };

        _shapeKindLabel = new Label
        {
            AutomationId = "controls-sample-shapes-parity-shape-kind",
            FontAttributes = FontAttributes.Bold
        };

        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-shapes-parity-style-state"
        };

        _geometryStateLabel = new Label
        {
            AutomationId = "controls-sample-shapes-parity-geometry-state"
        };

        RebuildShape();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        AutomationId = "controls-sample-shapes-parity-description",
                        Text = "Shapes parity probe: switch shape rows, aspect, paints, stroke options, and runtime geometry mutations."
                    },
                    new Border
                    {
                        StrokeThickness = 1,
                        Stroke = Brush.LightGray,
                        Padding = 8,
                        BackgroundColor = Colors.White,
                        Content = _shapeHost
                    },
                    _shapeKindLabel,
                    _styleStateLabel,
                    _geometryStateLabel,
                    BuildControlRows()
                }
            }
        };
    }

    private View BuildControlRows()
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Next Shape", "controls-sample-shapes-parity-next-shape", NextShape),
                        CreateButton("Toggle Aspect", "controls-sample-shapes-parity-toggle-aspect", ToggleAspect),
                        CreateButton("Toggle Fill", "controls-sample-shapes-parity-toggle-fill", ToggleFillMode),
                        CreateButton("Toggle Stroke", "controls-sample-shapes-parity-toggle-stroke", ToggleStrokeMode)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Dash", "controls-sample-shapes-parity-toggle-dash", ToggleDashPattern),
                        CreateButton("Toggle Cap", "controls-sample-shapes-parity-toggle-linecap", ToggleLineCap),
                        CreateButton("Toggle Join", "controls-sample-shapes-parity-toggle-linejoin", ToggleLineJoin),
                        CreateButton("Toggle Miter", "controls-sample-shapes-parity-toggle-miter", ToggleMiterLimit)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Mutate Points", "controls-sample-shapes-parity-mutate-points", MutatePoints),
                        CreateButton("Mutate Path Data", "controls-sample-shapes-parity-mutate-path-data", MutatePathData),
                        CreateButton("Mutate Path Transform", "controls-sample-shapes-parity-mutate-path-transform", MutatePathRenderTransform)
                    }
                }
            }
        };
    }

    private static Button CreateButton(string text, string automationId, Action action)
    {
        return new Button
        {
            Text = text,
            AutomationId = automationId,
            Command = new Command(action)
        };
    }

    private void NextShape()
    {
        _shapeIndex = (_shapeIndex + 1) % ShapeKinds.Length;
        RebuildShape();
    }

    private void ToggleAspect()
    {
        _aspectIndex = (_aspectIndex + 1) % AspectModes.Length;
        ApplyStyleToCurrentShape();
    }

    private void ToggleFillMode()
    {
        _fillModeIndex = (_fillModeIndex + 1) % Enum.GetValues<PaintMode>().Length;
        ApplyStyleToCurrentShape();
    }

    private void ToggleStrokeMode()
    {
        _strokeModeIndex = (_strokeModeIndex + 1) % Enum.GetValues<PaintMode>().Length;
        ApplyStyleToCurrentShape();
    }

    private void ToggleDashPattern()
    {
        _useDashPattern = !_useDashPattern;
        ApplyStyleToCurrentShape();
    }

    private void ToggleLineCap()
    {
        _lineCapIndex = (_lineCapIndex + 1) % LineCaps.Length;
        ApplyStyleToCurrentShape();
    }

    private void ToggleLineJoin()
    {
        _lineJoinIndex = (_lineJoinIndex + 1) % LineJoins.Length;
        ApplyStyleToCurrentShape();
    }

    private void ToggleMiterLimit()
    {
        _useHighMiterLimit = !_useHighMiterLimit;
        ApplyStyleToCurrentShape();
    }

    private void MutatePoints()
    {
        _useAlternatePoints = !_useAlternatePoints;

        switch (_currentShape)
        {
            case Polygon polygon:
                UpdatePointCollection(
                    polygon.Points,
                    _useAlternatePoints ? GetPolygonAlternatePoints() : GetPolygonDefaultPoints());
                break;
            case Polyline polyline:
                UpdatePointCollection(
                    polyline.Points,
                    _useAlternatePoints ? GetPolylineAlternatePoints() : GetPolylineDefaultPoints());
                break;
        }

        UpdateStateLabels();
    }

    private void MutatePathData()
    {
        if (_currentShape is MauiPath path)
        {
            _useAlternatePathData = !_useAlternatePathData;
            path.Data = BuildPathData(_useAlternatePathData);
            UpdateStateLabels();
        }
    }

    private void MutatePathRenderTransform()
    {
        if (_currentShape is MauiPath path)
        {
            _usePathRenderTransform = !_usePathRenderTransform;
            path.RenderTransform = _usePathRenderTransform
                ? new TranslateTransform
                {
                    X = 16,
                    Y = 8
                }
                : null;
            UpdateStateLabels();
        }
    }

    private void RebuildShape()
    {
        _shapeHost.Children.Clear();

        _currentShape = CreateShape(ShapeKinds[_shapeIndex]);
        _shapeHost.Children.Add(_currentShape);
        _shapeHost.InvalidateMeasure();

        UpdateStateLabels();
    }

    private Shape CreateShape(ProbeShapeKind shapeKind)
    {
        Shape shape = shapeKind switch
        {
            ProbeShapeKind.Ellipse => new Ellipse(),
            ProbeShapeKind.Line => new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = ProbeWidth,
                Y2 = ProbeHeight
            },
            ProbeShapeKind.Path => new MauiPath
            {
                Data = BuildPathData(_useAlternatePathData),
                RenderTransform = _usePathRenderTransform
                    ? new TranslateTransform
                    {
                        X = 16,
                        Y = 8
                    }
                    : null
            },
            ProbeShapeKind.Polygon => new Polygon
            {
                Points = GetPolygonDefaultPoints(),
                FillRule = FillRule.EvenOdd
            },
            ProbeShapeKind.Polyline => new Polyline
            {
                Points = GetPolylineDefaultPoints(),
                FillRule = FillRule.Nonzero
            },
            ProbeShapeKind.Rectangle => new Rectangle
            {
                RadiusX = 12,
                RadiusY = 20
            },
            ProbeShapeKind.RoundRectangle => new RoundRectangle
            {
                CornerRadius = new CornerRadius(8, 24, 28, 14)
            },
            _ => new Ellipse()
        };

        shape.AutomationId = $"controls-sample-shapes-parity-shape-{shapeKind.ToString().ToLowerInvariant()}";
        shape.HorizontalOptions = LayoutOptions.Fill;
        shape.VerticalOptions = LayoutOptions.Fill;
        shape.WidthRequest = ProbeWidth;
        shape.HeightRequest = ProbeHeight;

        ApplySharedStyle(shape);

        return shape;
    }

    private void ApplyStyleToCurrentShape()
    {
        if (_currentShape is null)
        {
            return;
        }

        ApplySharedStyle(_currentShape);
        UpdateStateLabels();
    }

    private void ApplySharedStyle(Shape shape)
    {
        shape.Aspect = AspectModes[_aspectIndex];
        shape.Fill = ResolveBrush((PaintMode)_fillModeIndex, Color.FromArgb("#2563EB"), Color.FromArgb("#22D3EE"));
        shape.Stroke = ResolveBrush((PaintMode)_strokeModeIndex, Color.FromArgb("#111827"), Color.FromArgb("#6366F1"));
        shape.StrokeThickness = 4;
        shape.StrokeDashArray = _useDashPattern ? new DoubleCollection { 3, 2 } : new DoubleCollection();
        shape.StrokeDashOffset = _useDashPattern ? 1 : 0;
        shape.StrokeLineCap = LineCaps[_lineCapIndex];
        shape.StrokeLineJoin = LineJoins[_lineJoinIndex];
        shape.StrokeMiterLimit = _useHighMiterLimit ? 15 : 4;
    }

    private Brush ResolveBrush(PaintMode mode, Color primary, Color secondary)
    {
        return mode switch
        {
            PaintMode.Solid => new SolidColorBrush(primary),
            PaintMode.Linear => new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(primary, 0),
                    new GradientStop(secondary, 1)
                },
                new Point(0, 0),
                new Point(1, 1)),
            PaintMode.Radial => new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(primary, 0),
                    new GradientStop(secondary, 1)
                },
                new Point(0.5, 0.5),
                0.75),
            PaintMode.Image when _imageBrush is not null => _imageBrush,
            PaintMode.Image => new SolidColorBrush(primary),
            _ => new SolidColorBrush(primary)
        };
    }

    private void UpdateStateLabels()
    {
        var shapeName = ShapeKinds[_shapeIndex].ToString();
        var fillMode = ((PaintMode)_fillModeIndex).ToString();
        var strokeMode = ((PaintMode)_strokeModeIndex).ToString();
        var imageModeAvailable = _imageBrush is not null ? "available" : "fallback";

        _shapeKindLabel.Text = $"shape={shapeName}";
        _styleStateLabel.Text = $"aspect={AspectModes[_aspectIndex]} fill={fillMode} stroke={strokeMode} dash={(_useDashPattern ? "on" : "off")} cap={LineCaps[_lineCapIndex]} join={LineJoins[_lineJoinIndex]} miter={(_useHighMiterLimit ? "high" : "base")} image={imageModeAvailable}";
        _geometryStateLabel.Text = $"points={(_useAlternatePoints ? "alt" : "base")} pathData={(_useAlternatePathData ? "alt" : "base")} pathTransform={(_usePathRenderTransform ? "on" : "off")}";
    }

    private static void UpdatePointCollection(PointCollection target, IReadOnlyList<Point> points)
    {
        target.Clear();
        for (var index = 0; index < points.Count; index++)
        {
            target.Add(points[index]);
        }
    }

    private static PointCollection GetPolygonDefaultPoints()
    {
        return
        [
            new Point(24, 18),
            new Point(296, 28),
            new Point(248, 194),
            new Point(86, 208)
        ];
    }

    private static PointCollection GetPolygonAlternatePoints()
    {
        return
        [
            new Point(12, 108),
            new Point(108, 12),
            new Point(304, 56),
            new Point(292, 202),
            new Point(94, 208)
        ];
    }

    private static PointCollection GetPolylineDefaultPoints()
    {
        return
        [
            new Point(12, 188),
            new Point(88, 44),
            new Point(162, 136),
            new Point(234, 38),
            new Point(308, 176)
        ];
    }

    private static PointCollection GetPolylineAlternatePoints()
    {
        return
        [
            new Point(16, 62),
            new Point(82, 196),
            new Point(148, 88),
            new Point(238, 184),
            new Point(304, 52)
        ];
    }

    private static Geometry BuildPathData(bool alternate)
    {
        if (alternate)
        {
            return new PathGeometry
            {
                FillRule = FillRule.Nonzero,
                Figures =
                [
                    new PathFigure
                    {
                        StartPoint = new Point(24, 196),
                        IsClosed = true,
                        Segments =
                        [
                            new LineSegment { Point = new Point(108, 24) },
                            new LineSegment { Point = new Point(196, 116) },
                            new LineSegment { Point = new Point(296, 24) },
                            new LineSegment { Point = new Point(240, 196) }
                        ]
                    }
                ]
            };
        }

        return new PathGeometry
        {
            FillRule = FillRule.EvenOdd,
            Figures =
            [
                new PathFigure
                {
                    StartPoint = new Point(24, 24),
                    IsClosed = true,
                    Segments =
                    [
                        new LineSegment { Point = new Point(296, 24) },
                        new LineSegment { Point = new Point(296, 196) },
                        new LineSegment { Point = new Point(24, 196) }
                    ]
                },
                new PathFigure
                {
                    StartPoint = new Point(96, 88),
                    IsClosed = true,
                    Segments =
                    [
                        new LineSegment { Point = new Point(224, 88) },
                        new LineSegment { Point = new Point(224, 152) },
                        new LineSegment { Point = new Point(96, 152) }
                    ]
                }
            ]
        };
    }

    private static Brush? TryCreateImageBrush(string source)
    {
        var imageBrushType = typeof(Shape).Assembly.GetType("Microsoft.Maui.Controls.ImageBrush", throwOnError: false);
        if (imageBrushType is null)
        {
            return null;
        }

        var brush = Activator.CreateInstance(imageBrushType) as Brush;
        var imageSourceProperty = imageBrushType.GetProperty("ImageSource", BindingFlags.Public | BindingFlags.Instance);
        if (brush is null || imageSourceProperty is null)
        {
            return null;
        }

        imageSourceProperty.SetValue(brush, ImageSource.FromFile(source));
        return brush;
    }

    private enum ProbeShapeKind
    {
        Ellipse,
        Line,
        Path,
        Polygon,
        Polyline,
        Rectangle,
        RoundRectangle
    }

    private enum PaintMode
    {
        Solid,
        Linear,
        Radial,
        Image
    }
}
