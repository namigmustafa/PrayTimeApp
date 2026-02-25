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
            ["FajrIshaMethod"]        = "Calculation Method",
            ["MuslimWorldLeague"]     = "Presidency of Religious Affairs (Türkiye)",
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

            // Notifications
            ["Notif_Title"]           = "Prayer Time",
            ["Notif_BeforeBody"]      = "{0} in {1} min",
            ["Notif_BeforeBody_H"]    = "{0} in {1}h {2}min",
            ["Notif_OnBody"]          = "{0} prayer started",
            ["Notif_EndsBody"]        = "{0} ends in {1} min",
            ["Notif_EndsBody_H"]      = "{0} ends in {1}h {2}min",
            ["AppliesToAll"]          = "Applies to all prayer times",
            ["CustomizePerPrayer"]    = "Customize per prayer",

            // Settings page UI
            ["Sett_Before"]          = "Before Prayer Time",
            ["Sett_On"]              = "On Prayer Time",
            ["Sett_Ends"]            = "Prayer Time Ends In",
            ["Sett_SoundAdhan"]      = "Sound",
            ["Sett_PlayNow"]         = "Play Sound Now",
            ["Sett_PlayNowDesc"]     = "Plays audio directly — no notification needed",
            ["Sett_TestNotif"]       = "Test Notification",
            ["Sett_TestNotifDesc"]   = "Fires in 10 s — go to home screen, keep app alive",
            ["Sett_ViewLog"]         = "Notification Log",
            ["Sett_ViewLogDesc"]     = "Tap to view debug log",
            ["Sett_SelectSound"]     = "Select Sound",
            ["Sett_PlayWhich"]       = "Play which sound?",
            ["Sett_TestWhich"]       = "Test with which sound?",
            ["Sett_TestAlarmTitle"]  = "Test Alarm Scheduled",
            ["Sett_TestAlarmBody"]   = "Sound: {0}\nFires in 10 seconds.\n\n1. Tap OK\n2. Swipe up to go home\n3. Wait for the notification",
            ["Sett_LogTitle"]        = "Notification Log (Copied!)",
            ["Sett_LogClearedTitle"] = "Log Cleared",
            ["Sett_LogClearedBody"]  = "Log file has been cleared.",
            ["Cancel"]               = "Cancel",
            ["OK"]                   = "OK",
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
            ["FajrIshaMethod"]        = "Hesablama Y\u00f6ntemi",
            ["MuslimWorldLeague"]     = "Diyanet İşleri Başkanlığı",
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

            // Notifications
            ["Notif_Title"]           = "Namaz Vakti",
            ["Notif_BeforeBody"]      = "{0} namazına {1} dakika kaldı",
            ["Notif_BeforeBody_H"]    = "{0} namazına {1} saat {2} dakika kaldı",
            ["Notif_OnBody"]          = "{0} namazı girdi",
            ["Notif_EndsBody"]        = "{0} namazının bitmesine {1} dakika kaldı",
            ["Notif_EndsBody_H"]      = "{0} namazının bitmesine {1} saat {2} dakika kaldı",
            ["AppliesToAll"]          = "Tüm namaz vakitlerine uygulanır",
            ["CustomizePerPrayer"]    = "Her namaz için özelleştir",

            // Settings page UI
            ["Sett_Before"]          = "Vakitten Önce",
            ["Sett_On"]              = "Namaz Vaktinde",
            ["Sett_Ends"]            = "Vaktin Bitmesine",
            ["Sett_SoundAdhan"]      = "Ses",
            ["Sett_PlayNow"]         = "Sesi Şimdi Çal",
            ["Sett_PlayNowDesc"]     = "Ses direkt çalınır — bildirim gerekmez",
            ["Sett_TestNotif"]       = "Test Bildirimi",
            ["Sett_TestNotifDesc"]   = "10 sn sonra ateşlenir — ana ekrana git",
            ["Sett_ViewLog"]         = "Bildirim Günlüğü",
            ["Sett_ViewLogDesc"]     = "Günlüğü görüntülemek için dokun",
            ["Sett_SelectSound"]     = "Ses Seçin",
            ["Sett_PlayWhich"]       = "Hangi ses çalınsın?",
            ["Sett_TestWhich"]       = "Hangi ses ile test edilsin?",
            ["Sett_TestAlarmTitle"]  = "Test Alarmı Kuruldu",
            ["Sett_TestAlarmBody"]   = "Ses: {0}\n10 saniye içinde çalınır.\n\n1. Tamam'a dokun\n2. Ana ekrana git\n3. Bildirimi bekle",
            ["Sett_LogTitle"]        = "Bildirim Günlüğü (Kopyalandı!)",
            ["Sett_LogClearedTitle"] = "Günlük Temizlendi",
            ["Sett_LogClearedBody"]  = "Günlük dosyası temizlendi.",
            ["Cancel"]               = "İptal",
            ["OK"]                   = "Tamam",
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
            ["FajrIshaMethod"]        = "Hesablama \u00dcsulu",
            ["MuslimWorldLeague"]     = "Türkiyə Diyanet İşleri Başkanlığı",
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

            // Notifications
            ["Notif_Title"]           = "Namaz Vaxtı",
            ["Notif_BeforeBody"]      = "{0} namazına {1} dəqiqə qalıb",
            ["Notif_BeforeBody_H"]    = "{0} namazına {1} saat {2} dəqiqə qalıb",
            ["Notif_OnBody"]          = "{0} namazı girdi",
            ["Notif_EndsBody"]        = "{0} namazının çıxmasına {1} dəqiqə qalıb",
            ["Notif_EndsBody_H"]      = "{0} namazının çıxmasına {1} saat {2} dəqiqə qalıb",
            ["AppliesToAll"]          = "Bütün namaz vaxtlarına tətbiq edilir",
            ["CustomizePerPrayer"]    = "Hər namaz üçün fərdiləşdir",

            // Settings page UI
            ["Sett_Before"]          = "Namazdan Əvvəl",
            ["Sett_On"]              = "Namaz Vaxtında",
            ["Sett_Ends"]            = "Vaxtın Bitməsinə",
            ["Sett_SoundAdhan"]      = "Səs",
            ["Sett_PlayNow"]         = "Səsi İndi Çal",
            ["Sett_PlayNowDesc"]     = "Səs birbaşa çalınır — bildiriş lazım deyil",
            ["Sett_TestNotif"]       = "Test Bildirişi",
            ["Sett_TestNotifDesc"]   = "10 san. sonra işə düşür — ana ekrana keç",
            ["Sett_ViewLog"]         = "Bildiriş Jurnalı",
            ["Sett_ViewLogDesc"]     = "Jurnalı görmək üçün toxun",
            ["Sett_SelectSound"]     = "Səs Seçin",
            ["Sett_PlayWhich"]       = "Hansı səs çalınsın?",
            ["Sett_TestWhich"]       = "Hansı səslə test edilsin?",
            ["Sett_TestAlarmTitle"]  = "Test Siqnalı Quruldu",
            ["Sett_TestAlarmBody"]   = "Səs: {0}\n10 saniyə sonra çalınacaq.\n\n1. OK düyməsinə toxun\n2. Ana ekrana keç\n3. Bildirişi gözlə",
            ["Sett_LogTitle"]        = "Bildiriş Jurnalı (Kopyalandı!)",
            ["Sett_LogClearedTitle"] = "Jurnal Təmizləndi",
            ["Sett_LogClearedBody"]  = "Jurnal faylı təmizləndi.",
            ["Cancel"]               = "Ləğv et",
            ["OK"]                   = "Tamam",
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
