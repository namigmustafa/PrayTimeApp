using Nooria.Services;

namespace Nooria;

public partial class MonthlyPage : ContentPage
{
    private CancellationTokenSource? _skeletonCts;
    private bool _noLocationShown = false;

    public MonthlyPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AnalyticsService.TrackScreen("MonthlyPage");
        _ = LoadAsync();
    }

    private void ShowSkeleton()
    {
        _skeletonCts?.Cancel();
        _skeletonCts = new CancellationTokenSource();
        SkeletonView.IsVisible = true;
        SkeletonView.Opacity   = 1.0;
        MainContent.IsVisible  = false;
        _ = PulseSkeletonAsync(_skeletonCts.Token);
    }

    private void HideSkeleton()
    {
        _skeletonCts?.Cancel();
        SkeletonView.IsVisible = false;
        SkeletonView.Opacity   = 1.0;
        MainContent.IsVisible  = true;
    }

    private async Task PulseSkeletonAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await SkeletonView.FadeTo(0.3, 600, Easing.SinInOut);
            if (ct.IsCancellationRequested) break;
            await SkeletonView.FadeTo(1.0, 600, Easing.SinInOut);
        }
    }

    private async Task LoadAsync()
    {
        ShowSkeleton();
        // Manual city takes priority over GPS
        string city, country, cityLabel;
        double lat = 0, lon = 0;

        var manual = LocationService.GetManualLocation();
        if (manual is not null)
        {
            city      = manual.City;
            country   = manual.Country;
            cityLabel = manual.CityLabel;
            lat       = manual.Latitude;
            lon       = manual.Longitude;
        }
        else
        {
            var info = await LocationService.GetLocationInfoAsync();
            city      = info.City;
            country   = info.Country;
            cityLabel = info.CityLabel;
            lat       = info.Latitude;
            lon       = info.Longitude;
        }

        LocationLabel.Text = cityLabel;

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
        {
            HideSkeleton();
            if (!_noLocationShown)
            {
                _noLocationShown = true;
                await Shell.Current.GoToAsync(nameof(NoLocationPage));
            }
            return;
        }
        _noLocationShown = false;

        var now   = DateTime.Now;
        var cache = await PrayerTimesService.GetMonthAsync(now.Year, now.Month, city, country, lat, lon);
        if (cache is null) { HideSkeleton(); return; }

        // Update month header
        var dateCulture = LocalizationService.CurrentLanguage switch
        {
            "tr" => new System.Globalization.CultureInfo("tr-TR"),
            "az" => new System.Globalization.CultureInfo("az-AZ"),
            _    => new System.Globalization.CultureInfo("en-US")
        };
        MonthYearLabel.Text = dateCulture.TextInfo.ToTitleCase(
            new DateTime(cache.Year, cache.Month, 1).ToString("MMMM yyyy", dateCulture));

        // Build hijri month label from today's entry (or first day)
        var today     = PrayerTimesService.GetToday(cache);
        var reference = today ?? cache.Days.FirstOrDefault();
        HijriMonthLabel.Text = reference is not null && !string.IsNullOrWhiteSpace(reference.HijriMonth)
            ? $"{reference.HijriMonth} {reference.HijriYear} AH"
            : "—";

        BuildTable(cache, now.Day);
        HideSkeleton();
    }

    private void BuildTable(PrayerMonthCache cache, int todayDay)
    {
        PrayerTableRows.Children.Clear();

        var dateCulture = LocalizationService.CurrentLanguage switch
        {
            "tr" => new System.Globalization.CultureInfo("tr-TR"),
            "az" => new System.Globalization.CultureInfo("az-AZ"),
            _    => new System.Globalization.CultureInfo("en-US")
        };
        var monthAbbr = dateCulture.TextInfo.ToTitleCase(
            new DateTime(cache.Year, cache.Month, 1).ToString("MMM", dateCulture));
        var isToday   = false;

        foreach (var day in cache.Days.OrderBy(d => d.Day))
        {
            isToday = day.Day == todayDay;

            var dateText = $"{day.Day} {monthAbbr}";

            Color dateFg   = isToday
                ? (Color)Application.Current!.Resources["GoldAccent"]
                : (Color)Application.Current!.Resources["TextPrimary"];

            FontAttributes dateAttrs = isToday ? FontAttributes.Bold : FontAttributes.None;

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    new ColumnDefinition(new GridLength(50)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                ),
                Padding = new Thickness(0, 10)
            };

            row.Add(MakeCell(dateText,             dateFg, dateAttrs,           0, center: false));
            row.Add(MakeCell(day.Timings.Fajr,    dateFg, FontAttributes.None, 1));
            row.Add(MakeCell(day.Timings.Sunrise,  dateFg, FontAttributes.None, 2));
            row.Add(MakeCell(day.Timings.Dhuhr,   dateFg, FontAttributes.None, 3));
            row.Add(MakeCell(day.Timings.Asr,     dateFg, FontAttributes.None, 4));
            row.Add(MakeCell(day.Timings.Maghrib,
                (Color)Application.Current!.Resources["GoldAccent"],
                FontAttributes.Bold, 5));
            row.Add(MakeCell(day.Timings.Isha,    dateFg, FontAttributes.None, 6));

            PrayerTableRows.Children.Add(row);

            var divider = new BoxView
            {
                HeightRequest = 1,
                Color = (Color)Application.Current!.Resources["GreenCardBorder"]
            };
            PrayerTableRows.Children.Add(divider);
        }

        // Remove last divider so the card bottom looks clean
        if (PrayerTableRows.Children.Count > 0)
            PrayerTableRows.Children.RemoveAt(PrayerTableRows.Children.Count - 1);
    }

    private static Label MakeCell(string text, Color fg, FontAttributes attrs, int column, bool center = true)
    {
        var lbl = new Label
        {
            Text                  = text,
            TextColor             = fg,
            FontSize              = 13,
            FontAttributes        = attrs,
            VerticalOptions       = LayoutOptions.Center,
            HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
        };
        Grid.SetColumn(lbl, column);
        return lbl;
    }
}
