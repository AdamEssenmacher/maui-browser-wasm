using Counter.Shared.App;

namespace Counter.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		Resources ??= new ResourceDictionary();
		Resources.MergedDictionaries.Add(new CounterStyles());
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new CounterPage());
	}
}
