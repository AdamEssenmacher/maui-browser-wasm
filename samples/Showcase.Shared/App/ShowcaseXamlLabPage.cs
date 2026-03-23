using System.ComponentModel;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Showcase.Shared.App;

public partial class ShowcaseXamlLabPage : ContentPage
{
    private static readonly TimeSpan PreviewDebounceDelay = TimeSpan.FromMilliseconds(350);

    private readonly IReadOnlyList<XamlLabSnippet> _snippets = CreateSnippets();
    private readonly XamlLabPreviewSession _previewSession;
    private int _selectedSnippetIndex;
    private bool _isDisposed;

    public ShowcaseXamlLabPage()
    {
        InitializeComponent();

        _previewSession = new XamlLabPreviewSession(new XamlLabFragmentCompiler(), PreviewDebounceDelay);
        _previewSession.Updated += OnPreviewUpdated;
        EditorView.PropertyChanged += OnEditorViewPropertyChanged;

        _selectedSnippetIndex = 0;
        LoadSnippetByIndex(_selectedSnippetIndex, applyImmediately: true);
    }

    private void OnLoadDefaultSnippetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        LoadSnippetByIndex(0, applyImmediately: true);
    }

    private void OnLoadLayoutSnippetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        LoadSnippetByIndex(1, applyImmediately: true);
    }

    private void OnLoadInputSnippetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        LoadSnippetByIndex(2, applyImmediately: true);
    }

    private void OnResetSnippetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        LoadSnippetByIndex(_selectedSnippetIndex, applyImmediately: true);
    }

    private void OnApplyNowClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _previewSession.ApplyNow();
    }

    private void OnSetRuntimeHtmlProbeClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        RuntimePropertyProbeLabel.Text = "<b>Rich</b> text";
        RuntimePropertyProbeLabel.TextType = TextType.Html;
    }

    private void OnSetRuntimePlainProbeClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        RuntimePropertyProbeLabel.Text = "Plain text";
        RuntimePropertyProbeLabel.TextType = TextType.Text;
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
        {
            DisposePageResources();
        }

        base.OnHandlerChanging(args);
    }

    private void LoadSnippetByIndex(int index, bool applyImmediately)
    {
        if (index < 0 || index >= _snippets.Count)
        {
            return;
        }

        _selectedSnippetIndex = index;
        var snippet = _snippets[index];
        EditorView.Text = snippet.Source;
        if (applyImmediately)
        {
            _previewSession.ApplyNow();
        }
    }

    private void OnEditorViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (!string.Equals(e.PropertyName, ShowcaseXamlEditorView.TextProperty.PropertyName, StringComparison.Ordinal))
        {
            return;
        }

        _previewSession.UpdateSource(EditorView.Text ?? string.Empty);
        EditorStatusLabel.Text = "Applying updates...";
    }

    private void OnPreviewUpdated(object? sender, XamlLabPreviewUpdate update)
    {
        _ = sender;
        if (_isDisposed)
        {
            return;
        }

        if (Dispatcher?.IsDispatchRequired == true)
        {
            Dispatcher.Dispatch(() => ApplyPreviewUpdate(update));
            return;
        }

        ApplyPreviewUpdate(update);
    }

    private void ApplyPreviewUpdate(XamlLabPreviewUpdate update)
    {
        if (_isDisposed)
        {
            return;
        }

        if (update.IsSuccess)
        {
            if (update.Preview is not View previewView)
            {
                ErrorPanel.IsVisible = true;
                ErrorLabel.Text = "Preview root is not a supported MAUI View instance.";
                EditorStatusLabel.Text = "Preview update failed.";
                return;
            }

            PreviewHost.Content = previewView;
            ErrorPanel.IsVisible = false;
            ErrorLabel.Text = string.Empty;
            EditorStatusLabel.Text = $"Applied revision {update.Revision}.";
            return;
        }

        ErrorPanel.IsVisible = true;
        ErrorLabel.Text = string.Join(Environment.NewLine, update.Errors);
        EditorStatusLabel.Text = update.PreservedLastGoodPreview
            ? "Invalid XAML. Showing last good preview."
            : "Invalid XAML. No valid preview yet.";
    }

    private void DisposePageResources()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        EditorView.PropertyChanged -= OnEditorViewPropertyChanged;
        _previewSession.Updated -= OnPreviewUpdated;
        _previewSession.Dispose();
    }

    private static IReadOnlyList<XamlLabSnippet> CreateSnippets()
    {
        return
        [
            new XamlLabSnippet(
                "Welcome stack",
                """
                <VerticalStackLayout Padding="16" Spacing="12">
                    <Label Text="Hello Mirage XAML Lab" FontSize="24" />
                    <Label Text="This preview is rendered through Mirage handlers." />
                    <Button Text="Sample action button" />
                    <Entry Placeholder="Type into Entry inside preview" />
                </VerticalStackLayout>
                """),
            new XamlLabSnippet(
                "Grid + alignment",
                """
                <Grid Padding="12" ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto" RowSpacing="10" ColumnSpacing="10">
                    <Label Text="Name" Grid.Row="0" Grid.Column="0" VerticalOptions="Center" />
                    <Entry Grid.Row="0" Grid.Column="1" Placeholder="Ada Lovelace" />
                    <Label Text="Role" Grid.Row="1" Grid.Column="0" VerticalOptions="Center" />
                    <Entry Grid.Row="1" Grid.Column="1" Placeholder=".NET Architect" />
                    <Button Grid.Row="2" Grid.Column="1" HorizontalOptions="End" Text="Save" />
                </Grid>
                """),
            new XamlLabSnippet(
                "Scroll + mixed layout",
                """
                <ScrollView>
                    <VerticalStackLayout Padding="16" Spacing="8">
                        <Label Text="Scrollable sample fragment" />
                        <HorizontalStackLayout Spacing="8">
                            <Button Text="Left" />
                            <Button Text="Center" />
                            <Button Text="Right" />
                        </HorizontalStackLayout>
                        <Label Text="Add more content to inspect measurement and spacing." />
                        <Label Text="Mirage keeps rendering in the normal MAUI handler pipeline." />
                        <Label Text="This lab intentionally supports a strict control whitelist." />
                    </VerticalStackLayout>
                </ScrollView>
                """)
        ];
    }

    private sealed record XamlLabSnippet(string Title, string Source);
}
