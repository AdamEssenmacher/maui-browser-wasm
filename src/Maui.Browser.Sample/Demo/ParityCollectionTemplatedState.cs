using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityCollectionTemplatedState : INotifyPropertyChanged
{
    private readonly ObservableCollection<ParityCollectionItem> _collectionItems = [];
    private int _nextItemId;
    private string _lastMutation = "initial";
    private string _lastScrollAction = "none";
    private SelectionMode _collectionSelectionMode = SelectionMode.None;
    private object? _selectedItem;
    private IList<object> _selectedItems = new List<object>();
    private bool _useTemplateHeaderFooter = true;
    private bool _isRefreshing;
    private bool _isRefreshEnabled = true;
    private int _refreshCommandCount;
    private string _lastRefreshSource = "none";

    public ParityCollectionTemplatedState()
    {
        HeaderTemplateData = new ParityCollectionHeaderData("Header template: Collection parity probe");
        FooterTemplateData = new ParityCollectionFooterData("Footer template: Selection + Move/Replace + ScrollTo");
        EmptyTemplateData = new ParityCollectionEmptyData("Collection empty state via template binding.");
        RefreshCommand = new Command<object?>(OnRefreshCommandExecuted);

        _collectionItems.CollectionChanged += OnCollectionItemsChanged;
        ResetCollectionItems();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ParityCollectionItem> CollectionItems => _collectionItems;

    public ParityCollectionHeaderData HeaderTemplateData { get; }

    public ParityCollectionFooterData FooterTemplateData { get; }

    public ParityCollectionEmptyData EmptyTemplateData { get; }

    public SelectionMode CollectionSelectionMode
    {
        get => _collectionSelectionMode;
        private set
        {
            if (!SetProperty(ref _collectionSelectionMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public IList<object> SelectedItems
    {
        get => _selectedItems;
        set
        {
            var normalized = value ?? new List<object>();
            if (!SetProperty(ref _selectedItems, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public bool UseTemplateHeaderFooter
    {
        get => _useTemplateHeaderFooter;
        private set
        {
            if (!SetProperty(ref _useTemplateHeaderFooter, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HeaderFooterSummary));
        }
    }

    public string CollectionSummary =>
        $"Collection items: {CollectionItems.Count}, last mutation: {_lastMutation}";

    public string SelectionSummary
    {
        get
        {
            var selectedTitle = SelectedItem is ParityCollectionItem selected
                ? selected.Title
                : "<none>";

            var selectedCount = CollectionSelectionMode == SelectionMode.Multiple
                ? SelectedItems.OfType<ParityCollectionItem>().Count()
                : selectedTitle == "<none>" ? 0 : 1;

            return $"Selection mode: {CollectionSelectionMode}, selected item: {selectedTitle}, selected count: {selectedCount}";
        }
    }

    public string HeaderFooterSummary =>
        $"Header/Footer mode: {(UseTemplateHeaderFooter ? "template" : "object view")}";

    public string ScrollSummary => $"Last scroll action: {_lastScrollAction}";

    public string HeaderObjectText => $"Header object view ({CollectionItems.Count} items)";

    public string FooterObjectText => $"Footer object view ({CollectionItems.Count} items)";

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (!SetProperty(ref _isRefreshing, value))
            {
                return;
            }

            OnPropertyChanged(nameof(RefreshSummary));
        }
    }

    public bool IsRefreshEnabled
    {
        get => _isRefreshEnabled;
        set
        {
            if (!SetProperty(ref _isRefreshEnabled, value))
            {
                return;
            }

            if (!value && IsRefreshing)
            {
                IsRefreshing = false;
            }

            OnPropertyChanged(nameof(RefreshSummary));
        }
    }

    public ICommand RefreshCommand { get; }

    public object RefreshCommandParameter => "collection-family-trigger";

    public Color RefreshColor => Color.FromArgb("#0EA5E9");

    public string RefreshSummary =>
        $"Refresh enabled: {(IsRefreshEnabled ? "on" : "off")}, refreshing: {(IsRefreshing ? "true" : "false")}, command count: {_refreshCommandCount}, last source: {_lastRefreshSource}";

    public void CycleSelectionMode()
    {
        var nextMode = CollectionSelectionMode switch
        {
            SelectionMode.None => SelectionMode.Single,
            SelectionMode.Single => SelectionMode.Multiple,
            _ => SelectionMode.None
        };

        CollectionSelectionMode = nextMode;
        if (nextMode == SelectionMode.None)
        {
            ClearSelection();
        }
    }

    public void ApplyProgrammaticSelection()
    {
        if (CollectionItems.Count == 0)
        {
            ClearSelection();
            return;
        }

        if (CollectionSelectionMode == SelectionMode.None)
        {
            CollectionSelectionMode = SelectionMode.Single;
        }

        if (CollectionSelectionMode == SelectionMode.Single)
        {
            SelectedItem = CollectionItems[Math.Min(3, CollectionItems.Count - 1)];
            SelectedItems = new List<object>();
            return;
        }

        var selected = new List<object>
        {
            CollectionItems[0],
            CollectionItems[Math.Min(4, CollectionItems.Count - 1)]
        };
        SelectedItems = selected;
    }

    public void AddCollectionItem()
    {
        var id = _nextItemId++;
        _collectionItems.Add(new ParityCollectionItem(id, $"Item {id}"));
        _lastMutation = "add";
        RaiseCollectionMutationProperties();
    }

    public void RemoveCollectionItem()
    {
        if (_collectionItems.Count == 0)
        {
            _lastMutation = "remove (noop)";
            RaiseCollectionMutationProperties();
            return;
        }

        _collectionItems.RemoveAt(_collectionItems.Count - 1);
        _lastMutation = "remove";
        PruneSelectionForCurrentItems();
        RaiseCollectionMutationProperties();
    }

    public void MoveFirstToEnd()
    {
        if (_collectionItems.Count <= 1)
        {
            _lastMutation = "move (noop)";
            RaiseCollectionMutationProperties();
            return;
        }

        _collectionItems.Move(0, _collectionItems.Count - 1);
        _lastMutation = "move";
        RaiseCollectionMutationProperties();
    }

    public void ReplaceSecondItem()
    {
        if (_collectionItems.Count < 2)
        {
            _lastMutation = "replace (noop)";
            RaiseCollectionMutationProperties();
            return;
        }

        var id = _nextItemId++;
        _collectionItems[1] = new ParityCollectionItem(id, $"Replaced {id}");
        _lastMutation = "replace";
        PruneSelectionForCurrentItems();
        RaiseCollectionMutationProperties();
    }

    public void ClearCollectionItems()
    {
        _collectionItems.Clear();
        _lastMutation = "clear";
        ClearSelection();
        RaiseCollectionMutationProperties();
    }

    public void ResetCollectionItems()
    {
        _collectionItems.Clear();
        _collectionItems.Add(new ParityCollectionItem(1, "Alpha row"));
        _collectionItems.Add(new ParityCollectionItem(2, "Beta row"));
        _collectionItems.Add(new ParityCollectionItem(3, "Gamma row"));
        _collectionItems.Add(new ParityCollectionItem(4, "Delta row"));
        _collectionItems.Add(new ParityCollectionItem(5, "Epsilon row"));
        _collectionItems.Add(new ParityCollectionItem(6, "Zeta row"));
        _collectionItems.Add(new ParityCollectionItem(7, "Eta row"));
        _collectionItems.Add(new ParityCollectionItem(8, "Theta row"));
        _collectionItems.Add(new ParityCollectionItem(9, "Iota row"));
        _collectionItems.Add(new ParityCollectionItem(10, "Kappa row"));
        _collectionItems.Add(new ParityCollectionItem(11, "Lambda row"));
        _collectionItems.Add(new ParityCollectionItem(12, "Mu row"));
        _nextItemId = 13;
        _lastMutation = "reset";
        ClearSelection();
        RaiseCollectionMutationProperties();
    }

    public void ToggleHeaderFooterMode()
    {
        UseTemplateHeaderFooter = !UseTemplateHeaderFooter;
    }

    public void RecordScrollAction(string action)
    {
        _lastScrollAction = action;
        OnPropertyChanged(nameof(ScrollSummary));
    }

    public void CompleteRefresh()
    {
        if (!IsRefreshing)
        {
            _lastRefreshSource = "complete (noop)";
            OnPropertyChanged(nameof(RefreshSummary));
            return;
        }

        IsRefreshing = false;
        _lastRefreshSource = "complete button";
        OnPropertyChanged(nameof(RefreshSummary));
    }

    public void ToggleRefreshEnabled()
    {
        IsRefreshEnabled = !IsRefreshEnabled;
        _lastRefreshSource = IsRefreshEnabled ? "enabled toggle" : "disabled toggle";
        OnPropertyChanged(nameof(RefreshSummary));
    }

    public int ResolveCenterScrollIndex() => Math.Min(8, Math.Max(0, CollectionItems.Count - 1));

    public int ResolveEndScrollIndex() => Math.Max(0, CollectionItems.Count - 1);

    public int ResolveMakeVisibleIndex() => Math.Min(3, Math.Max(0, CollectionItems.Count - 1));

    private void OnCollectionItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CollectionSummary));
        OnPropertyChanged(nameof(HeaderObjectText));
        OnPropertyChanged(nameof(FooterObjectText));
    }

    private void ClearSelection()
    {
        SelectedItem = null;
        SelectedItems = new List<object>();
    }

    private void PruneSelectionForCurrentItems()
    {
        if (SelectedItem is ParityCollectionItem selectedItem && !_collectionItems.Contains(selectedItem))
        {
            SelectedItem = null;
        }

        if (SelectedItems.Count == 0)
        {
            return;
        }

        var remainingSelection = SelectedItems.OfType<ParityCollectionItem>()
            .Where(_collectionItems.Contains)
            .Cast<object>()
            .ToList();
        SelectedItems = remainingSelection;
    }

    private void RaiseCollectionMutationProperties()
    {
        OnPropertyChanged(nameof(CollectionSummary));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(HeaderObjectText));
        OnPropertyChanged(nameof(FooterObjectText));
    }

    private void OnRefreshCommandExecuted(object? parameter)
    {
        _refreshCommandCount++;
        _lastRefreshSource = parameter?.ToString() ?? "command";
        OnPropertyChanged(nameof(RefreshSummary));
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal sealed class ParityCollectionItem
{
    public ParityCollectionItem(int id, string title)
    {
        Id = id;
        Title = title;
    }

    public int Id { get; }

    public string Title { get; }

    public string IdText => $"#{Id}";
}

internal sealed class ParityCollectionHeaderData
{
    public ParityCollectionHeaderData(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

internal sealed class ParityCollectionFooterData
{
    public ParityCollectionFooterData(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

internal sealed class ParityCollectionEmptyData
{
    public ParityCollectionEmptyData(string text)
    {
        Text = text;
    }

    public string Text { get; }
}
