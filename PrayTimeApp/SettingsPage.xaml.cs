using Nooria.Services;
using NotifSvc  = Nooria.Services.NotificationService;
using FileLogger = Nooria.Services.FileLogger;

namespace Nooria;

public partial class SettingsPage : ContentPage
{
    static readonly string[] Tones =
        ["Default", "Adhan Bayati", "Apple", "Early Riser", "iPhone Alarm",
         "Revelation", "Apple Hard", "Silent"];

    int    _beforeH, _beforeM;
    int    _endsH,   _endsM;
    bool   _loading;
    string _testTone    = "Default";
    bool   _testPlaying;          // suppresses Toggled events during OnAppearing

    CancellationTokenSource? _rescheduleDebounce;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AnalyticsService.TrackScreen("SettingsPage");
        _loading = true;

        LanguageBadgeLabel.Text  = LocalizationService.CurrentLanguageDisplay;
        CalcMethodBadgeLabel.Text = CalcMethodDisplay(PrayerTimesService.CalcMethodId);
        ThresholdValueLabel.Text = $"{LocationService.ThresholdKm} km";

        // ── Before Prayer Time ────────────────────────────────────────────────
        _beforeH = Preferences.Get("notif_before_h", 0);
        _beforeM = Preferences.Get("notif_before_m", 30);
        var beforeOn = Preferences.Get("notif_before_on", false);
        BeforePrayerSwitch.IsToggled  = beforeOn;
        BeforePrayerDetail.IsVisible  = beforeOn;
        BeforePrayerToneLabel.Text    = ToneDisplayText(Preferences.Get("notif_before_tone", Tones[0]));
        RefreshBeforeLabels();

        // ── On Prayer Time ────────────────────────────────────────────────────
        var onOn = Preferences.Get("notif_on_on", false);
        OnPrayerSwitch.IsToggled = onOn;
        OnPrayerDetail.IsVisible = onOn;
        OnPrayerToneLabel.Text   = ToneDisplayText(Preferences.Get("notif_on_tone", Tones[0]));
        RefreshOnSummary(onOn);

        // ── Prayer Time Ends In ───────────────────────────────────────────────
        _endsH = Preferences.Get("notif_ends_h", 0);
        _endsM = Preferences.Get("notif_ends_m", 30);
        var endsOn = Preferences.Get("notif_ends_on", false);
        EndsInSwitch.IsToggled  = endsOn;
        EndsInDetail.IsVisible  = endsOn;
        EndsInToneLabel.Text    = ToneDisplayText(Preferences.Get("notif_ends_tone", Tones[0]));
        RefreshEndsLabels();

