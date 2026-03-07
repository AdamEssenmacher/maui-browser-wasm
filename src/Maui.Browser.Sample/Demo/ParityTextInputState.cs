using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityTextInputState : INotifyPropertyChanged
{
    private string _labelProbeText =
        "This label probes line wrapping, truncation, and max-line behavior for browser label projection.";
    private bool _labelTruncationEnabled;

    private string _entryText = "text-start";
    private int _entryCompletedCount;
    private string _numericEntryText = "12";

    private string _passwordText = "hunter2";
    private string _editorText = "Editor line 1\nEditor line 2";

    private string _searchText = string.Empty;
    private int _searchPressedCount;
    private string _lastSearchText = "<none>";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string LabelProbeText
    {
        get => _labelProbeText;
        set => SetProperty(ref _labelProbeText, value);
    }

    public bool LabelTruncationEnabled
    {
        get => _labelTruncationEnabled;
        private set
        {
            if (!SetProperty(ref _labelTruncationEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(LabelModeSummary));
        }
    }

    public string LabelModeSummary => LabelTruncationEnabled
        ? "Label mode: tail truncation (max-lines=1)"
        : "Label mode: word-wrap (max-lines=3)";

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

    public string EntrySummary => $"Entry text: {EntryText} | completed: {EntryCompletedCount}";

    public string NumericEntryText
    {
        get => _numericEntryText;
        set
        {
            if (!SetProperty(ref _numericEntryText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(NumericEntrySummary));
        }
    }

    public string NumericEntrySummary => $"Numeric entry text: {NumericEntryText}";

    public string PasswordText
    {
        get => _passwordText;
        set
        {
            if (!SetProperty(ref _passwordText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PasswordSummary));
        }
    }

    public string PasswordSummary => $"Password length: {PasswordText.Length}";

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

    public string EditorSummary => $"Editor chars: {EditorText.Length}";

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

    public int SearchPressedCount
    {
        get => _searchPressedCount;
        private set
        {
            if (!SetProperty(ref _searchPressedCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SearchSummary));
        }
    }

    public string LastSearchText
    {
        get => _lastSearchText;
        private set
        {
            if (!SetProperty(ref _lastSearchText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SearchSummary));
        }
    }

    public string SearchSummary => $"Search text: {SearchText} | presses: {SearchPressedCount} | last: {LastSearchText}";

    public void ToggleLabelMode()
    {
        LabelTruncationEnabled = !LabelTruncationEnabled;
    }

    public void RecordEntryCompleted()
    {
        EntryCompletedCount += 1;
    }

    public void RecordSearchPressed(string? query)
    {
        SearchPressedCount += 1;
        LastSearchText = string.IsNullOrWhiteSpace(query) ? "<empty>" : query;
    }

    public void ApplyProgrammaticUpdate()
    {
        LabelProbeText =
            "Programmatic label update verifies MAUI-to-DOM propagation for text and line-break styles.";
        EntryText = "programmatic-entry";
        NumericEntryText = "9876";
        PasswordText = "programmatic-password";
        EditorText = "Programmatic editor text\nSecond line";
        SearchText = "programmatic-search";
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
