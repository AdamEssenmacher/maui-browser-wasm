using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Showcase.Shared.App;

public partial class ShowcaseMapLabPage : ContentPage
{
    private static readonly MapSpan SanFranciscoRegion =
        MapSpan.FromCenterAndRadius(new Location(37.7749, -122.4194), Distance.FromKilometers(8));

    private static readonly MapSpan SeattleRegion =
        MapSpan.FromCenterAndRadius(new Location(47.6062, -122.3321), Distance.FromKilometers(9));

    private readonly Pin _hqPin = new()
    {
        Label = "Mirage HQ",
        Address = "San Francisco",
        Location = new Location(37.7896, -122.3952),
        Type = PinType.Place
    };

    private readonly Pin _teamPin = new()
    {
        Label = "Provider Team",
        Address = "North Beach",
        Location = new Location(37.8067, -122.4128),
        Type = PinType.Place
    };

    private bool _showingSeattle;

    public ShowcaseMapLabPage()
    {
        InitializeComponent();
        ShowcaseMap.Pins.Add(_hqPin);
        ShowcaseMap.Pins.Add(_teamPin);
        RegionSummaryLabel.Text = "Region: San Francisco";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyRegionProjection(moveRequested: true);
    }

    private void OnMoveRegionClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _showingSeattle = !_showingSeattle;
        ApplyRegionProjection(moveRequested: true);
    }

    private void ApplyRegionProjection(bool moveRequested)
    {
        var targetRegion = _showingSeattle ? SeattleRegion : SanFranciscoRegion;
        if (moveRequested)
        {
            ShowcaseMap.MoveToRegion(targetRegion);
        }

        if (_showingSeattle)
        {
            _hqPin.Label = "Mirage PNW";
            _hqPin.Address = "Seattle";
            _hqPin.Location = new Location(47.6062, -122.3321);
            _teamPin.Label = "Provider Team";
            _teamPin.Address = "Pike Place";
            _teamPin.Location = new Location(47.6101, -122.3421);
            RegionSummaryLabel.Text = "Region: Seattle";
        }
        else
        {
            _hqPin.Label = "Mirage HQ";
            _hqPin.Address = "San Francisco";
            _hqPin.Location = new Location(37.7896, -122.3952);
            _teamPin.Label = "Provider Team";
            _teamPin.Address = "North Beach";
            _teamPin.Location = new Location(37.8067, -122.4128);
            RegionSummaryLabel.Text = "Region: San Francisco";
        }
    }
}
