using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Browser.Sample.Demo;

internal sealed class ShowcaseHomeState : INotifyPropertyChanged
{
    private int _themeToggleCount;
    private string _themeModeText = "launch";
    private bool _primaryActionEnabled = true;
    private int _primaryActionCount;
    private string _focusProbeText = "Focus this field to see state styling.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ThemeModeText
    {
        get => _themeModeText;
        private set
        {
            if (!SetProperty(ref _themeModeText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ThemeSummary));
        }
    }

    public int ThemeToggleCount
    {
        get => _themeToggleCount;
        private set
        {
            if (!SetProperty(ref _themeToggleCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ThemeSummary));
        }
    }

    public bool PrimaryActionEnabled
    {
        get => _primaryActionEnabled;
        private set
        {
            if (!SetProperty(ref _primaryActionEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PrimaryActionSummary));
        }
    }

    public int PrimaryActionCount
    {
        get => _primaryActionCount;
        private set
        {
            if (!SetProperty(ref _primaryActionCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PrimaryActionSummary));
        }
    }

    public string FocusProbeText
    {
        get => _focusProbeText;
        set => SetProperty(ref _focusProbeText, value);
    }

    public string ThemeSummary => $"Theme: {ThemeModeText} | toggles: {ThemeToggleCount}";

    public string PrimaryActionSummary =>
        $"Primary action count: {PrimaryActionCount} | enabled: {(PrimaryActionEnabled ? "yes" : "no")}";

    public void ApplyTheme(ShowcaseThemeMode mode, bool incrementToggle)
    {
        ThemeModeText = ShowcaseThemeManager.DescribeMode(mode);
        if (incrementToggle)
        {
            ThemeToggleCount += 1;
        }
    }

    public void TogglePrimaryActionEnabled()
    {
        PrimaryActionEnabled = !PrimaryActionEnabled;
    }

    public void RecordPrimaryAction()
    {
        PrimaryActionCount += 1;
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
