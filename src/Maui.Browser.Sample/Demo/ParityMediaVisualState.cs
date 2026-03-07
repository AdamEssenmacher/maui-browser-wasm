using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityMediaVisualState : INotifyPropertyChanged
{
    internal enum MediaSourceMode
    {
        Uri,
        File,
        Font,
        UnsupportedStream
    }

    internal enum MediaBorderMode
    {
        SolidRound,
        DashedRectangle,
        UnsupportedGradient
    }

    private MediaSourceMode _currentSourceMode = MediaSourceMode.Uri;
    private Aspect _currentAspect = Aspect.AspectFit;
    private MediaBorderMode _currentBorderMode = MediaBorderMode.SolidRound;
    private int _imageButtonClickCount;
    private int _imageButtonPressedCount;
    private int _imageButtonReleasedCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MediaSourceMode CurrentSourceMode
    {
        get => _currentSourceMode;
        private set
        {
            if (!SetProperty(ref _currentSourceMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SourceSummary));
            OnPropertyChanged(nameof(LoadSummary));
        }
    }

    public Aspect CurrentAspect
    {
        get => _currentAspect;
        private set
        {
            if (!SetProperty(ref _currentAspect, value))
            {
                return;
            }

            OnPropertyChanged(nameof(AspectSummary));
        }
    }

    public MediaBorderMode CurrentBorderMode
    {
        get => _currentBorderMode;
        private set
        {
            if (!SetProperty(ref _currentBorderMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(BorderSummary));
        }
    }

    public int ImageButtonClickCount
    {
        get => _imageButtonClickCount;
        private set
        {
            if (!SetProperty(ref _imageButtonClickCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ImageButtonClicksSummary));
        }
    }

    public int ImageButtonPressedCount
    {
        get => _imageButtonPressedCount;
        private set
        {
            if (!SetProperty(ref _imageButtonPressedCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ImageButtonPressedSummary));
        }
    }

    public int ImageButtonReleasedCount
    {
        get => _imageButtonReleasedCount;
        private set
        {
            if (!SetProperty(ref _imageButtonReleasedCount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ImageButtonReleasedSummary));
        }
    }

    public string SourceSummary => $"Image source mode: {CurrentSourceModeLabel}";

    public string AspectSummary => $"Image aspect: {CurrentAspectLabel}";

    public string LoadSummary => CurrentSourceMode == MediaSourceMode.UnsupportedStream
        ? "Image load state expectation: unsupported fallback"
        : "Image load state expectation: loading then loaded";

    public string ImageButtonClicksSummary => $"ImageButton clicks: {ImageButtonClickCount}";

    public string ImageButtonPressedSummary => $"ImageButton pressed count: {ImageButtonPressedCount}";

    public string ImageButtonReleasedSummary => $"ImageButton released count: {ImageButtonReleasedCount}";

    public string BorderSummary => $"Border mode: {CurrentBorderModeLabel}";

    public void CycleSourceMode()
    {
        CurrentSourceMode = CurrentSourceMode switch
        {
            MediaSourceMode.Uri => MediaSourceMode.File,
            MediaSourceMode.File => MediaSourceMode.Font,
            _ => MediaSourceMode.Uri
        };
    }

    public void UseUnsupportedSourceMode()
    {
        CurrentSourceMode = MediaSourceMode.UnsupportedStream;
    }

    public void ToggleAspect()
    {
        CurrentAspect = CurrentAspect switch
        {
            Aspect.AspectFit => Aspect.AspectFill,
            Aspect.AspectFill => Aspect.Fill,
            Aspect.Fill => Aspect.Center,
            _ => Aspect.AspectFit
        };
    }

    public void ToggleBorderMode()
    {
        CurrentBorderMode = CurrentBorderMode switch
        {
            MediaBorderMode.SolidRound => MediaBorderMode.DashedRectangle,
            MediaBorderMode.DashedRectangle => MediaBorderMode.UnsupportedGradient,
            _ => MediaBorderMode.SolidRound
        };
    }

    public void RecordImageButtonClicked()
    {
        ImageButtonClickCount += 1;
    }

    public void RecordImageButtonPressed()
    {
        ImageButtonPressedCount += 1;
    }

    public void RecordImageButtonReleased()
    {
        ImageButtonReleasedCount += 1;
    }

    private string CurrentSourceModeLabel => CurrentSourceMode switch
    {
        MediaSourceMode.Uri => "uri",
        MediaSourceMode.File => "file",
        MediaSourceMode.Font => "font",
        _ => "unsupported-stream"
    };

    private string CurrentAspectLabel => CurrentAspect switch
    {
        Aspect.AspectFill => "aspect-fill",
        Aspect.Fill => "fill",
        Aspect.Center => "center",
        _ => "aspect-fit"
    };

    private string CurrentBorderModeLabel => CurrentBorderMode switch
    {
        MediaBorderMode.DashedRectangle => "dashed-rectangle",
        MediaBorderMode.UnsupportedGradient => "unsupported-gradient",
        _ => "solid-round"
    };

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
