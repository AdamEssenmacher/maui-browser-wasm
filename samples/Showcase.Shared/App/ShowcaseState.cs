using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Showcase.Shared.App;

public sealed class ShowcaseState : INotifyPropertyChanged
{
    private int _clickCount;
    private int _genericWidgetClickCount;
    private int _layoutLabScrollCommandAttemptCount;
    private string _name = "Adam";

    public ShowcaseState()
    {
        ClickCommand = new Command(OnClick);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ClickCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.Equals(_name, value, StringComparison.Ordinal))
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Greeting));
            OnPropertyChanged(nameof(GenericWidgetText));
        }
    }

    public string Greeting => string.IsNullOrWhiteSpace(_name)
        ? "Hello, developer"
        : $"Hello, {_name}";

    public string ClickCountText => $"Click count: {_clickCount}";

    public string GenericWidgetText => string.IsNullOrWhiteSpace(_name)
        ? "vendor-pill element: Hello, developer"
        : $"vendor-pill element: Hello, {_name}";

    public string GenericWidgetClicksText => $"Generic widget clicks: {_genericWidgetClickCount}";

    public string LayoutLabScrollCommandCountText => $"LayoutLab RequestScrollTo attempts: {_layoutLabScrollCommandAttemptCount}";

    private void OnClick()
    {
        _clickCount++;
        OnPropertyChanged(nameof(ClickCountText));
    }

    public void RegisterGenericWidgetClick()
    {
        _genericWidgetClickCount++;
        OnPropertyChanged(nameof(GenericWidgetClicksText));
    }

    public void RegisterLayoutLabScrollCommandAttempt()
    {
        _layoutLabScrollCommandAttemptCount++;
        OnPropertyChanged(nameof(LayoutLabScrollCommandCountText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
