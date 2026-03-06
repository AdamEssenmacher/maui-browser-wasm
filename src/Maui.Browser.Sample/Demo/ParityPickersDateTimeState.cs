using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityPickersDateTimeState : INotifyPropertyChanged
{
    private DateTime _selectedDate = new(2026, 3, 6);
    private TimeSpan _selectedTime = new(9, 30, 0);
    private double _stepperValue = 2;
    private double _progressValue = 0.35;
    private bool _activityRunning = true;
    private string _lastMutation = "initial";

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (!SetProperty(ref _selectedDate, value))
            {
                return;
            }

            LastMutation = "date-changed";
            OnPropertyChanged(nameof(DateSummary));
        }
    }

    public TimeSpan SelectedTime
    {
        get => _selectedTime;
        set
        {
            if (!SetProperty(ref _selectedTime, value))
            {
                return;
            }

            LastMutation = "time-changed";
            OnPropertyChanged(nameof(TimeSummary));
        }
    }

    public double StepperValue
    {
        get => _stepperValue;
        set
        {
            if (!SetProperty(ref _stepperValue, value))
            {
                return;
            }

            LastMutation = "stepper-changed";
            OnPropertyChanged(nameof(StepperSummary));
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            var clamped = Math.Clamp(value, 0, 1);
            if (!SetProperty(ref _progressValue, clamped))
            {
                return;
            }

            LastMutation = "progress-changed";
            OnPropertyChanged(nameof(ProgressSummary));
        }
    }

    public bool ActivityRunning
    {
        get => _activityRunning;
        set
        {
            if (!SetProperty(ref _activityRunning, value))
            {
                return;
            }

            LastMutation = "activity-toggled";
            OnPropertyChanged(nameof(ActivitySummary));
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

            OnPropertyChanged(nameof(MutationSummary));
        }
    }

    public string DateSummary => $"Date value: {SelectedDate:yyyy-MM-dd}";

    public string TimeSummary => $"Time value: {SelectedTime:hh\\:mm}";

    public string StepperSummary => $"Stepper value: {StepperValue.ToString("0.###", CultureInfo.InvariantCulture)}";

    public string ProgressSummary => $"Progress value: {ProgressValue.ToString("0.##", CultureInfo.InvariantCulture)}";

    public string ActivitySummary => $"Activity running: {(ActivityRunning ? "on" : "off")}";

    public string MutationSummary => $"Last mutation: {LastMutation}";

    public void ToggleActivity()
    {
        ActivityRunning = !ActivityRunning;
    }

    public void IncrementProgress()
    {
        ProgressValue = ProgressValue + 0.1;
        if (ProgressValue >= 0.99)
        {
            ProgressValue = 0;
        }
    }

    public void ApplyProgrammaticUpdate()
    {
        SelectedDate = new DateTime(2026, 12, 24);
        SelectedTime = new TimeSpan(14, 45, 0);
        StepperValue = 6.5;
        ProgressValue = 0.82;
        ActivityRunning = false;
        LastMutation = "programmatic-update";
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
