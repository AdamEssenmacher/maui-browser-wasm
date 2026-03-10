using Microsoft.Maui.Controls;

namespace Counter.Shared.App;

public partial class CounterPage : ContentPage
{
    public CounterPage()
    {
        InitializeComponent();
        BindingContext = new CounterState();
    }
}
