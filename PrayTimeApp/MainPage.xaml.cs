using System.Globalization;
using PrayTimeApp.Services;
using NotifSvc = PrayTimeApp.Services.NotificationService;

namespace PrayTimeApp;

public partial class MainPage : ContentPage
{
    private IDispatcherTimer? _timer;
    private DateTime _countdownTarget;
    private string   _city    = "";
    private string   _country = "";
    private double   _lat;
    private double   _lon;
    private bool     _refreshing;

    // true only on fresh process start (static resets when process is killed)
    private static bool _sessionChecked = false;

    // Set to true by CitySearchPage after a city is selected
    internal static bool PendingCityReload = false;

    public MainPage()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(CitySearchPage), typeof(CitySearchPage));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_sessionChecked || PendingCityReload)
        {
            _sessionChecked   = true;
            PendingCityReload = false;
            _ = StartupAsync();
        }

        EnsureTimerRunning();

        // Show/hide the "Stop Adhan" banner based on current playback state
        AdhanPlayerService.PlayingChanged += OnAdhanPlayingChanged;
        UpdateStopBanner(AdhanPlayerService.IsPlaying);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        AdhanPlayerService.PlayingChanged -= OnAdhanPlayingChanged;
    }

    private void OnAdhanPlayingChanged(bool isPlaying) => UpdateStopBanner(isPlaying);

    private void UpdateStopBanner(bool isPlaying) =>
        StopAdhanBanner.IsVisible = isPlaying;

    private void OnStopAdhanClicked(object sender, EventArgs e) =>
        AdhanPlayerService.StopNow();

    // ── Startup: permission → load → schedule ────────────────────────────────

    private async Task StartupAsync()
    {
        await NotifSvc.RequestPermissionAsync();
        await LoadAllAsync();
    }

    // ── Location + Prayer times ───────────────────────────────────────────────

    private async Task LoadAllAsync(bool forceRefresh = false)
    {
        // ── Manual city override ──────────────────────────────────────────────
        var manual = LocationService.GetManualLocation();
        if (manual is not null)
        {
            LocationLabel.Text = Truncate(manual.CityLabel);
            _city    = manual.City;
            _country = manual.Country;
            _lat     = manual.Latitude;
            _lon     = manual.Longitude;
            await LoadPrayerTimesAsync(forceRefresh);
            return;
        }

        // ── GPS mode ──────────────────────────────────────────────────────────
        if (forceRefresh)
            LocationLabel.Text = "…";

        var info = await LocationService.GetLocationInfoAsync(forceRefresh);

        LocationLabel.Text = Truncate(info.CityLabel);

        if (string.IsNullOrWhiteSpace(info.City) || string.IsNullOrWhiteSpace(info.Country))
            return;

        _city    = info.City;
        _country = info.Country;
        _lat     = info.Latitude;
        _lon     = info.Longitude;

        bool shouldFetch = forceRefresh;

        var stored = LocationService.GetFetchCoords();

        if (stored is null)
        {
            shouldFetch = true;
        }
        else
        {
            double distKm = LocationService.HaversineDistanceKm(
                info.Latitude, info.Longitude,
                stored.Value.Lat, stored.Value.Lon);

            if (forceRefresh)
            {
                shouldFetch = true;
            }
            else if (distKm >= LocationService.ThresholdKm)
            {
                bool cityChanged =
                    !string.Equals(info.City,    stored.Value.City,    StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(info.Country, stored.Value.Country, StringComparison.OrdinalIgnoreCase);

                if (cityChanged)
                {
                    PrayerTimesService.ClearDiskCache();
                    shouldFetch = true;
                }

                LocationService.SaveFetchCoords(info.Latitude, info.Longitude, info.City, info.Country);
            }
        }

        await LoadPrayerTimesAsync(shouldFetch);

        if (shouldFetch && info.Latitude != 0)
            LocationService.SaveFetchCoords(info.Latitude, info.Longitude, info.City, info.Country);
    }

    private async Task LoadPrayerTimesAsync(bool forceRefresh = false, bool rescheduleNotifs = true)
    {
        if (string.IsNullOrWhiteSpace(_city)) return;

        // Always reset display first — prevents stale highlight from previous city
        HighlightCurrentPrayerRow("");
        CurrentPrayerName.Text = "—";

        var now   = DateTime.Now;
        var year  = now.Year;
        var month = now.Month;

        var cache = await PrayerTimesService.GetMonthAsync(
            year, month, _city, _country, _lat, _lon, forceRefresh);
        var today = PrayerTimesService.GetToday(cache);

        if (today is null) return;

        var tz = cache?.TimeZoneId ?? "";

        // Date labels — use the app's selected language for day/month names
        var dateCulture = LocalizationService.CurrentLanguage switch
        {
            "tr" => new CultureInfo("tr-TR"),
            "az" => new CultureInfo("az-AZ"),
            _    => new CultureInfo("en-US")
        };
        var ti = dateCulture.TextInfo;
        DateLabel.Text  = ti.ToTitleCase(DateTime.Today.ToString("dddd, d MMM", dateCulture));
        HijriLabel.Text = string.IsNullOrWhiteSpace(today.HijriMonth)
            ? "—"
            : $"{today.HijriMonth} {today.HijriYear} AH";

        // Prayer time labels
        FajrTime.Text    = today.Timings.Fajr;
        SunriseTime.Text = today.Timings.Sunrise;
        DhuhrTime.Text   = today.Timings.Dhuhr;
        AsrTime.Text     = today.Timings.Asr;
        MaghribTime.Text = today.Timings.Maghrib;
        IshaTime.Text    = today.Timings.Isha;

        // Yesterday for current-prayer fallback (before today's first prayer)
        PrayerDay? yesterday = null;
        if (now.Day == 1)
        {
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear  = month == 1 ? year - 1 : year;
            var prevCache = await PrayerTimesService.GetMonthAsync(prevYear, prevMonth, _city, _country, _lat, _lon);
            yesterday = prevCache?.Days.LastOrDefault();
        }
        else
        {
            yesterday = cache?.Days.FirstOrDefault(d => d.Day == now.Day - 1);
        }

        // Tomorrow for next-prayer fallback (after today's last prayer)
        PrayerDay? tomorrow = null;
        if (now.Day == DateTime.DaysInMonth(year, month))
        {
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear  = month == 12 ? year + 1 : year;
            var nextCache = await PrayerTimesService.GetMonthAsync(nextYear, nextMonth, _city, _country, _lat, _lon);
            tomorrow = nextCache?.Days.FirstOrDefault(d => d.Day == 1);
        }
        else
        {
            tomorrow = cache?.Days.FirstOrDefault(d => d.Day == now.Day + 1);
        }

        // Current prayer → card
        var current = PrayerTimesService.CurrentPrayer(today, yesterday, tz);
        if (current is not null)
        {
            CurrentPrayerName.Text = LocalizationService.GetString($"Prayer_{current.Value.Name}");
            HighlightCurrentPrayerRow(current.Value.Name);
        }

        // Next prayer → countdown target
        var next = PrayerTimesService.NextPrayer(today, tomorrow, tz);
        if (next is not null)
            _countdownTarget = next.Value.Target;

        // (Re-)schedule all alarms for the next 7 days
        PrayerMonthCache? nextMonthCache = null;
        if (now.Day > DateTime.DaysInMonth(year, month) - 6)
        {
            var nm = month == 12 ? 1 : month + 1;
            var ny = month == 12 ? year + 1 : year;
            nextMonthCache = PrayerTimesService.GetCachedMonth(ny, nm);
        }
        if (rescheduleNotifs)
            await NotifSvc.ScheduleAllAsync(cache, nextMonthCache);
    }

    // ── Row highlighting ──────────────────────────────────────────────────────

    private void HighlightCurrentPrayerRow(string prayerName)
    {
        var res         = Application.Current!.Resources;
        var gold        = (Color)res["GoldAccent"];
        var goldBg      = (Color)res["GoldBadgeBg"];
        var textPrimary = (Color)res["TextPrimary"];
        var textSecond  = (Color)res["TextSecondary"];
        var iconBg      = (Color)res["IconBg"];

        (Border row, Border icon, Label name, Label time)[] rows =
        [
            (FajrRow,    FajrIcon,    FajrNameLbl,    FajrTime),
            (SunriseRow, SunriseIcon, SunriseNameLbl, SunriseTime),
            (DhuhrRow,   DhuhrIcon,   DhuhrNameLbl,   DhuhrTime),
            (AsrRow,     AsrIcon,     AsrNameLbl,     AsrTime),
            (MaghribRow, MaghribIcon, MaghribNameLbl, MaghribTime),
            (IshaRow,    IshaIcon,    IshaNameLbl,    IshaTime),
        ];
        string[] names = ["Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha"];

        for (int i = 0; i < names.Length; i++)
        {
            bool active = !string.IsNullOrEmpty(prayerName) && names[i] == prayerName;
            var (rowB, iconB, nameLbl, timeLbl) = rows[i];

            rowB.Stroke             = active ? gold : Colors.Transparent;
            rowB.StrokeThickness    = active ? 1.5  : 0;
            iconB.BackgroundColor   = active ? goldBg   : iconBg;
            nameLbl.TextColor       = active ? gold     : textPrimary;
            timeLbl.TextColor       = active ? gold     : textSecond;
            timeLbl.FontAttributes  = active ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void EnsureTimerRunning()
    {
        if (_timer is not null) { _timer.Start(); return; }

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var remaining = _countdownTarget - DateTime.Now;

        if (remaining.TotalSeconds <= 0)
        {
            HrsLabel.Text = MinLabel.Text = SecLabel.Text = "00";
            if (!_refreshing) _ = AutoSwitchAsync();
            return;
        }

        HrsLabel.Text = ((int)remaining.TotalHours).ToString("D2");
        MinLabel.Text = remaining.Minutes.ToString("D2");
        SecLabel.Text = remaining.Seconds.ToString("D2");
    }

    private async Task AutoSwitchAsync()
    {
        _refreshing = true;
        try   { await LoadPrayerTimesAsync(rescheduleNotifs: false); }
        finally { _refreshing = false; }
    }

    // ── Location edit (city search) ───────────────────────────────────────────

    private async void OnLocationEditTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CitySearchPage));
    }


    private static string Truncate(string text, int max = 25)
        => text.Length > max ? text[..max] + "…" : text;

}
