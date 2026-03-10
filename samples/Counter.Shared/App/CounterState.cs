using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Counter.Shared.App;

internal sealed class CounterState : INotifyPropertyChanged
{
    private int _count;
    private string _inputText = string.Empty;

    public CounterState()
    {
        IncrementCommand = new Command(OnIncrement);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand IncrementCommand { get; }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (string.Equals(_inputText, value, StringComparison.Ordinal))
            {
                return;
            }

            _inputText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LiveText));
        }
    }

    public string LiveText => string.IsNullOrWhiteSpace(_inputText)
        ? "Live text: (empty)"
        : $"Live text: {_inputText}";

    public string CountText => $"Count: {_count}";

    private void OnIncrement()
    {
        _count++;
        OnPropertyChanged(nameof(CountText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