        _loading = false;
    }

    // ── Language ──────────────────────────────────────────────────────────────

    private async void OnChangeLanguageTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("SelectLanguage"),
            LocalizationService.GetString("Cancel"), null,
            "English", "Türkçe", "Azərbaycan", "Русский", "العربية", "Español", "Français", "Deutsch");

        string? langCode = result switch
        {
            "English"    => "en",
            "Türkçe"     => "tr",
            "Azərbaycan" => "az",
            "Русский"    => "ru",
            "العربية"    => "ar",
            "Español"    => "es",
            "Français"   => "fr",
            "Deutsch"    => "de",
            _            => null
        };

        if (langCode is null || langCode == LocalizationService.CurrentLanguage) return;

        LocalizationService.SetLanguage(langCode);
        MainPage.PendingCityReload = true;
        Application.Current!.MainPage = new AppShell();
    }

    // ── Calculation method ────────────────────────────────────────────────────

    private static string CalcMethodDisplay(int methodId)
    {
        var def = PrayerTimesService.AllMethods.FirstOrDefault(m => m.DisplayId == methodId);
        return def is null ? $"Method {methodId}" : LocalizationService.GetString(def.NameKey);
    }

    private async void OnChangeCalcMethodTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(CalcMethodPickerPage));

    private async void OnConfigureCalcMethodTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(CalcMethodConfigPage));

    // ── Location threshold ────────────────────────────────────────────────────

    private void OnThresholdDecrease(object sender, TappedEventArgs e)
    {
        LocationService.ThresholdKm -= 10;
        ThresholdValueLabel.Text = $"{LocationService.ThresholdKm} km";
    }

    private void OnThresholdIncrease(object sender, TappedEventArgs e)
    {
        LocationService.ThresholdKm += 10;
        ThresholdValueLabel.Text = $"{LocationService.ThresholdKm} km";
    }

    // ── Before Prayer Time ────────────────────────────────────────────────────

    private void OnBeforePrayerToggled(object sender, ToggledEventArgs e)
    {
        if (_loading) return;
        BeforePrayerDetail.IsVisible = e.Value;
        RefreshBeforeLabels();
        Preferences.Set("notif_before_on", e.Value);
        RescheduleNotifications();
    }

    private void OnBeforePrayerHDec(object sender, TappedEventArgs e)
    {
        if (_beforeH > 0) _beforeH--;
        Preferences.Set("notif_before_h", _beforeH);
        RefreshBeforeLabels();
        RescheduleNotifications();
    }
    private void OnBeforePrayerHInc(object sender, TappedEventArgs e)
    {
        if (_beforeH < 23) _beforeH++;
        Preferences.Set("notif_before_h", _beforeH);
        RefreshBeforeLabels();
        RescheduleNotifications();
    }
    private void OnBeforePrayerMDec(object sender, TappedEventArgs e)
    {
        if (_beforeM > 0) _beforeM--; else _beforeM = 59;
        Preferences.Set("notif_before_m", _beforeM);
        RefreshBeforeLabels();
        RescheduleNotifications();
    }
    private void OnBeforePrayerMInc(object sender, TappedEventArgs e)
    {
        _beforeM = (_beforeM + 1) % 60;
        Preferences.Set("notif_before_m", _beforeM);
        RefreshBeforeLabels();
        RescheduleNotifications();
    }

    private async void OnBeforePrayerToneTapped(object sender, TappedEventArgs e)
    {
        var cancel = LocalizationService.GetString("Cancel");
        var pick = await DisplayActionSheet(LocalizationService.GetString("Sett_SelectSound"), cancel, null, Tones);
        if (pick is null || pick == cancel) return;
        BeforePrayerToneLabel.Text = ToneDisplayText(pick);
        Preferences.Set("notif_before_tone", pick);
        RefreshBeforeLabels();
        RescheduleNotifications();
    }

    void RefreshBeforeLabels()
    {
        BeforePrayerHLabel.Text = $"{_beforeH} h";
        BeforePrayerMLabel.Text = $"{_beforeM} min";
        BeforePrayerSummary.Text = BeforePrayerSwitch.IsToggled
            ? $"{FormatOffset(_beforeH, _beforeM)} before · {Preferences.Get("notif_before_tone", Tones[0])}"
            : "Off";
    }

    // ── On Prayer Time ────────────────────────────────────────────────────────

    private void OnOnPrayerToggled(object sender, ToggledEventArgs e)
    {
        if (_loading) return;
        OnPrayerDetail.IsVisible = e.Value;
        RefreshOnSummary(e.Value);
        Preferences.Set("notif_on_on", e.Value);
        RescheduleNotifications();
    }

    private async void OnOnPrayerToneTapped(object sender, TappedEventArgs e)
    {
        var cancel = LocalizationService.GetString("Cancel");
        var pick = await DisplayActionSheet(LocalizationService.GetString("Sett_SelectSound"), cancel, null, Tones);
        if (pick is null || pick == cancel) return;
        OnPrayerToneLabel.Text = ToneDisplayText(pick);
        Preferences.Set("notif_on_tone", pick);
        RefreshOnSummary(OnPrayerSwitch.IsToggled);
        RescheduleNotifications();
    }

    void RefreshOnSummary(bool on)
    {
        OnPrayerSummary.Text = on
            ? $"At prayer time · {Preferences.Get("notif_on_tone", Tones[0])}"
            : "Off";
    }

    // ── Prayer Time Ends In ───────────────────────────────────────────────────

    private void OnEndsInToggled(object sender, ToggledEventArgs e)
    {
        if (_loading) return;
        EndsInDetail.IsVisible = e.Value;
        RefreshEndsLabels();
        Preferences.Set("notif_ends_on", e.Value);
        RescheduleNotifications();
    }

    private void OnEndsInHDec(object sender, TappedEventArgs e)
    {
        if (_endsH > 0) _endsH--;
        Preferences.Set("notif_ends_h", _endsH);
        RefreshEndsLabels();
        RescheduleNotifications();
    }
    private void OnEndsInHInc(object sender, TappedEventArgs e)
    {
        if (_endsH < 23) _endsH++;
        Preferences.Set("notif_ends_h", _endsH);
        RefreshEndsLabels();
        RescheduleNotifications();
    }
    private void OnEndsInMDec(object sender, TappedEventArgs e)
    {
        if (_endsM > 0) _endsM--; else _endsM = 59;
        Preferences.Set("notif_ends_m", _endsM);
        RefreshEndsLabels();
        RescheduleNotifications();
    }
    private void OnEndsInMInc(object sender, TappedEventArgs e)
    {
        _endsM = (_endsM + 1) % 60;
        Preferences.Set("notif_ends_m", _endsM);
        RefreshEndsLabels();
        RescheduleNotifications();
    }

    private async void OnEndsInToneTapped(object sender, TappedEventArgs e)
    {
        var cancel = LocalizationService.GetString("Cancel");
        var pick = await DisplayActionSheet(LocalizationService.GetString("Sett_SelectSound"), cancel, null, Tones);
        if (pick is null || pick == cancel) return;
        EndsInToneLabel.Text = ToneDisplayText(pick);
        Preferences.Set("notif_ends_tone", pick);
        RefreshEndsLabels();
        RescheduleNotifications();
    }

    void RefreshEndsLabels()
    {
        EndsInHLabel.Text = $"{_endsH} h";
        EndsInMLabel.Text = $"{_endsM} min";
        EndsInSummary.Text = EndsInSwitch.IsToggled
            ? $"{FormatOffset(_endsH, _endsM)} remaining · {Preferences.Get("notif_ends_tone", Tones[0])}"
            : "Off";
    }

    // ── Reschedule ────────────────────────────────────────────────────────────

    private async void RescheduleNotifications()
    {
        // Debounce: if another call arrives within 600 ms, cancel this one.
        _rescheduleDebounce?.Cancel();
        var cts = _rescheduleDebounce = new CancellationTokenSource();
        try   { await Task.Delay(600, cts.Token); }
        catch (OperationCanceledException) { return; }

        var today = DateTime.Today;
        var cur   = PrayerTimesService.GetCachedMonth(today.Year, today.Month);

        // If not in memory, load from disk using the last-known location
        if (cur is null)
        {
            double lat = 0, lon = 0;
            string city = "", country = "";

            var manual = LocationService.GetManualLocation();
            if (manual is not null)
            {
                lat = manual.Latitude;  lon = manual.Longitude;
                city = manual.City;     country = manual.Country;
            }
            else
            {
                var fc = LocationService.GetFetchCoords();
                if (fc is not null)
                {
                    lat = fc.Value.Lat;    lon = fc.Value.Lon;
                    city = fc.Value.City;  country = fc.Value.Country;
                }
            }

            if (!string.IsNullOrEmpty(city))
                cur = await PrayerTimesService.GetMonthAsync(
                    today.Year, today.Month, city, country, lat, lon);
        }

        var nm   = today.Month == 12 ? 1 : today.Month + 1;
        var ny   = today.Month == 12 ? today.Year + 1 : today.Year;
        var next = PrayerTimesService.GetCachedMonth(ny, nm);

        FileLogger.Log($"Reschedule: cur={( cur == null ? "NULL" : $"{cur.Year}/{cur.Month} {cur.Days.Count}d")}");
        await NotifSvc.ScheduleAllAsync(cur, next);
    }

    // ── Play / Pause sound test ───────────────────────────────────────────────

    private async void OnSelectTestToneTapped(object sender, TappedEventArgs e)
    {
        if (_testPlaying) StopTestSound();
        var cancel = LocalizationService.GetString("Cancel");
        var pick = await DisplayActionSheet(LocalizationService.GetString("Sett_SelectSound"), cancel, null, Tones);
        if (pick is null || pick == cancel) return;
        _testTone = pick;
        TestToneLabel.Text = ToneDisplayText(pick);
    }

    private void OnPlayStopTapped(object sender, TappedEventArgs e)
    {
        // If the actual player already stopped (sound ended naturally), reset state first
        if (_testPlaying && !NotifSvc.IsSoundPlaying)
        {
            _testPlaying = false;
            PlayStopIcon.Text = "▶";
            PlayStopBtn.BackgroundColor = (Color)Application.Current!.Resources["GoldAccent"];
            return;
        }

        if (_testPlaying)
            StopTestSound();
        else
            StartTestSound();
    }

    void StartTestSound()
    {
        NotifSvc.PlaySoundNow(_testTone);
        _testPlaying = true;
        PlayStopIcon.Text = "⏹";
        PlayStopBtn.BackgroundColor = Color.FromArgb("#C0392B");
    }

    void StopTestSound()
    {
        NotifSvc.StopSoundNow();
        _testPlaying = false;
        PlayStopIcon.Text = "▶";
        PlayStopBtn.BackgroundColor = (Color)Application.Current!.Resources["GoldAccent"];
    }


    // ── Helpers ───────────────────────────────────────────────────────────────

    static string FormatOffset(int h, int m)
    {
        if (h == 0) return $"{m} min";
        if (m == 0) return $"{h} h";
        return $"{h} h {m} min";
    }

    static string ToneDisplayText(string tone)
    {
        var file = NotifSvc.GetDisplayFileName(tone);
        return string.IsNullOrEmpty(file) ? tone : $"{tone} - {file}";
    }
}
