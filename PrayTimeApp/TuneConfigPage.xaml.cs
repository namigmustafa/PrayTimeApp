using Nooria.Services;

namespace Nooria;

public partial class TuneConfigPage : ContentPage
{
    public TuneConfigPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadValues(PrayerTimesService.CalcTune);
    }

    private void LoadValues(string tune)
    {
        var parts = tune.Split(',');
        int[] vals = new int[9];
        for (int i = 0; i < 9; i++)
            vals[i] = i < parts.Length && int.TryParse(parts[i].Trim(), out int v) ? v : 0;

        ImsakEntry.Text        = vals[0].ToString();
        FajrEntry.Text         = vals[1].ToString();
        SunriseEntry.Text      = vals[2].ToString();
        DhuhrEntry.Text        = vals[3].ToString();
        AsrEntry.Text          = vals[4].ToString();
        MaghribEntry.Text      = vals[5].ToString();
        SunsetEntry.Text       = vals[6].ToString();
        IshaEntry.Text         = vals[7].ToString();
        MidnightTuneEntry.Text = vals[8].ToString();
    }

    private async void OnApplyTapped(object sender, TappedEventArgs e)
    {
        var entries = new[] { ImsakEntry, FajrEntry, SunriseEntry, DhuhrEntry, AsrEntry,
                              MaghribEntry, SunsetEntry, IshaEntry, MidnightTuneEntry };
        var vals = new int[9];
        for (int i = 0; i < 9; i++)
        {
            if (!int.TryParse(entries[i].Text?.Trim() ?? "0", out vals[i]))
            {
                await DisplayAlert("Error", $"Invalid value for field {i + 1}. Must be an integer.", "OK");
                return;
            }
        }
        PrayerTimesService.CalcTune = string.Join(",", vals);
        PrayerTimesService.ClearDiskCache();
        MainPage.PendingCityReload = true;
        await Shell.Current.GoToAsync("..");
    }

    private void OnResetTapped(object sender, TappedEventArgs e)
    {
        var def = PrayerTimesService.CurrentMethodDef;
        string defaults = def?.DefaultTune ?? "0,0,0,0,0,0,0,0,0";
        LoadValues(defaults);
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");
}
