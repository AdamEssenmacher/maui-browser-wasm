using Mirage.Maui.HotReload;
using Microsoft.Maui.Controls;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class XamlHotReloadApplicatorTests
{
    [Fact]
    public void ReapplyingElementXaml_RebindsXNameFieldsWithoutDuplicateNameScopeErrors()
    {
        var page = new ProbePage();

        const string firstXaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <Label x:Name="TitleProbe" Text="First pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        const string secondXaml = """
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <VerticalStackLayout>
                    <Label x:Name="TitleProbe" Text="Second pass" />
                </VerticalStackLayout>
            </ContentPage>
            """;

        var firstApplied = MirageXamlHotReloadApplicator.TryApplyElementXaml(page, firstXaml, out var firstError);
        Assert.True(firstApplied, firstError?.ToString());
        Assert.Equal("First pass", page.TitleProbeField?.Text);

        var secondApplied = MirageXamlHotReloadApplicator.TryApplyElementXaml(page, secondXaml, out var secondError);
        Assert.True(secondApplied, secondError?.ToString());
        Assert.Equal("Second pass", page.TitleProbeField?.Text);
    }

    [Fact]
    public void ReapplyingResourceDictionaryXaml_ClearsAndReloadsEntries()
    {
        var dictionary = new ResourceDictionary();

        const string firstXaml = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Color x:Key="AccentColor">#FF0000</Color>
            </ResourceDictionary>
            """;

        const string secondXaml = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Color x:Key="AccentColor">#00FF00</Color>
            </ResourceDictionary>
            """;

        var firstApplied = MirageXamlHotReloadApplicator.TryApplyResourceDictionaryXaml(dictionary, firstXaml, out var firstError);
        Assert.True(firstApplied, firstError?.ToString());
        Assert.True(dictionary.ContainsKey("AccentColor"));

        var firstColor = Assert.IsType<Color>(dictionary["AccentColor"]);
        Assert.InRange(firstColor.Red, 0.99f, 1.0f);
        Assert.InRange(firstColor.Green, 0.0f, 0.01f);

        var secondApplied = MirageXamlHotReloadApplicator.TryApplyResourceDictionaryXaml(dictionary, secondXaml, out var secondError);
        Assert.True(secondApplied, secondError?.ToString());
        Assert.True(dictionary.ContainsKey("AccentColor"));

        var secondColor = Assert.IsType<Color>(dictionary["AccentColor"]);
        Assert.InRange(secondColor.Red, 0.0f, 0.01f);
        Assert.InRange(secondColor.Green, 0.99f, 1.0f);
    }

    private sealed class ProbePage : ContentPage
    {
#pragma warning disable CS0649
        private Label? TitleProbe;
#pragma warning restore CS0649

        public Label? TitleProbeField => TitleProbe;
    }
}
