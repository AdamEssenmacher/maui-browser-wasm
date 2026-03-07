using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Layouts;

namespace Maui.Browser.Sample.Demo;

internal sealed class ShowcaseLayoutCollectionState : INotifyPropertyChanged
{
    private int _nextItemId = 7;
    private bool _isRefreshing;
    private bool _isRefreshEnabled = true;
    private int _refreshCount;
    private string _lastRefreshSource = "none";
    private string _lastMutation = "initial";
    private FlexDirection _currentFlexDirection = FlexDirection.Row;
    private FlexWrap _currentFlexWrap = FlexWrap.Wrap;

    public ShowcaseLayoutCollectionState()
    {
        Items = new ObservableCollection<ShowcaseFeedItem>
        {
            new(1, "Launch narrative", "Craft a clear story around handler parity."),
            new(2, "Style system", "Demonstrate implicit, explicit, and dynamic styles."),
            new(3, "Media depth", "Show polished image and border behaviors."),
            new(4, "Layout confidence", "Validate stack, grid, and flex semantics."),
            new(5, "Collection readiness", "Prove refresh and mutation reliability."),
            new(6, "Accessibility pass", "Keep semantics and interaction states aligned.")
        };

        RefreshCommand = new Command(() => BeginRefresh("command"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ShowcaseFeedItem> Items { get; }

    public ICommand RefreshCommand { get; }

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

            OnPropertyChanged(nameof(RefreshSummary));
        }
    }

    public int RefreshCount
    {
        get => _refreshCount;
        private set
        {
            if (!SetProperty(ref _refreshCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(RefreshSummary));
        }
    }

    public string LastRefreshSource
    {
        get => _lastRefreshSource;
        private set
        {
            if (!SetProperty(ref _lastRefreshSource, value))
            {
                return;
            }

            OnPropertyChanged(nameof(RefreshSummary));
        }
    }

    public string LastMutation
    {
        get => _lastMutation;
        private set
        {
            if (!SetProperty(ref _lastMutation, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CollectionSummary));
        }
    }

    public FlexDirection CurrentFlexDirection
    {
        get => _currentFlexDirection;
        private set
        {
            if (!SetProperty(ref _currentFlexDirection, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FlexSummary));
        }
    }

    public FlexWrap CurrentFlexWrap
    {
        get => _currentFlexWrap;
        private set
        {
            if (!SetProperty(ref _currentFlexWrap, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FlexSummary));
        }
    }

    public string CollectionSummary => $"Items: {Items.Count} | last mutation: {LastMutation}";

    public string RefreshSummary =>
        $"Refresh enabled: {(IsRefreshEnabled ? "on" : "off")}, refreshing: {IsRefreshing.ToString().ToLowerInvariant()}, count: {RefreshCount}, source: {LastRefreshSource}";

    public string FlexSummary => $"Flex direction: {CurrentFlexDirection.ToString().ToLowerInvariant()}, wrap: {CurrentFlexWrap.ToString().ToLowerInvariant()}";

    public void ToggleFlexDirection()
    {
        CurrentFlexDirection = CurrentFlexDirection == FlexDirection.Row
            ? FlexDirection.Column
            : FlexDirection.Row;
    }

    public void ToggleFlexWrap()
    {
        CurrentFlexWrap = CurrentFlexWrap == FlexWrap.Wrap
            ? FlexWrap.NoWrap
            : FlexWrap.Wrap;
    }

    public void AddItem()
    {
        Items.Add(new ShowcaseFeedItem(_nextItemId, $"New card {_nextItemId}", "Added from showcase action."));
        _nextItemId += 1;
        LastMutation = "add";
        OnPropertyChanged(nameof(CollectionSummary));
    }

    public void RemoveItem()
    {
        if (Items.Count == 0)
        {
            LastMutation = "remove-empty";
            return;
        }

        Items.RemoveAt(Items.Count - 1);
        LastMutation = "remove";
        OnPropertyChanged(nameof(CollectionSummary));
    }

    public void BeginRefresh(string source)
    {
        if (!IsRefreshEnabled || IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        RefreshCount += 1;
        LastRefreshSource = source;
    }

    public void CompleteRefresh()
    {
        IsRefreshing = false;
        LastMutation = "refresh-complete";
        OnPropertyChanged(nameof(CollectionSummary));
    }

    public void ToggleRefreshEnabled()
    {
        IsRefreshEnabled = !IsRefreshEnabled;
        LastRefreshSource = "enabled-toggle";
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal sealed record ShowcaseFeedItem(int Id, string Title, string Subtitle);
