using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class EditorParityProbePage : ContentPage
{
    private readonly Editor _editor;
    private readonly Label _stateLabel;
    private readonly Label _selectionLabel;
    private readonly Label _completedLabel;
    private readonly Button _blurTargetButton;

    private int _completedCount;
    private int _maxLength = 160;

    public EditorParityProbePage()
    {
        Title = "Editor Parity Probe";
        AutomationId = "controls-sample-page-editorparitypage";

        _editor = new Editor
        {
            AutomationId = "controls-sample-editor-parity-host",
            Text = "Editor parity baseline",
            Placeholder = "Type multiline text",
            PlaceholderColor = Color.FromArgb("#64748B"),
            TextColor = Color.FromArgb("#1E293B"),
            MaxLength = _maxLength,
            CharacterSpacing = 0.5,
            HeightRequest = 140,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Start
        };

        _editor.Completed += (_, _) =>
        {
            _completedCount++;
            UpdateStateLabels();
        };

        _stateLabel = new Label
        {
            AutomationId = "controls-sample-editor-parity-state",
            FontAttributes = FontAttributes.Bold
        };

        _selectionLabel = new Label
        {
            AutomationId = "controls-sample-editor-parity-selection"
        };

        _completedLabel = new Label
        {
            AutomationId = "controls-sample-editor-parity-completed"
        };

        _blurTargetButton = new Button
        {
            Text = "Blur Target",
            AutomationId = "controls-sample-editor-parity-blur-target"
        };

        _editor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(Editor.Text)
                or nameof(Editor.TextTransform)
                or nameof(Editor.IsReadOnly)
                or nameof(Editor.IsEnabled)
                or nameof(Editor.MaxLength)
                or nameof(Editor.CursorPosition)
                or nameof(Editor.SelectionLength))
            {
                UpdateStateLabels();
            }
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
                        AutomationId = "controls-sample-editor-parity-description",
                        Text = "Editor parity probe for text, transform, readonly/enabled, max-length, selection, and completed-on-blur behavior."
                    },
                    _editor,
                    _stateLabel,
                    _selectionLabel,
                    _completedLabel,
                    BuildControls()
                }
            }
        };

        UpdateStateLabels();
    }

    private View BuildControls()
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
                        CreateButton("Set Text A", "controls-sample-editor-parity-set-text-a", () =>
                        {
                            _editor.Text = "Line A\nLine B";
                        }),
                        CreateButton("Set Text B", "controls-sample-editor-parity-set-text-b", () =>
                        {
                            _editor.Text = "second sample text";
                        }),
                        CreateButton("Toggle Transform", "controls-sample-editor-parity-toggle-transform", () =>
                        {
                            _editor.TextTransform = _editor.TextTransform == TextTransform.Uppercase
                                ? TextTransform.None
                                : TextTransform.Uppercase;
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle ReadOnly", "controls-sample-editor-parity-toggle-readonly", () =>
                        {
                            _editor.IsReadOnly = !_editor.IsReadOnly;
                        }),
                        CreateButton("Toggle Enabled", "controls-sample-editor-parity-toggle-enabled", () =>
                        {
                            _editor.IsEnabled = !_editor.IsEnabled;
                        }),
                        CreateButton("Toggle MaxLength", "controls-sample-editor-parity-toggle-maxlength", () =>
                        {
                            _maxLength = _maxLength == 160 ? 8 : 160;
                            _editor.MaxLength = _maxLength;
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Set Selection", "controls-sample-editor-parity-set-selection", () =>
                        {
                            _editor.CursorPosition = 1;
                            _editor.SelectionLength = 4;
                        }),
                        CreateButton("Focus Editor", "controls-sample-editor-parity-focus-editor", () =>
                        {
                            _editor.Focus();
                        }),
                        _blurTargetButton
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

    private void UpdateStateLabels()
    {
        _stateLabel.Text = $"transform={_editor.TextTransform} readonly={_editor.IsReadOnly} enabled={_editor.IsEnabled} max={_editor.MaxLength}";
        _selectionLabel.Text = $"cursor={_editor.CursorPosition} selection={_editor.SelectionLength} length={(_editor.Text ?? string.Empty).Length}";
        _completedLabel.Text = $"completed={_completedCount}";
    }
}
