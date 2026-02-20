using PrayTimeApp.Services;

namespace PrayTimeApp;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LanguageBadgeLabel.Text = LocalizationService.CurrentLanguageDisplay;
        ThresholdValueLabel.Text = $"{LocationService.ThresholdKm} km";
    }

    // ── Language ──────────────────────────────────────────────────────────────

    private async void OnChangeLanguageTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("SelectLanguage"),
            "Cancel", null,
            "English", "Türkçe", "Azərbaycan");

        string? langCode = result switch
        {
            "English"    => "en",
            "Türkçe"     => "tr",
            "Azərbaycan" => "az",
            _            => null
        };

        if (langCode is null || langCode == LocalizationService.CurrentLanguage) return;

        LocalizationService.SetLanguage(langCode);
        MainPage.PendingCityReload = true;   // force reload on new shell's first OnAppearing
        Application.Current!.MainPage = new AppShell();
    }

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
}
