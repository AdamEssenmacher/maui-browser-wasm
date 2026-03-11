using Showcase.Shared.App;

namespace Showcase.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		Resources ??= new ResourceDictionary();
		Resources.MergedDictionaries.Add(new ShowcaseStyles());
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new ShowcaseHeroPage()));
	}
}
