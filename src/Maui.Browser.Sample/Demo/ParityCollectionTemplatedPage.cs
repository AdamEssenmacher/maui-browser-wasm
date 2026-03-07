using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class ParityCollectionTemplatedPage : ContentPage
{
    private readonly ParityCollectionTemplatedState _state = new();

    private CollectionView? CollectionProbeElement => FindByName("CollectionProbe") as CollectionView;

    public ParityCollectionTemplatedPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;
        ApplyHeaderFooterMode();
    }

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnToggleSelectionModeClicked(object? sender, EventArgs e)
    {
        _state.CycleSelectionMode();
    }

    private void OnApplyProgrammaticSelectionClicked(object? sender, EventArgs e)
    {
        _state.ApplyProgrammaticSelection();
    }

    private void OnAddItemClicked(object? sender, EventArgs e)
    {
        _state.AddCollectionItem();
        RefreshHeaderFooterIfUsingObjectMode();
    }

    private void OnRemoveItemClicked(object? sender, EventArgs e)
    {
        _state.RemoveCollectionItem();
        RefreshHeaderFooterIfUsingObjectMode();
    }

    private void OnMoveItemClicked(object? sender, EventArgs e)
    {
        _state.MoveFirstToEnd();
    }

    private void OnReplaceItemClicked(object? sender, EventArgs e)
    {
        _state.ReplaceSecondItem();
        RefreshHeaderFooterIfUsingObjectMode();
    }

    private void OnClearItemsClicked(object? sender, EventArgs e)
    {
        _state.ClearCollectionItems();
        RefreshHeaderFooterIfUsingObjectMode();
    }

    private void OnResetItemsClicked(object? sender, EventArgs e)
    {
        _state.ResetCollectionItems();
        RefreshHeaderFooterIfUsingObjectMode();
    }

    private void OnToggleHeaderFooterModeClicked(object? sender, EventArgs e)
    {
        _state.ToggleHeaderFooterMode();
        ApplyHeaderFooterMode();
    }

    private void OnScrollToStartClicked(object? sender, EventArgs e)
    {
        ScrollToIndex(0, ScrollToPosition.Start, "start index 0");
    }

    private void OnScrollToCenterClicked(object? sender, EventArgs e)
    {
        var index = _state.ResolveCenterScrollIndex();
        ScrollToIndex(index, ScrollToPosition.Center, $"center index {index}");
    }

    private void OnScrollToEndClicked(object? sender, EventArgs e)
    {
        var index = _state.ResolveEndScrollIndex();
        ScrollToIndex(index, ScrollToPosition.End, $"end index {index}");
    }

    private void OnScrollToMakeVisibleClicked(object? sender, EventArgs e)
    {
        var index = _state.ResolveMakeVisibleIndex();
        ScrollToIndex(index, ScrollToPosition.MakeVisible, $"make-visible index {index}");
    }

    private void ScrollToIndex(int index, ScrollToPosition position, string action)
    {
        var collectionProbe = CollectionProbeElement;
        if (collectionProbe is null || _state.CollectionItems.Count == 0)
        {
            return;
        }

        var clampedIndex = Math.Max(0, Math.Min(index, _state.CollectionItems.Count - 1));
        collectionProbe.ScrollTo(clampedIndex, -1, position, false);
        _state.RecordScrollAction(action);
    }

    private void ApplyHeaderFooterMode()
    {
        var collectionProbe = CollectionProbeElement;
        if (collectionProbe is null)
        {
            return;
        }

        if (_state.UseTemplateHeaderFooter)
        {
            collectionProbe.Header = _state.HeaderTemplateData;
            collectionProbe.Footer = _state.FooterTemplateData;
            collectionProbe.HeaderTemplate = Resources["CollectionHeaderTemplate"] as DataTemplate;
            collectionProbe.FooterTemplate = Resources["CollectionFooterTemplate"] as DataTemplate;
            return;
        }

        collectionProbe.HeaderTemplate = null;
        collectionProbe.FooterTemplate = null;
        collectionProbe.Header = new Label
        {
            AutomationId = "parity-collection-header-object-view",
            BackgroundColor = Color.FromArgb("#DBEAFE"),
            FontSize = 12,
            Padding = new Thickness(8, 6),
            Text = _state.HeaderObjectText,
            TextColor = Color.FromArgb("#1E3A8A")
        };

        collectionProbe.Footer = new Label
        {
            AutomationId = "parity-collection-footer-object-view",
            BackgroundColor = Color.FromArgb("#DCFCE7"),
            FontSize = 12,
            Padding = new Thickness(8, 6),
            Text = _state.FooterObjectText,
            TextColor = Color.FromArgb("#14532D")
        };
    }

    private void RefreshHeaderFooterIfUsingObjectMode()
    {
        if (_state.UseTemplateHeaderFooter)
        {
            return;
        }

        ApplyHeaderFooterMode();
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ParityCollectionTemplatedPage));
#pragma warning restore IL2026
    }
}
