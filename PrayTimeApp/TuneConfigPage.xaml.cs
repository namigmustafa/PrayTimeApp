using Nooria.Services;

namespace Nooria;

public partial class TuneConfigPage : ContentPage
{
    private int[] _vals = new int[9];

    // Sıra: imsak, fajr, sunrise, dhuhr, asr, maghrib, sunset, isha, midnight
    private Label[] ValueLabels => [ImsakValue, FajrValue, SunriseValue, DhuhrValue,
                                     AsrValue, MaghribValue, SunsetValue, IshaValue, MidnightValue];

    public TuneConfigPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var cfg = CalcMethodConfigService.GetConfig(PrayerTimesService.CalcMethodId);
        LoadValues(cfg.Tune);
    }

    private void LoadValues(string tune)
    {
        var parts = tune.Split(',');
        for (int i = 0; i < 9; i++)
            _vals[i] = i < parts.Length && int.TryParse(parts[i].Trim(), out int v) ? v : 0;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        var labels = ValueLabels;
        for (int i = 0; i < 9; i++)
            labels[i].Text = _vals[i].ToString();
    }

    private void Step(int index, int delta)
    {
        _vals[index] += delta;
        ValueLabels[index].Text = _vals[index].ToString();
    }

    // Imsak
    private void OnImsakMinus(object s, TappedEventArgs e)   => Step(0, -1);
    private void OnImsakPlus(object s, TappedEventArgs e)    => Step(0,  1);
    // Fajr
    private void OnFajrMinus(object s, TappedEventArgs e)    => Step(1, -1);
    private void OnFajrPlus(object s, TappedEventArgs e)     => Step(1,  1);
    // Sunrise
    private void OnSunriseMinus(object s, TappedEventArgs e) => Step(2, -1);
    private void OnSunrisePlus(object s, TappedEventArgs e)  => Step(2,  1);
    // Dhuhr
    private void OnDhuhrMinus(object s, TappedEventArgs e)   => Step(3, -1);
    private void OnDhuhrPlus(object s, TappedEventArgs e)    => Step(3,  1);
    // Asr
    private void OnAsrMinus(object s, TappedEventArgs e)     => Step(4, -1);
    private void OnAsrPlus(object s, TappedEventArgs e)      => Step(4,  1);
    // Maghrib
    private void OnMaghribMinus(object s, TappedEventArgs e) => Step(5, -1);
    private void OnMaghribPlus(object s, TappedEventArgs e)  => Step(5,  1);
    // Sunset
    private void OnSunsetMinus(object s, TappedEventArgs e)  => Step(6, -1);
    private void OnSunsetPlus(object s, TappedEventArgs e)   => Step(6,  1);
    // Isha
    private void OnIshaMinus(object s, TappedEventArgs e)    => Step(7, -1);
    private void OnIshaPlus(object s, TappedEventArgs e)     => Step(7,  1);
    // Midnight
    private void OnMidnightMinus(object s, TappedEventArgs e) => Step(8, -1);
    private void OnMidnightPlus(object s, TappedEventArgs e)  => Step(8,  1);

    private async void OnApplyTapped(object sender, TappedEventArgs e)
    {
        var tune = string.Join(",", _vals);
        var cfg = CalcMethodConfigService.GetConfig(PrayerTimesService.CalcMethodId);
        cfg.Tune = tune;
        CalcMethodConfigService.SaveConfig(PrayerTimesService.CalcMethodId, cfg);
        PrayerTimesService.ClearDiskCache();
        MainPage.PendingCityReload = true;
        await Shell.Current.GoToAsync("..");
    }

    private void OnResetTapped(object sender, TappedEventArgs e)
    {
        var def = CalcMethodConfigService.GetDefaultConfig(PrayerTimesService.CalcMethodId);
        LoadValues(def.Tune);
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");
}
