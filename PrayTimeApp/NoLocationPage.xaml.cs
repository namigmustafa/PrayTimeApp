using Nooria.Services;

namespace Nooria;

public partial class NoLocationPage : ContentPage
{
    private CancellationTokenSource? _debounce;
    private bool _waitingForSettings = false;

    public NoLocationPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // If another tab already selected a city, dismiss ourselves immediately
        // so the underlying page can reload with the new location.
        if (LocationService.GetManualLocation() is not null)
        {
            MainPage.PendingCityReload = true;
            await Shell.Current.GoToAsync("..");
            return;
        }

        ApplyTranslations();
        if (Window is not null)
            Window.Resumed += OnWindowResumed;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Window is not null)
            Window.Resumed -= OnWindowResumed;
    }

    // ── Translations ──────────────────────────────────────────────────────────

    private void ApplyTranslations()
    {
        var S = LocalizationService.GetString;

        LanguageBadgeLabel.Text = LocalizationService.CurrentLanguageDisplay;
        TitleLabel.Text         = S("NoLocationTitle");
        DescLabel.Text          = S("NoLocationDesc");
        UseLocationLabel.Text   = "📍  " + S("UseCurrentLocation");
        OrLabel.Text            = $" {S("Or")} ";
        NoResultsLabel.Text     = S("NoResults");
        SearchEntry.Placeholder = S("SearchCityHint");
    }

    // ── Language selection (same as SettingsPage) ─────────────────────────────

    private async void OnChangeLanguageTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("SelectLanguage"),
            LocalizationService.GetString("Cancel"), null,
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
        ApplyTranslations();
    }

    // ── Window resume (return from device Settings) ───────────────────────────

    private async void OnWindowResumed(object? sender, EventArgs e)
    {
        if (!_waitingForSettings) return;
        _waitingForSettings = false;

        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
            await ProceedWithGpsAsync();
    }

    // ── Use current location ──────────────────────────────────────────────────

    private async void OnUseCurrentLocationTapped(object sender, TappedEventArgs e)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Denied)
        {
            var S = LocalizationService.GetString;
            bool open = await DisplayAlert(
                S("LocationPermission"),
                S("LocationPermDenied"),
                S("OpenSettings"),
                S("Cancel"));

            if (open)
            {
                _waitingForSettings = true;
                AppInfo.Current.ShowSettingsUI();
            }

            return;
        }

        // Not yet asked — try requesting
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
            return;     // still not granted, stay on page

        await ProceedWithGpsAsync();
    }

    private async Task ProceedWithGpsAsync()
    {
        LocationService.ClearManualLocation();
        LocationService.ClearCache();           // force fresh GPS on next load
        MainPage.PendingCityReload = true;
        await Shell.Current.GoToAsync("..");
    }

    // ── Debounced search ──────────────────────────────────────────────────────

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;

        var query = e.NewTextValue?.Trim() ?? "";

        if (query.Length < 2)
        {
            ResultsList.ItemsSource    = null;
            ResultsList.IsVisible      = false;
            NoResultsLabel.IsVisible   = false;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            return;
        }

        _ = Task.Run(async () =>
        {
            try { await Task.Delay(400, token); }
            catch (OperationCanceledException) { return; }

            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                NoResultsLabel.IsVisible   = false;
            });

            var results = await LocationService.SearchCitiesAsync(query);

            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;

                bool hasResults = results.Count > 0;
                ResultsList.ItemsSource  = hasResults ? results : null;
                ResultsList.IsVisible    = hasResults;
                NoResultsLabel.IsVisible = !hasResults;
            });
        });
    }

    // ── City selected ─────────────────────────────────────────────────────────

    private async void OnCitySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not LocationInfo info) return;

        LocationService.SaveManualLocation(
            info.Latitude, info.Longitude,
            info.City, info.Country, info.CountryCode);

        PrayerTimesService.ClearDiskCache();    // force re-fetch for new city

        MainPage.PendingCityReload = true;
        await Shell.Current.GoToAsync("..");
    }
}
