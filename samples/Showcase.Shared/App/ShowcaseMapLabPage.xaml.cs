using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Graphics;
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

    private readonly Circle _coverageCircle = new()
    {
        StrokeColor = Colors.DodgerBlue,
        FillColor = Colors.LightBlue,
        StrokeWidth = 3f,
        Radius = Distance.FromKilometers(2.5),
        Center = new Location(37.7896, -122.3952)
    };

    private readonly Polyline _routePolyline = new()
    {
        StrokeColor = Colors.OrangeRed,
        StrokeWidth = 4f
    };

    private readonly Polygon _districtPolygon = new()
    {
        StrokeColor = Colors.MediumSeaGreen,
        StrokeWidth = 3f,
        FillColor = Colors.PaleGreen
    };

    private bool _showingSeattle;
    private int _mapClickCount;
    private int _pinClickCount;
    private int _infoClickCount;

    public ShowcaseMapLabPage()
    {
        InitializeComponent();

        ShowcaseMap.Pins.Add(_hqPin);
        ShowcaseMap.Pins.Add(_teamPin);

        ShowcaseMap.MapElements.Add(_coverageCircle);
        ShowcaseMap.MapElements.Add(_routePolyline);
        ShowcaseMap.MapElements.Add(_districtPolygon);

        ShowcaseMap.MapClicked += OnMapClicked;
        _hqPin.MarkerClicked += OnPinMarkerClicked;
        _hqPin.InfoWindowClicked += OnPinInfoWindowClicked;
        _teamPin.MarkerClicked += OnPinMarkerClicked;
        _teamPin.InfoWindowClicked += OnPinInfoWindowClicked;

        UpdateMapElements();
        UpdateSummaryLabels();
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

    private void OnCycleMapTypeClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        ShowcaseMap.MapType = ShowcaseMap.MapType switch
        {
            MapType.Street => MapType.Satellite,
            MapType.Satellite => MapType.Hybrid,
            _ => MapType.Street
        };

        UpdateSummaryLabels();
    }

    private void OnToggleScrollClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        ShowcaseMap.IsScrollEnabled = !ShowcaseMap.IsScrollEnabled;
        UpdateSummaryLabels();
    }

    private void OnToggleZoomClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        ShowcaseMap.IsZoomEnabled = !ShowcaseMap.IsZoomEnabled;
        UpdateSummaryLabels();
    }

    private void OnToggleUserLocationClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        ShowcaseMap.IsShowingUser = !ShowcaseMap.IsShowingUser;
        UpdateSummaryLabels();
    }

    private void OnToggleTrafficClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        ShowcaseMap.IsTrafficEnabled = !ShowcaseMap.IsTrafficEnabled;
        UpdateSummaryLabels();
    }

    private void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        _ = sender;
        _ = e;
        _mapClickCount++;
        UpdateSummaryLabels();
    }

    private void OnPinMarkerClicked(object? sender, PinClickedEventArgs e)
    {
        _ = sender;
        _pinClickCount++;
        // Keep MAUI parity path exercised: marker click handlers can request popup dismissal.
        e.HideInfoWindow = true;
        UpdateSummaryLabels();
    }

    private void OnPinInfoWindowClicked(object? sender, PinClickedEventArgs e)
    {
        _ = sender;
        _infoClickCount++;
        e.HideInfoWindow = true;
        UpdateSummaryLabels();
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
        }
        else
        {
            _hqPin.Label = "Mirage HQ";
            _hqPin.Address = "San Francisco";
            _hqPin.Location = new Location(37.7896, -122.3952);
            _teamPin.Label = "Provider Team";
            _teamPin.Address = "North Beach";
            _teamPin.Location = new Location(37.8067, -122.4128);
        }

        UpdateMapElements();
        UpdateSummaryLabels();
    }

    private void UpdateMapElements()
    {
        if (_showingSeattle)
        {
            _coverageCircle.Center = new Location(47.6062, -122.3321);
            _coverageCircle.Radius = Distance.FromKilometers(2.8);

            ReplaceGeopath(
                _routePolyline.Geopath,
                new Location(47.6101, -122.3421),
                new Location(47.6062, -122.3321),
                new Location(47.5984, -122.3341));

            ReplaceGeopath(
                _districtPolygon.Geopath,
                new Location(47.6151, -122.3425),
                new Location(47.6042, -122.3542),
                new Location(47.5961, -122.3387),
                new Location(47.6063, -122.3244));
            return;
        }

        _coverageCircle.Center = new Location(37.7896, -122.3952);
        _coverageCircle.Radius = Distance.FromKilometers(2.5);

        ReplaceGeopath(
            _routePolyline.Geopath,
            new Location(37.7896, -122.3952),
            new Location(37.7981, -122.4075),
            new Location(37.8067, -122.4128));

        ReplaceGeopath(
            _districtPolygon.Geopath,
            new Location(37.8060, -122.4178),
            new Location(37.7926, -122.4210),
            new Location(37.7885, -122.4014),
            new Location(37.8012, -122.3927));
    }

    private static void ReplaceGeopath(IList<Location> geopath, params Location[] points)
    {
        geopath.Clear();
        foreach (var point in points)
        {
            geopath.Add(point);
        }
    }

    private void UpdateSummaryLabels()
    {
        RegionSummaryLabel.Text = _showingSeattle ? "Region: Seattle" : "Region: San Francisco";
        OptionsSummaryLabel.Text =
            $"MapType={ShowcaseMap.MapType} | Scroll={ShowcaseMap.IsScrollEnabled} | Zoom={ShowcaseMap.IsZoomEnabled} | User={ShowcaseMap.IsShowingUser} | Traffic={ShowcaseMap.IsTrafficEnabled}";
        InteractionSummaryLabel.Text =
            $"Map clicks={_mapClickCount} | Pin clicks={_pinClickCount} | Info clicks={_infoClickCount}";
    }
}
