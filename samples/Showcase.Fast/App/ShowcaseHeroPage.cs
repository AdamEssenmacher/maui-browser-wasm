using Mirage.Web.Runtime;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Fast.App;

public partial class ShowcaseHeroPage : ContentPage
{
    private const string BacklogTableHostName = "BacklogTableHost";

    private readonly IMiragePageNavigator _pageNavigator;
    private readonly ShowcaseState _state;

    public ShowcaseHeroPage(IMiragePageNavigator pageNavigator)
    {
        _pageNavigator = pageNavigator;
        _state = new ShowcaseState();
        BindingContext = _state;
        LoadXamlFromResource();
        BuildBacklogTable();
    }

    private void OnDemoNavClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("demo");
    }

    private void OnArchitectureNavClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("architecture");
    }

    private void OnProvidersNavClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("providers");
    }

    private void OnDemoCtaClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("demo");
    }

    private void OnArchitectureCtaClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("architecture");
    }

    private void OnOpenContentViewDemoClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ScrollTo("content-view-demo-page");
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseHeroPage));
#pragma warning restore IL2026
    }

    private void ScrollTo(string sectionId)
    {
        _ = _pageNavigator.ScrollToAutomationId(sectionId, smooth: true);
    }

    private void OnGenericWidgetClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _state.RegisterGenericWidgetClick();
    }

    private void BuildBacklogTable()
    {
        var backlogTableHost = BacklogTableHost ?? this.FindByName<ContentView>(BacklogTableHostName);
        if (backlogTableHost is null)
        {
            return;
        }

        var table = new Grid
        {
            AutomationId = "showcase-backlog-table",
            ColumnSpacing = 0,
            RowSpacing = 0,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.9, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(3.2, GridUnitType.Star) }
            }
        };

        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddHeaderCell(table, row: 0, column: 0, "maui-type", "MAUI Type");
        AddHeaderCell(table, row: 0, column: 1, "category", "Category");
        AddHeaderCell(table, row: 0, column: 2, "status", "Status");
        AddHeaderCell(table, row: 0, column: 3, "notes", "Notes");

        var rowIndex = 1;
        foreach (var row in ShowcaseBacklogCatalog.Rows)
        {
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddRowMarker(table, row, rowIndex);
            AddDataCell(table, rowIndex, 0, $"{row.Key}-maui-type", row.MauiType);
            AddDataCell(table, rowIndex, 1, $"{row.Key}-category", row.Category);
            AddStatusCell(table, rowIndex, row.Key, row.Status);
            AddDataCell(table, rowIndex, 3, $"{row.Key}-notes", row.Notes);
            rowIndex++;
        }

        backlogTableHost.Content = table;
    }

    private static void AddHeaderCell(Grid table, int row, int column, string key, string text)
    {
        var label = new Label
        {
            AutomationId = $"showcase-backlog-cell-header-{key}",
            Text = text
        };

        Grid.SetRow(label, row);
        Grid.SetColumn(label, column);
        table.Children.Add(label);
    }

    private void AddStatusCell(Grid table, int row, string key, ShowcaseBacklogStatus status)
    {
        var button = new Button
        {
            AutomationId = $"showcase-backlog-cell-{key}-status-{ToStatusSlug(status)}",
            Text = ToStatusLabel(status),
            Style = ResolveStatusStyle(status)
        };

        Grid.SetRow(button, row);
        Grid.SetColumn(button, 2);
        table.Children.Add(button);
    }

    private static void AddRowMarker(Grid table, ShowcaseBacklogRow row, int rowIndex)
    {
        var marker = new ContentView
        {
            AutomationId = $"showcase-backlog-row-{row.Key}",
            InputTransparent = true
        };

        Grid.SetRow(marker, rowIndex);
        Grid.SetColumn(marker, 0);
        Grid.SetColumnSpan(marker, 4);
        table.Children.Add(marker);
    }

    private static void AddDataCell(Grid table, int row, int column, string key, string text)
    {
        var label = new Label
        {
            AutomationId = $"showcase-backlog-cell-{key}",
            Text = text
        };

        Grid.SetRow(label, row);
        Grid.SetColumn(label, column);
        table.Children.Add(label);
    }

    private static string ToStatusLabel(ShowcaseBacklogStatus status)
    {
        return status switch
        {
            ShowcaseBacklogStatus.Complete => "Fully Implemented (Complete)",
            ShowcaseBacklogStatus.Started => "Partially Implemented (Started)",
            _ => "Not Implemented (Not Started)"
        };
    }

    private static string ToStatusSlug(ShowcaseBacklogStatus status)
    {
        return status switch
        {
            ShowcaseBacklogStatus.Complete => "complete",
            ShowcaseBacklogStatus.Started => "started",
            _ => "not-started"
        };
    }

    private Style? ResolveStatusStyle(ShowcaseBacklogStatus status)
    {
        var styleKey = status switch
        {
            ShowcaseBacklogStatus.Complete => "ShowcaseBacklogStatusCompleteButtonStyle",
            ShowcaseBacklogStatus.Started => "ShowcaseBacklogStatusStartedButtonStyle",
            _ => "ShowcaseBacklogStatusNotStartedButtonStyle"
        };

        if (Resources.TryGetValue(styleKey, out var pageStyle)
            && pageStyle is Style typedPageStyle)
        {
            return typedPageStyle;
        }

        if (Application.Current?.Resources.TryGetValue(styleKey, out var appStyle) == true
            && appStyle is Style typedAppStyle)
        {
            return typedAppStyle;
        }

        return null;
    }
}
