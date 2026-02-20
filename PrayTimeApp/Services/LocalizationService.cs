using Microsoft.Maui.Storage;

namespace PrayTimeApp.Services;

public static class LocalizationService
{
    private static string _currentLanguage = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new()
        {
            // Tabs
            ["Tab_Home"]              = "HOME",
            ["Tab_Monthly"]           = "MONTHLY",
            ["Tab_Qibla"]             = "QIBLA",
            ["Tab_Quran"]             = "QURAN",
            ["Tab_Settings"]          = "SETTINGS",

            // Main Page
            ["Greeting"]              = "Assalamu Alaikum",
            ["FastDay"]               = "Fast Day",
            ["UpcomingPrayer"]        = "UPCOMING PRAYER",
            ["Time"]                  = "TIME",
            ["StartsIn"]              = "STARTS IN",
            ["Hrs"]                   = "HOURS",
            ["Min"]                   = "MINUTE",
            ["Sec"]                   = "SECOND",
            ["Local"]                 = "LOCAL",
            //["CurrentPrayer"]         = "CURRENT PRAYER",
            ["NextIn"]                = "PRAYER TIME ENDS IN",
            ["DailyTimes"]            = "Daily Times",
            ["MonthlyTable"]          = "Monthly Table",

            // Prayer names
            ["Prayer_Fajr"]           = "Fajr",
            ["Prayer_Sunrise"]        = "Sunrise",
            ["Prayer_Dhuhr"]          = "Dhuhr",
            ["Prayer_Asr"]            = "Asr",
            ["Prayer_Maghrib"]        = "Maghrib",
            ["Prayer_Isha"]           = "Isha",

            // Monthly Page
            ["MonthlySchedule"]       = "Monthly Praying Times",
            ["Date"]                  = "Day",
            ["BenefitOfMonth"]        = "BENEFIT OF THE MONTH",
            ["BenefitQuote"]          = "\u201cTake advantage of five before five: your youth before your old age, your health before your sickness...\u201d",
            ["BenefitAttribution"]    = "\u2014 Hadith, Al-Hakim",

            // Qibla / Quran
            ["Qibla"]                 = "Qibla",
            ["Quran"]                 = "Quran",
            ["ComingSoon"]            = "Coming soon",

            // Settings
            ["Settings"]              = "Settings",
            ["AppLanguage"]           = "App Language",
            ["SelectLanguage"]        = "Select your language",
            ["Change"]                = "CHANGE",
            ["LocationThreshold"]     = "Location Update Threshold",
            ["LocationThresholdDesc"] = "Re-fetch prayer times when city changes after moving this distance",
            ["NotificationsAndPrefs"] = "NOTIFICATIONS & PREFERENCES",
            ["PrayerNotifications"]   = "Prayer Notifications",
            ["DarkMode"]              = "Dark Mode",
            ["LocationServices"]      = "Location Services",
            ["CalculationMethod"]     = "CALCULATION METHOD",
            ["FajrIshaMethod"]        = "Fajr & Isha Method",
            ["MuslimWorldLeague"]     = "MUSLIM WORLD LEAGUE",
            ["Account"]               = "ACCOUNT",
            ["ProfileInformation"]    = "Profile Information",
            ["UpdatePersonalDetails"] = "Update your personal details",

            // City Search
            ["SelectCity"]            = "Select City",
            ["SearchCityHint"]        = "Type a city name…",
            ["UseCurrentLocation"]    = "Use Current Location",
            ["NoResults"]             = "No results found",

            // Qibla
            ["QiblaInstruction"]      = "Hold phone flat and rotate until the arrow points up",
        },

