using Nooria.Services;

namespace Nooria;

public partial class CalcMethodPickerPage : ContentPage
{
    private static readonly Dictionary<int, string> MethodDescriptions = new()
    {
        { 0,  "Shia Muslims globally (uses 16° Fajr / 4° Maghrib)." },
        { 1,  "Users in Pakistan, India, Bangladesh, and Afghanistan." },
        { 2,  "Users in the United States and Canada." },
        { 3,  "Users in Europe, Australia, and parts of East Asia." },
        { 4,  "Users in Saudi Arabia." },
        { 5,  "Users in Egypt, Sudan, Syria, Lebanon, and Jordan." },
        { 7,  "Users in Iran (Shia standard)." },
        { 8,  "Users in the UAE, Oman, and Bahrain." },
        { 9,  "Users specifically in Kuwait." },
        { 10, "Users specifically in Qatar." },
        { 11, "Users specifically in Singapore." },
        { 12, "Users in France (uses 12° angle for high latitudes)." },
        { 13, "Users in Turkey." },
        { 14, "Users in Russia and CIS countries." },
        { 15, "Users who follow Moonsighting.com (global usage)." },
        { 16, "Users specifically in Dubai, UAE." },
        { 17, "Users in Malaysia." },
        { 18, "Users in Tunisia." },
        { 19, "Users in Algeria." },
        { 20, "Users in Indonesia." },
        { 21, "Users in Morocco." },
        { 22, "Users in Portugal." },
        { 23, "Users in Jordan (Ministry of Awqaf)." },
        { 24, "Users in Azerbaijan. Note: These timings are calculated mathematically and are not directly sourced from the Caucasian Muslims Office; a variance of ±2 minutes may occur." },
        { 99, "For advanced users who need to set custom calculation angles." },
    };

    public CalcMethodPickerPage()
    {
        InitializeComponent();
        BuildList();
    }

    private void BuildList()
    {
        int currentId = PrayerTimesService.CalcMethodId;

        foreach (var def in PrayerTimesService.AllMethods)
        {
            bool isSelected = def.DisplayId == currentId;
            string name = LocalizationService.GetString(def.NameKey);
            MethodDescriptions.TryGetValue(def.DisplayId, out string? desc);

            var nameLabel = new Label
            {
                Text = name,
                TextColor = isSelected ? Color.FromArgb("#C8973A") : Colors.White,
                FontSize = 15,
                FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
            };

            var descLabel = new Label
            {
                Text = desc ?? "",
                TextColor = Color.FromArgb("#999999"),
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0),
            };

            var checkLabel = new Label
            {
                Text = "✓",
                TextColor = Color.FromArgb("#C8973A"),
                FontSize = 18,
                IsVisible = isSelected,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
            };

            var textStack = new VerticalStackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
            };
            textStack.Children.Add(nameLabel);
            textStack.Children.Add(descLabel);

            var row = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                ],
                Padding = new Thickness(0, 14),
            };
            row.Children.Add(textStack);
            Grid.SetColumn(textStack, 0);
            row.Children.Add(checkLabel);
            Grid.SetColumn(checkLabel, 1);

            // Separator
            var separator = new BoxView
            {
                HeightRequest = 0.5,
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                Margin = new Thickness(0),
            };

            var capturedDef = def;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => await OnMethodSelected(capturedDef);
            row.GestureRecognizers.Add(tap);

            MethodListLayout.Children.Add(row);
            MethodListLayout.Children.Add(separator);
        }
    }

    private async Task OnMethodSelected(CalcMethodDefinition def)
    {
        if (def.DisplayId == PrayerTimesService.CalcMethodId)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }
        PrayerTimesService.CalcMethodId = def.DisplayId;
        PrayerTimesService.ApplyMethodDefaults(def);
        PrayerTimesService.ClearDiskCache();
        MainPage.PendingCityReload = true;
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");
}
