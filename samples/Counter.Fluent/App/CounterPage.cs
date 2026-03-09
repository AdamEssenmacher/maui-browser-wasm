using Microsoft.Maui.Controls;

namespace Counter.Fluent.App;

public partial class CounterPage : ContentPage
{
    public CounterPage()
    {
        InitializeComponent();
        BindingContext = new CounterState();
    }
}
