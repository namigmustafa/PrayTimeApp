using Nooria.Services;

namespace Nooria;

public partial class CalcMethodConfigPage : ContentPage
{
    private string _shafaq   = "general";
    private string _tune     = "0,0,0,0,0,0,0,0,0";
    private int    _school   = 0;
    private int    _midnight = 0;
    private int    _latAdj   = 0;
    private string _calendar = "HJCoSA";

    public CalcMethodConfigPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _shafaq   = PrayerTimesService.CalcShafaq;
        _tune     = PrayerTimesService.CalcTune;
        _school   = PrayerTimesService.CalcSchool;
        _midnight = PrayerTimesService.CalcMidnight;
        _latAdj   = PrayerTimesService.CalcLatAdj;
        _calendar = PrayerTimesService.CalcCalendar;
        RefreshAllLabels();
    }

    private void RefreshAllLabels()
    {
        ShafaqValueLabel.Text = _shafaq switch
        {
            "ahmer" => LocalizationService.GetString("Shafaq_Ahmer"),
            "abyad" => LocalizationService.GetString("Shafaq_Abyad"),
            _       => LocalizationService.GetString("Shafaq_General"),
        };
        TuneValueLabel.Text = _tune;
        SchoolValueLabel.Text = _school switch
        {
            1 => LocalizationService.GetString("School_Hanafi"),
            _ => LocalizationService.GetString("School_Shafi"),
        };
        MidnightValueLabel.Text = _midnight switch
        {
            1 => LocalizationService.GetString("Midnight_Jafari"),
            _ => LocalizationService.GetString("Midnight_Standard"),
        };
        LatAdjValueLabel.Text = _latAdj switch
        {
            1 => LocalizationService.GetString("LatAdj_Middle"),
            2 => LocalizationService.GetString("LatAdj_Seventh"),
            3 => LocalizationService.GetString("LatAdj_Angle"),
            _ => LocalizationService.GetString("LatAdj_None"),
        };
        CalendarValueLabel.Text = _calendar switch
        {
            "UAQ"          => LocalizationService.GetString("Calendar_UAQ"),
            "DIYANET"      => LocalizationService.GetString("Calendar_DIYANET"),
            "MATHEMATICAL" => LocalizationService.GetString("Calendar_MATHEMATICAL"),
            _              => LocalizationService.GetString("Calendar_HJCoSA"),
        };
    }

    private async void OnShafaqTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("CalcConfig_Shafaq"),
            LocalizationService.GetString("Cancel"),
            null,
            LocalizationService.GetString("Shafaq_General"),
            LocalizationService.GetString("Shafaq_Ahmer"),
            LocalizationService.GetString("Shafaq_Abyad"));
        if      (result == LocalizationService.GetString("Shafaq_Ahmer"))  _shafaq = "ahmer";
        else if (result == LocalizationService.GetString("Shafaq_Abyad")) _shafaq = "abyad";
        else if (result == LocalizationService.GetString("Shafaq_General")) _shafaq = "general";
        else return;
        RefreshAllLabels();
    }

    private async void OnTuneTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TuneConfigPage));
    }

    private async void OnSchoolTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("CalcConfig_School"),
            LocalizationService.GetString("Cancel"),
            null,
            LocalizationService.GetString("School_Shafi"),
            LocalizationService.GetString("School_Hanafi"));
        if      (result == LocalizationService.GetString("School_Hanafi")) _school = 1;
        else if (result == LocalizationService.GetString("School_Shafi"))  _school = 0;
        else return;
        RefreshAllLabels();
    }

    private async void OnMidnightTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("CalcConfig_Midnight"),
            LocalizationService.GetString("Cancel"),
            null,
            LocalizationService.GetString("Midnight_Standard"),
            LocalizationService.GetString("Midnight_Jafari"));
        if      (result == LocalizationService.GetString("Midnight_Jafari"))   _midnight = 1;
        else if (result == LocalizationService.GetString("Midnight_Standard")) _midnight = 0;
        else return;
        RefreshAllLabels();
    }

    private async void OnLatAdjTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("CalcConfig_LatAdj"),
            LocalizationService.GetString("Cancel"),
            null,
            LocalizationService.GetString("LatAdj_None"),
            LocalizationService.GetString("LatAdj_Middle"),
            LocalizationService.GetString("LatAdj_Seventh"),
            LocalizationService.GetString("LatAdj_Angle"));
        if      (result == LocalizationService.GetString("LatAdj_Middle"))   _latAdj = 1;
        else if (result == LocalizationService.GetString("LatAdj_Seventh"))  _latAdj = 2;
        else if (result == LocalizationService.GetString("LatAdj_Angle"))    _latAdj = 3;
        else if (result == LocalizationService.GetString("LatAdj_None"))     _latAdj = 0;
        else return;
        RefreshAllLabels();
    }

    private async void OnCalendarTapped(object sender, TappedEventArgs e)
    {
        string result = await DisplayActionSheet(
            LocalizationService.GetString("CalcConfig_Calendar"),
            LocalizationService.GetString("Cancel"),
            null,
            LocalizationService.GetString("Calendar_HJCoSA"),
            LocalizationService.GetString("Calendar_UAQ"),
            LocalizationService.GetString("Calendar_DIYANET"),
            LocalizationService.GetString("Calendar_MATHEMATICAL"));
        if      (result == LocalizationService.GetString("Calendar_UAQ"))          _calendar = "UAQ";
        else if (result == LocalizationService.GetString("Calendar_DIYANET"))      _calendar = "DIYANET";
        else if (result == LocalizationService.GetString("Calendar_MATHEMATICAL")) _calendar = "MATHEMATICAL";
        else if (result == LocalizationService.GetString("Calendar_HJCoSA"))       _calendar = "HJCoSA";
        else return;
        RefreshAllLabels();
    }

    private async void OnApplyTapped(object sender, TappedEventArgs e)
    {
        PrayerTimesService.CalcShafaq   = _shafaq;
        PrayerTimesService.CalcSchool   = _school;
        PrayerTimesService.CalcMidnight = _midnight;
        PrayerTimesService.CalcLatAdj   = _latAdj;
        PrayerTimesService.CalcCalendar = _calendar;
        PrayerTimesService.ClearDiskCache();
        MainPage.PendingCityReload = true;
        await Shell.Current.GoToAsync("..");
    }

    private void OnResetTapped(object sender, TappedEventArgs e)
    {
        var def = PrayerTimesService.CurrentMethodDef;
        _shafaq   = def?.DefaultShafaq   ?? "general";
        _tune     = def?.DefaultTune     ?? "0,0,0,0,0,0,0,0,0";
        _school   = def?.DefaultSchool   ?? 0;
        _midnight = def?.DefaultMidnight ?? 0;
        _latAdj   = def?.DefaultLatAdj   ?? 0;
        _calendar = def?.DefaultCalendar ?? "HJCoSA";
        RefreshAllLabels();
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("..");
}
