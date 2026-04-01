using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class GraphicsViewParityProbePage : ContentPage
{
    private readonly GraphicsView _graphicsView;
    private readonly ContentView _graphicsHost;
    private readonly Label _drawStateLabel;
    private readonly Label _interactionStateLabel;
    private readonly Label _styleStateLabel;
    private readonly Label _sizeStateLabel;
    private GraphicsProbeMode _mode = GraphicsProbeMode.Square;
    private Color _accentColor = Color.FromArgb("#DC2626");
    private bool _useAlternateColor;
    private bool _useAlternateBackground;
    private bool _useCompactSize;
    private int _invalidateCount;

    public GraphicsViewParityProbePage()
    {
        Title = "GraphicsView Parity Probe";
        AutomationId = "controls-sample-page-graphicsviewparitypage";

        _graphicsView = new GraphicsView
        {
            AutomationId = "controls-sample-graphicsview-parity-host",
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Background = new SolidColorBrush(Color.FromArgb("#F8FAFC")),
            Drawable = new ProbeDrawable(() => _mode, () => _accentColor)
        };
        _graphicsHost = new ContentView
        {
            WidthRequest = 220,
            HeightRequest = 160,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Content = _graphicsView
        };
        _graphicsView.StartHoverInteraction += (_, args) => OnInteractionChanged($"interaction=hover-start points={FormatPoints(args.Touches)}");
        _graphicsView.MoveHoverInteraction += (_, args) => OnInteractionChanged($"interaction=hover-move points={FormatPoints(args.Touches)}");
        _graphicsView.EndHoverInteraction += (_, _) => OnInteractionChanged("interaction=hover-end");
        _graphicsView.StartInteraction += (_, args) => OnInteractionChanged($"interaction=start points={FormatPoints(args.Touches)}");
        _graphicsView.DragInteraction += (_, args) => OnInteractionChanged($"interaction=drag points={FormatPoints(args.Touches)}");
        _graphicsView.EndInteraction += (_, args) => OnInteractionChanged($"interaction=end inside={args.IsInsideBounds} points={FormatPoints(args.Touches)}");
        _graphicsView.CancelInteraction += (_, _) => OnInteractionChanged("interaction=cancel");
        _graphicsView.SizeChanged += (_, _) => UpdateStateLabels();

        _drawStateLabel = new Label
        {
            AutomationId = "controls-sample-graphicsview-parity-draw-state",
            FontAttributes = FontAttributes.Bold
        };
        _interactionStateLabel = new Label
        {
            AutomationId = "controls-sample-graphicsview-parity-interaction-state"
        };
        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-graphicsview-parity-style-state"
        };
        _sizeStateLabel = new Label
        {
            AutomationId = "controls-sample-graphicsview-parity-size-state"
        };

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
                        AutomationId = "controls-sample-graphicsview-parity-description",
                        Text = "GraphicsView parity probe for canvas redraw/invalidate flow, runtime size updates, background and flow-direction projection, and pointer/hover interaction routing."
                    },
                    _graphicsHost,
                    _drawStateLabel,
                    _interactionStateLabel,
                    _styleStateLabel,
                    _sizeStateLabel,
                    BuildDrawableControls(),
                    BuildStyleControls()
                }
            }
        };

        UpdateStateLabels();
    }

    private View BuildDrawableControls()
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
                        CreateButton("Square", "controls-sample-graphicsview-parity-square", () => SetMode(GraphicsProbeMode.Square)),
                        CreateButton("Triangle", "controls-sample-graphicsview-parity-triangle", () => SetMode(GraphicsProbeMode.Triangle)),
                        CreateButton("EllipseArc", "controls-sample-graphicsview-parity-ellipsearc", () => SetMode(GraphicsProbeMode.EllipseArc))
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("LineDash", "controls-sample-graphicsview-parity-linedash", () => SetMode(GraphicsProbeMode.LineDash)),
                        CreateButton("Text", "controls-sample-graphicsview-parity-text", () => SetMode(GraphicsProbeMode.Text)),
                        CreateButton("Invalidate", "controls-sample-graphicsview-parity-invalidate", InvalidateCanvas)
                    }
                }
            }
        };
    }

    private View BuildStyleControls()
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
                        CreateButton("Toggle Color", "controls-sample-graphicsview-parity-toggle-color", ToggleColor),
                        CreateButton("Toggle Size", "controls-sample-graphicsview-parity-toggle-size", ToggleSize),
                        CreateButton("Toggle Background", "controls-sample-graphicsview-parity-toggle-background", ToggleBackground)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Flow", "controls-sample-graphicsview-parity-toggle-flow", ToggleFlowDirection)
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

    private void SetMode(GraphicsProbeMode mode)
    {
        _mode = mode;
        _graphicsView.Invalidate();
        UpdateStateLabels();
    }

    private void ToggleColor()
    {
        _useAlternateColor = !_useAlternateColor;
        _accentColor = _useAlternateColor
            ? Color.FromArgb("#2563EB")
            : Color.FromArgb("#DC2626");

        _graphicsView.Invalidate();
        UpdateStateLabels();
    }

    private void InvalidateCanvas()
    {
        _invalidateCount++;
        _graphicsView.Invalidate();
        UpdateStateLabels();
    }

    private void ToggleSize()
    {
        _useCompactSize = !_useCompactSize;
        _graphicsHost.WidthRequest = _useCompactSize ? 160 : 220;
        _graphicsHost.HeightRequest = _useCompactSize ? 120 : 160;
        _graphicsHost.InvalidateMeasure();
        _graphicsView.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1), _graphicsView.Invalidate);
        UpdateStateLabels();
    }

    private void ToggleBackground()
    {
        _useAlternateBackground = !_useAlternateBackground;
        _graphicsView.Background = new SolidColorBrush(
            _useAlternateBackground
                ? Color.FromArgb("#FEF3C7")
                : Color.FromArgb("#F8FAFC"));
        UpdateStateLabels();
    }

    private void ToggleFlowDirection()
    {
        _graphicsView.FlowDirection = _graphicsView.FlowDirection == FlowDirection.RightToLeft
            ? FlowDirection.LeftToRight
            : FlowDirection.RightToLeft;
        UpdateStateLabels();
    }

    private void OnInteractionChanged(string interaction)
    {
        _interactionStateLabel.Text = interaction;
    }

    private static string FormatPoints(IEnumerable<PointF>? points)
    {
        if (points is null)
        {
            return "[]";
        }

        return "[" + string.Join(";", points.Select(static point => $"{point.X:0.#},{point.Y:0.#}")) + "]";
    }

    private void UpdateStateLabels()
    {
        _drawStateLabel.Text = $"mode={_mode} color={_accentColor.ToArgbHex()} invalidates={_invalidateCount}";
        _interactionStateLabel.Text ??= "interaction=idle";
        _styleStateLabel.Text = $"background={(_useAlternateBackground ? "alt" : "default")} flow={_graphicsView.FlowDirection}";
        _sizeStateLabel.Text = $"request={_graphicsHost.WidthRequest:0}x{_graphicsHost.HeightRequest:0} actual={_graphicsView.Width:0.#}x{_graphicsView.Height:0.#}";
    }

    private enum GraphicsProbeMode
    {
        Square,
        Triangle,
        EllipseArc,
        LineDash,
        Text
    }

    private sealed class ProbeDrawable(Func<GraphicsProbeMode> getMode, Func<Color> getAccentColor) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var accent = getAccentColor();
            canvas.SaveState();
            canvas.StrokeColor = accent;
            canvas.FillColor = accent;
            canvas.StrokeSize = 6;

            switch (getMode())
            {
                case GraphicsProbeMode.Square:
                    DrawSquare(canvas, dirtyRect);
                    break;
                case GraphicsProbeMode.Triangle:
                    DrawTriangle(canvas, dirtyRect);
                    break;
                case GraphicsProbeMode.EllipseArc:
                    DrawEllipseArc(canvas, dirtyRect);
                    break;
                case GraphicsProbeMode.LineDash:
                    DrawLineDash(canvas, dirtyRect);
                    break;
                case GraphicsProbeMode.Text:
                    DrawText(canvas, dirtyRect);
                    break;
            }

            canvas.RestoreState();
        }

        private static void DrawSquare(ICanvas canvas, RectF dirtyRect)
        {
            var width = dirtyRect.Width * 0.45f;
            var height = dirtyRect.Height * 0.5f;
            var x = dirtyRect.Width * 0.18f;
            var y = dirtyRect.Height * 0.2f;
            canvas.FillRectangle(x, y, width, height);
        }

        private static void DrawTriangle(ICanvas canvas, RectF dirtyRect)
        {
            var path = new PathF();
            path.MoveTo(dirtyRect.Width * 0.55f, dirtyRect.Height * 0.1f);
            path.LineTo(dirtyRect.Width * 0.9f, dirtyRect.Height * 0.8f);
            path.LineTo(dirtyRect.Width * 0.25f, dirtyRect.Height * 0.8f);
            path.Close();
            canvas.FillPath(path);
        }

        private static void DrawEllipseArc(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillEllipse(dirtyRect.Width * 0.2f, dirtyRect.Height * 0.18f, dirtyRect.Width * 0.48f, dirtyRect.Height * 0.46f);
            canvas.DrawArc(dirtyRect.Width * 0.55f, dirtyRect.Height * 0.24f, dirtyRect.Width * 0.24f, dirtyRect.Height * 0.26f, 0, 210, true, false);
        }

        private static void DrawLineDash(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeDashPattern = [10, 6];
            canvas.DrawLine(dirtyRect.Width * 0.12f, dirtyRect.Height * 0.22f, dirtyRect.Width * 0.88f, dirtyRect.Height * 0.75f);
        }

        private static void DrawText(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FontSize = 22;
            canvas.FontColor = Colors.Black;
            canvas.DrawString(
                "Mirage",
                dirtyRect.Left,
                dirtyRect.Top,
                dirtyRect.Width,
                dirtyRect.Height,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }
    }
}
