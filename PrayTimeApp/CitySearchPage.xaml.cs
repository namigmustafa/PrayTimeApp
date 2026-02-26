using Nooria.Services;

namespace Nooria;

public partial class CitySearchPage : ContentPage
{
    private CancellationTokenSource? _debounce;

    public CitySearchPage()
    {
        InitializeComponent();
    }

    // ── Back ──────────────────────────────────────────────────────────────────

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");

    // ── Use current location (clear manual override) ──────────────────────────

    private async void OnUseCurrentLocationTapped(object sender, TappedEventArgs e)
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
            ResultsList.ItemsSource  = null;
            ResultsList.IsVisible    = false;
            NoResultsLabel.IsVisible = false;
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
                ResultsList.ItemsSource = hasResults ? results : null;
                ResultsList.IsVisible   = hasResults;
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