        ["tr"] = new()
        {
            // Tabs
            ["Tab_Home"]              = "ANA SAYFA",
            ["Tab_Monthly"]           = "VAKİTLER",
            ["Tab_Qibla"]             = "K\u0130BLE",
            ["Tab_Quran"]             = "KURAN",
            ["Tab_Settings"]          = "AYARLAR",

            // Main Page
            ["Greeting"]              = "Esselamu Aleykum",
            ["FastDay"]               = "Oru\u00e7 G\u00fcn\u00fc",
            ["UpcomingPrayer"]        = "YAKLA\u015eAN NAMAZ",
            ["Time"]                  = "VAK\u0130T",
            ["StartsIn"]              = "BA\u015eLANGICI",
            ["Hrs"]                   = "SAAT",
            ["Min"]                   = "DAKİKA",
            ["Sec"]                   = "SANİYE",
            ["Local"]                 = "YEREL",
            //["CurrentPrayer"]         = "MEVCUT NAMAZ",
            ["NextIn"]                = "VAKTİN ÇIKMASINA",
            ["DailyTimes"]            = "G\u00fcnl\u00fck Vakitler",
            ["MonthlyTable"]          = "Ayl\u0131k Tablo",

            // Prayer names
            ["Prayer_Fajr"]           = "Sabah",
            ["Prayer_Sunrise"]        = "G\u00fcne\u015f",
            ["Prayer_Dhuhr"]          = "\u00d6\u011fle",
            ["Prayer_Asr"]            = "\u0130kindi",
            ["Prayer_Maghrib"]        = "Ak\u015fam",
            ["Prayer_Isha"]           = "Yats\u0131",

            // Monthly Page
            ["MonthlySchedule"]       = "Ayl\u0131k Namaz Vakitleri",
            ["Date"]                  = "Gün",
            ["BenefitOfMonth"]        = "AYIN FAYDALARI",
            ["BenefitQuote"]          = "\u201cBe\u015ften \u00f6nce be\u015ften yararlan: ya\u015fl\u0131l\u0131\u011f\u0131ndan \u00f6nce gen\u00e7li\u011fin, hastal\u0131\u011f\u0131ndan \u00f6nce sa\u011fl\u0131\u011f\u0131n...\u201d",
            ["BenefitAttribution"]    = "\u2014 Hadis, El-Hakim",

            // Qibla / Quran
            ["Qibla"]                 = "K\u0131ble",
            ["Quran"]                 = "Kuran",
            ["ComingSoon"]            = "\u00c7ok yak\u0131nda",

            // Settings
            ["Settings"]              = "Ayarlar",
            ["AppLanguage"]           = "Uygulama Dili",
            ["SelectLanguage"]        = "Dil seçin",
            ["Change"]                = "DE\u011e\u0130\u015eT\u0130R",
            ["LocationThreshold"]     = "Konum G\u00fcncelleme E\u015fi\u011fi",
            ["LocationThresholdDesc"] = "Bu mesafeyi a\u015f\u0131p \u015fehir de\u011fi\u015firse namaz vakitleri yenilenir",
            ["NotificationsAndPrefs"] = "B\u0130LD\u0130R\u0130MLER VE TERC\u0130HLER",
            ["PrayerNotifications"]   = "Namaz Bildirimleri",
            ["DarkMode"]              = "Karanl\u0131k Mod",
            ["LocationServices"]      = "Konum Hizmetleri",
            ["CalculationMethod"]     = "HESAPLAMA Y\u00d6NTEM\u0130",
            ["FajrIshaMethod"]        = "Sabah ve Yats\u0131 Y\u00f6ntemi",
            ["MuslimWorldLeague"]     = "M\u00dcSL\u00dcMAN D\u00dcNYA L\u0130G\u0130",
            ["Account"]               = "HESAP",
            ["ProfileInformation"]    = "Profil Bilgileri",
            ["UpdatePersonalDetails"] = "Ki\u015fisel bilgilerinizi g\u00fcncelleyin",

            // City Search
            ["SelectCity"]            = "\u015eehir Se\u00e7",
            ["SearchCityHint"]        = "\u015eehir ad\u0131 yaz\u0131n\u2026",
            ["UseCurrentLocation"]    = "Mevcut Konumu Kullan",
            ["NoResults"]             = "Sonu\u00e7 bulunamad\u0131",

            // Qibla
            ["QiblaInstruction"]      = "Telefonu düz tutun ve ok yukarı bakana kadar döndürün",
        },

