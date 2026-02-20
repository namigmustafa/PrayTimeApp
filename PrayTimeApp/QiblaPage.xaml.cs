using PrayTimeApp.Services;

namespace PrayTimeApp;

public partial class QiblaPage : ContentPage
{
    // Mecca coordinates
    private const double MeccaLat = 21.4225;
    private const double MeccaLon = 39.8262;

    private double _qiblaBearing;
    private bool   _compassStarted;

    public QiblaPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCompass();
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        double lat, lon;

        var manual = LocationService.GetManualLocation();
        if (manual is not null)
        {
            lat = manual.Latitude;
            lon = manual.Longitude;
            LocationLabel.Text = manual.CityLabel;
        }
        else
        {
            var info = await LocationService.GetLocationInfoAsync();
            lat = info.Latitude;
            lon = info.Longitude;
            LocationLabel.Text = info.CityLabel;
        }

        if (lat == 0 && lon == 0)
        {
            StatusLabel.Text = "Location unavailable";
            return;
        }

        _qiblaBearing      = CalculateBearing(lat, lon, MeccaLat, MeccaLon);
        BearingLabel.Text  = $"{_qiblaBearing:F1}°";

        StartCompass();
    }

    // ── Compass ───────────────────────────────────────────────────────────────

    private void StartCompass()
    {
        if (_compassStarted) return;

        if (!Compass.Default.IsSupported)
        {
            // No magnetometer — show static arrow at qibla bearing
            QiblaArrow.Rotation  = _qiblaBearing;
            StatusLabel.Text     = "No compass sensor";
            InstructionLabel.Text = $"Qibla is {_qiblaBearing:F0}° from North";
            return;
        }

        Compass.Default.ReadingChanged += OnCompassReading;
        Compass.Default.Start(SensorSpeed.UI);
        _compassStarted       = true;
        InstructionLabel.Text = LocalizationService.GetString("QiblaInstruction");
    }

    private void StopCompass()
    {
        if (!_compassStarted) return;
        Compass.Default.ReadingChanged -= OnCompassReading;
        if (Compass.Default.IsMonitoring)
            Compass.Default.Stop();
        _compassStarted = false;
    }

    private void OnCompassReading(object? sender, CompassChangedEventArgs e)
    {
        // Arrow angle: how many degrees clockwise from "up" the Qibla arrow must point
        var heading     = e.Reading.HeadingMagneticNorth;
        var arrowAngle  = (_qiblaBearing - heading + 360) % 360;

        MainThread.BeginInvokeOnMainThread(() =>
            QiblaArrow.Rotation = arrowAngle);
    }

    // ── Bearing calculation ───────────────────────────────────────────────────

    private static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var y = Math.Sin(Δλ) * Math.Cos(φ2);
        var x = Math.Cos(φ1) * Math.Sin(φ2) - Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(Δλ);

        return (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
    }
}
