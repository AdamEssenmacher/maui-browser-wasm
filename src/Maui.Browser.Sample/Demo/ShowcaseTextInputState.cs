using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Browser.Sample.Demo;

internal sealed class ShowcaseTextInputState : INotifyPropertyChanged
{
    private string _liveHeadline = "Browser handler styling with live text.";
    private string _entryText = "launch copy";
    private int _entryCompletedCount;
    private string _editorText = "Edit multiline notes for your product story.";
    private string _searchText = string.Empty;
    private int _searchCount;
    private string _lastSearch = "<none>";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string LiveHeadline
    {
        get => _liveHeadline;
        set
        {
            if (!SetProperty(ref _liveHeadline, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HeadlineSummary));
        }
    }

    public string EntryText
    {
        get => _entryText;
        set
        {
            if (!SetProperty(ref _entryText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(EntrySummary));
        }
    }

    public int EntryCompletedCount
    {
        get => _entryCompletedCount;
        private set
        {
            if (!SetProperty(ref _entryCompletedCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(EntrySummary));
        }
    }

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (!SetProperty(ref _editorText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(EditorSummary));
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SearchSummary));
        }
    }

    public int SearchCount
    {
        get => _searchCount;
        private set
        {
            if (!SetProperty(ref _searchCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SearchSummary));
        }
    }

    public string LastSearch
    {
        get => _lastSearch;
        private set
        {
            if (!SetProperty(ref _lastSearch, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SearchSummary));
        }
    }

    public string HeadlineSummary => $"Headline chars: {LiveHeadline.Length}";
    public string EntrySummary => $"Entry: {EntryText} | completed: {EntryCompletedCount}";
    public string EditorSummary => $"Editor chars: {EditorText.Length}";
    public string SearchSummary => $"Search: {SearchText} | submits: {SearchCount} | last: {LastSearch}";

    public void RecordEntryCompleted()
    {
        EntryCompletedCount += 1;
    }

    public void RecordSearch(string? query)
    {
        SearchCount += 1;
        LastSearch = string.IsNullOrWhiteSpace(query) ? "<empty>" : query;
    }

    public void ApplyProgrammaticPreset()
    {
        LiveHeadline = "Programmatic headline update via MAUI state.";
        EntryText = "programmatic headline";
        EditorText = "Programmatic editor content\nwith second line context.";
        SearchText = "product launch";
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