        ["az"] = new()
        {
            // Tabs
            ["Tab_Home"]              = "ANA S\u018fH\u0130F\u018f",
            ["Tab_Monthly"]           = "VAXTLAR",
            ["Tab_Qibla"]             = "Q\u0130BL\u018f",
            ["Tab_Quran"]             = "QURAN",
            ["Tab_Settings"]          = "AYARLAR",

            // Main Page
            ["Greeting"]              = "\u018fssalamu \u018fleykum",
            ["FastDay"]               = "Oruc G\u00fcn\u00fc",
            ["UpcomingPrayer"]        = "YAXINLA\u015eAN NAMAZ",
            ["Time"]                  = "VAXT",
            ["StartsIn"]              = "BA\u015eLAYIR",
            ["Hrs"]                   = "SAAT",
            ["Min"]                   = "DƏQİQƏ",
            ["Sec"]                   = "SANİYƏ",
            ["Local"]                 = "YERLI",
            //["CurrentPrayer"]         = "CAR\u0130 NAMAZ",
            ["NextIn"]                = "VAXTIN ÇIXMASINA",
            ["DailyTimes"]            = "G\u00fcnl\u00fck Vaxtlar",
            ["MonthlyTable"]          = "Ayl\u0131q C\u0259dv\u0259l",

            // Prayer names
            ["Prayer_Fajr"]           = "Sabah",
            ["Prayer_Sunrise"]        = "G\u00fcn\u0259\u015f",
            ["Prayer_Dhuhr"]          = "Günorta",
            ["Prayer_Asr"]            = "\u0130kindi",
            ["Prayer_Maghrib"]        = "Ax\u015fam",
            ["Prayer_Isha"]           = "Yats\u0131",

            // Monthly Page
            ["MonthlySchedule"]       = "Ayl\u0131q Namaz Vaxtları",
            ["Date"]                  = "Gün",
            ["BenefitOfMonth"]        = "AYIN FAYDASI",
            ["BenefitQuote"]          = "\u201cBe\u015fd\u0259n \u0259vv\u0259l be\u015fd\u0259n istifad\u0259 et: qocal\u0131\u011f\u0131ndan \u0259vv\u0259l g\u0259ncliyin, x\u0259st\u0259liyind\u0259n \u0259vv\u0259l sa\u011flaml\u0131\u011f\u0131n...\u201d",
            ["BenefitAttribution"]    = "\u2014 H\u0259dis, \u018fl-Hakim",

            // Qibla / Quran
            ["Qibla"]                 = "Qibl\u0259",
            ["Quran"]                 = "Quran",
            ["ComingSoon"]            = "Tezlikl\u0259",

            // Settings
            ["Settings"]              = "T\u0259nziml\u0259m\u0259l\u0259r",
            ["AppLanguage"]           = "T\u0259tbiq Dili",
            ["SelectLanguage"]        = "Dil seçin",
            ["Change"]                = "D\u018fY\u0130\u015eD\u0130R",
            ["LocationThreshold"]     = "Konum Yenilm\u0259 H\u0259ddi",
            ["LocationThresholdDesc"] = "Bu m\u0259saf\u0259d\u0259n sonra \u015f\u0259h\u0259r d\u0259yi\u015fs\u0259 namaz vaxtlar\u0131 yenid\u0259n al\u0131n\u0131r",
            ["NotificationsAndPrefs"] = "B\u0130LD\u0130R\u0130\u015eL\u018fR V\u018f T\u018fRC\u0130HL\u018fR",
            ["PrayerNotifications"]   = "Namaz Bildiri\u015fl\u0259ri",
            ["DarkMode"]              = "Qaranl\u0131q Rejim",
            ["LocationServices"]      = "M\u0259kan Xidm\u0259tl\u0259ri",
            ["CalculationMethod"]     = "HESABLAMA \u00dcSULU",
            ["FajrIshaMethod"]        = "S\u00fcbh v\u0259 \u0130\u015fa \u00dcsulu",
            ["MuslimWorldLeague"]     = "M\u00dcS\u018fLMAN D\u00dcNYA L\u0130QASI",
            ["Account"]               = "HESAB",
            ["ProfileInformation"]    = "Profil M\u0259lumat\u0131",
            ["UpdatePersonalDetails"] = "\u015e\u0259xsi m\u0259lumatlar\u0131n\u0131z\u0131 yeniley\u0259n",

            // City Search
            ["SelectCity"]            = "\u015e\u0259h\u0259r Se\u00e7",
            ["SearchCityHint"]        = "\u015e\u0259h\u0259r ad\u0131 yaz\u0131n\u2026",
            ["UseCurrentLocation"]    = "M\u0259vcut Yeri \u0130stifad\u0259 Et",
            ["NoResults"]             = "N\u0259tic\u0259 tapilmad\u0131",

            // Qibla
            ["QiblaInstruction"]      = "Telefonu düz saxlayın və ox yuxarı baxana qədər döndərin",
        }
    };

    public static string CurrentLanguage => _currentLanguage;

    public static string CurrentLanguageDisplay => _currentLanguage switch
    {
        "tr" => "Türkçe",
        "az" => "Azərbaycan",
        _    => "English"
    };

    public static void LoadSavedLanguage()
    {
        _currentLanguage = Preferences.Get("AppLanguage", "en");
    }

    public static void SetLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        Preferences.Set("AppLanguage", languageCode);
    }

    public static string GetString(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var dict) &&
            dict.TryGetValue(key, out var value))
            return value;

        // Fallback to English
        if (_translations["en"].TryGetValue(key, out var fallback))
            return fallback;

        return key;
    }
}
