using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Nooria.Services;

// ── Models ────────────────────────────────────────────────────────────────────

public class PrayerTimings
{
    public string Fajr       { get; set; } = "—";
    public string Sunrise    { get; set; } = "—";
    public string Dhuhr      { get; set; } = "—";
    public string Asr        { get; set; } = "—";
    public string Sunset     { get; set; } = "—";
    public string Maghrib    { get; set; } = "—";
    public string Isha       { get; set; } = "—";
    public string Imsak      { get; set; } = "—";
    public string Midnight   { get; set; } = "—";
    public string Firstthird { get; set; } = "—";
    public string Lastthird  { get; set; } = "—";
}

public class PrayerDay
{
    public int    Day         { get; set; }
    public string HijriDate   { get; set; } = "";   // "01-07-1446"
    public string HijriMonth  { get; set; } = "";   // "Rajab"
    public string HijriYear   { get; set; } = "";
    public PrayerTimings Timings { get; set; } = new();
}

public class PrayerMonthCache
{
    public int    Year         { get; set; }
    public int    Month        { get; set; }
    public string City         { get; set; } = "";
    public string Country      { get; set; } = "";
    public string TimeZoneId   { get; set; } = "";   // IANA id, e.g. "Asia/Baku"
    public int    CalcMethodId { get; set; } = 0;
    public DateTime FetchedAt  { get; set; }
    public List<PrayerDay> Days { get; set; } = new();
}

public record CalcMethodDefinition(
    int     DisplayId,
    int     ApiMethodId,
    string  NameKey,
    string? DefaultShafaq   = null,
    string? DefaultTune     = null,
    int?    DefaultSchool   = null,
    int?    DefaultMidnight = null,
    int?    DefaultLatAdj   = null,
    string? DefaultCalendar = null
);

// ── Service ───────────────────────────────────────────────────────────────────

public static class PrayerTimesService
{
    // ── All supported calculation methods ─────────────────────────────────────
    public static readonly CalcMethodDefinition[] AllMethods =
    [
        new(0,  0,  "CalcMethod_0"),
        new(1,  1,  "CalcMethod_1"),
        new(2,  2,  "CalcMethod_2"),
        new(3,  3,  "CalcMethod_3"),
        new(4,  4,  "CalcMethod_4"),
        new(5,  5,  "CalcMethod_5"),
        new(7,  7,  "CalcMethod_7"),
        new(8,  8,  "CalcMethod_8"),
        new(9,  9,  "CalcMethod_9"),
        new(10, 10, "CalcMethod_10"),
        new(11, 11, "CalcMethod_11"),
        new(12, 12, "CalcMethod_12"),
        new(13, 13, "CalcMethod_13", DefaultTune: "0,0,0,0,0,0,0,0,0", DefaultSchool: 0, DefaultMidnight: 0, DefaultLatAdj: 3),
        new(14, 14, "CalcMethod_14"),
        new(15, 15, "CalcMethod_15"),
        new(16, 16, "CalcMethod_16"),
        new(17, 17, "CalcMethod_17"),
        new(18, 18, "CalcMethod_18"),
        new(19, 19, "CalcMethod_19"),
        new(20, 20, "CalcMethod_20"),
        new(21, 21, "CalcMethod_21"),
        new(22, 22, "CalcMethod_22"),
        new(23, 23, "CalcMethod_23"),
        new(24, 3,  "CalcMethod_24", DefaultShafaq: "general", DefaultTune: "1,5,-1,0,1,15,0,-10,9", DefaultSchool: 1, DefaultCalendar: "UAQ"),
        new(99, 99, "CalcMethod_99"),
    ];

    public static CalcMethodDefinition? CurrentMethodDef
        => AllMethods.FirstOrDefault(m => m.DisplayId == CalcMethodId);

    // ── Calculation method ID (with migration from old string pref) ───────────
    private const string MethodIdPrefKey = "calc_method_id";

    public static int CalcMethodId
    {
        get => MigrateAndGetMethodId();
        set => Preferences.Set(MethodIdPrefKey, value);
    }

    private static int MigrateAndGetMethodId()
    {
        if (Preferences.ContainsKey(MethodIdPrefKey))
            return Preferences.Get(MethodIdPrefKey, 13);
        var old = Preferences.Get("calc_method", "diyanet");
        int migrated = old == "qmi" ? 24 : 13;
        Preferences.Set(MethodIdPrefKey, migrated);
        Preferences.Remove("calc_method");
        return migrated;
    }

    // ── Individual calculation parameters ─────────────────────────────────────
    public static string CalcShafaq   { get => Preferences.Get("calc_shafaq",   "general");           set => Preferences.Set("calc_shafaq",   value); }
    public static string CalcTune     { get => Preferences.Get("calc_tune",     "0,0,0,0,0,0,0,0,0"); set => Preferences.Set("calc_tune",     value); }
    public static int    CalcSchool   { get => Preferences.Get("calc_school",   0);                    set => Preferences.Set("calc_school",   value); }
    public static int    CalcMidnight { get => Preferences.Get("calc_midnight", 0);                    set => Preferences.Set("calc_midnight", value); }
    public static int    CalcLatAdj   { get => Preferences.Get("calc_lat_adj",  0);                    set => Preferences.Set("calc_lat_adj",  value); }
    public static string CalcCalendar { get => Preferences.Get("calc_calendar", "HJCoSA");             set => Preferences.Set("calc_calendar", value); }

    // Apply the default parameter values for a given method definition.
    public static void ApplyMethodDefaults(CalcMethodDefinition def)
    {
        if (def.DefaultShafaq   is not null) CalcShafaq   = def.DefaultShafaq;
        if (def.DefaultTune     is not null) CalcTune     = def.DefaultTune;
        if (def.DefaultSchool   is not null) CalcSchool   = def.DefaultSchool.Value;
        if (def.DefaultMidnight is not null) CalcMidnight = def.DefaultMidnight.Value;
        if (def.DefaultLatAdj   is not null) CalcLatAdj   = def.DefaultLatAdj.Value;
        if (def.DefaultCalendar is not null) CalcCalendar = def.DefaultCalendar;
    }

    private static string MethodParams
    {
        get
        {
            var def      = CurrentMethodDef;
            int apiMethod = def?.ApiMethodId ?? 13;
            string shafaq   = def?.DefaultShafaq   ?? CalcShafaq;
            string tune     = def?.DefaultTune     ?? CalcTune;
            int    school   = def?.DefaultSchool   ?? CalcSchool;
            int    midnight = def?.DefaultMidnight ?? CalcMidnight;
            int    latAdj   = def?.DefaultLatAdj   ?? CalcLatAdj;
            string calendar = def?.DefaultCalendar ?? CalcCalendar;

            var sb = new System.Text.StringBuilder();
            sb.Append($"method={apiMethod}");
            if (!string.IsNullOrEmpty(shafaq) && shafaq != "general")
                sb.Append($"&shafaq={shafaq}");
            if (!string.IsNullOrEmpty(tune) && tune != "0,0,0,0,0,0,0,0,0")
                sb.Append($"&tune={tune.Replace(",", "%2C")}");
            if (school   != 0) sb.Append($"&school={school}");
            if (midnight != 0) sb.Append($"&midnightMode={midnight}");
            if (latAdj   != 0) sb.Append($"&latitudeAdjustmentMethod={latAdj}");
            if (!string.IsNullOrEmpty(calendar) && calendar != "HJCoSA")
                sb.Append($"&calendarMethod={calendar}");
            return sb.ToString();
        }
    }

    private static PrayerMonthCache? _mem;
    private static PrayerMonthCache? _mem2; // second most-recently-used month

    // Returns a cached month from the two-slot in-memory store (no I/O).
    public static PrayerMonthCache? GetCachedMonth(int year, int month)
    {
        if (_mem?.Year == year && _mem.Month == month) return _mem;
        if (_mem2?.Year == year && _mem2.Month == month) return _mem2;
        return null;
    }

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    // Returns the cached month, fetching from API if needed.
    // lat/lon are used for the API call when provided (more accurate than city name).
    public static async Task<PrayerMonthCache?> GetMonthAsync(
        int year, int month, string city, string country,
        double lat = 0, double lon = 0,
        bool forceRefresh = false)
    {
        if (!forceRefresh
            && _mem?.Year == year
            && _mem.Month == month
            && string.Equals(_mem.City, city, StringComparison.OrdinalIgnoreCase)
            && _mem.CalcMethodId == CalcMethodId)
            return _mem;

        if (!forceRefresh)
        {
            var disk = await LoadDiskAsync(year, month, city, country);
            if (disk is not null) { SetMem(disk); return disk; }
        }

        return await FetchAsync(year, month, city, country, lat, lon);
    }

    // Convenience: get today's entry from the cache (or null).
    public static PrayerDay? GetToday(PrayerMonthCache? cache)
        => cache?.Days.FirstOrDefault(d => d.Day == DateTime.Today.Day);

    // Find which prayer period is currently active (most recently started).
    // Returns (prayerName, startRawTime) or null if no data.
    public static (string Name, string RawTime)? CurrentPrayer(
        PrayerDay? today, PrayerDay? yesterday, string timeZoneId = "")
    {
        if (today is null) return null;

        var now  = GetCityNow(timeZoneId);
        var date = now.Date;

        // Walk in reverse — first prayer whose start time <= now is the current one
        var list = PrayerList(today).ToList();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var (name, raw) = list[i];
            if (!TryParseTime(raw, out var t)) continue;
            if (date + t <= now) return (name, raw);
        }

        // Before today's first prayer — current is yesterday's Isha
        if (yesterday is not null)
        {
            var ishaRaw = yesterday.Timings.Isha;
            if (!string.IsNullOrEmpty(ishaRaw) && ishaRaw != "—")
                return ("Isha", ishaRaw);
        }

        return null;
    }

    // Find which prayer comes next after 'now'.
    // Returns (prayerName, targetDateTimeUtc) or null if no data.
    public static (string Name, DateTime Target)? NextPrayer(
        PrayerDay? today, PrayerDay? tomorrow, string timeZoneId = "")
    {
        if (today is null) return null;

        var now  = GetCityNow(timeZoneId);
        var date = now.Date;

        // Sunrise is included so that Fajr countdown ends at Sunrise, not Dhuhr
        (string Name, string Raw)[] targets =
        [
            ("Fajr",    today.Timings.Fajr),
            ("Sunrise", today.Timings.Sunrise),
            ("Dhuhr",   today.Timings.Dhuhr),
            ("Asr",     today.Timings.Asr),
            ("Maghrib", today.Timings.Maghrib),
            ("Isha",    today.Timings.Isha),
        ];

        foreach (var (name, raw) in targets)
        {
            if (!TryParseTime(raw, out var t)) continue;
            var dt = date + t;
            if (dt > now) return (name, CityLocalToUtc(dt, timeZoneId));
        }

        // All done for today — use tomorrow's Fajr
        if (tomorrow is not null && TryParseTime(tomorrow.Timings.Fajr, out var fajr))
            return ("Fajr", CityLocalToUtc(date.AddDays(1) + fajr, timeZoneId));

        return null;
    }

    // Converts a city-local DateTime (Kind.Unspecified) to UTC.
    private static DateTime CityLocalToUtc(DateTime cityLocal, string timeZoneId)
    {
        if (!string.IsNullOrEmpty(timeZoneId))
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(cityLocal, DateTimeKind.Unspecified), tz);
            }
            catch { }
        }
        // Fallback: treat as device-local and convert to UTC
        return DateTime.SpecifyKind(cityLocal, DateTimeKind.Local).ToUniversalTime();
    }

    // Returns "now" in the prayer city's local timezone (falls back to device time).
    public static DateTime GetCityNow(string timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId)) return DateTime.Now;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch { return DateTime.Now; }
    }

    // ── Fetch + persist ──────────────────────────────────────────────────────

    private static async Task<PrayerMonthCache?> FetchAsync(
        int year, int month, string city, string country,
        double lat = 0, double lon = 0)
    {
        try
        {
            // Prefer coordinates — more accurate and timezone-aware
            string url;
            if (lat != 0 || lon != 0)
            {
                var latS = lat.ToString("F6", CultureInfo.InvariantCulture);
                var lonS = lon.ToString("F6", CultureInfo.InvariantCulture);
                url = $"https://api.aladhan.com/v1/calendar/{year}/{month}" +
                      $"?latitude={latS}&longitude={lonS}&{MethodParams}";
            }
            else
            {
                url = $"https://api.aladhan.com/v1/calendarByCity/{year}/{month}" +
                      $"?city={Uri.EscapeDataString(city)}" +
                      $"&country={Uri.EscapeDataString(country)}&{MethodParams}";
            }

            var raw = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(raw);

            if (!doc.RootElement.TryGetProperty("data", out var dataArr)
                || dataArr.ValueKind != JsonValueKind.Array)
                return null;

            // Extract timezone from first entry's meta
            string timeZoneId = "";
            foreach (var first in dataArr.EnumerateArray())
            {
                if (first.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("timezone", out var tz))
                    timeZoneId = tz.GetString() ?? "";
                break;
            }

            var days = new List<PrayerDay>();

            foreach (var entry in dataArr.EnumerateArray())
            {
                if (!entry.TryGetProperty("timings", out var t)
                    || !entry.TryGetProperty("date",    out var dateEl))
                    continue;

                // Gregorian day number
                var dayNum = 0;
                if (dateEl.TryGetProperty("gregorian", out var greg)
                    && greg.TryGetProperty("day", out var dayEl))
                    int.TryParse(dayEl.GetString(), out dayNum);
                if (dayNum == 0) continue;

                // Hijri
                string hijriDate = "", hijriMonth = "", hijriYear = "";
                if (dateEl.TryGetProperty("hijri", out var hijri))
                {
                    if (hijri.TryGetProperty("date", out var hd)) hijriDate = hd.GetString() ?? "";
                    if (hijri.TryGetProperty("year", out var hy)) hijriYear = hy.GetString() ?? "";
                    if (hijri.TryGetProperty("month", out var hm)
                        && hm.TryGetProperty("en", out var hme))
                        hijriMonth = hme.GetString() ?? "";
                }

                days.Add(new PrayerDay
                {
                    Day        = dayNum,
                    HijriDate  = hijriDate,
                    HijriMonth = hijriMonth,
                    HijriYear  = hijriYear,
                    Timings = new PrayerTimings
                    {
                        Fajr       = Strip(t, "Fajr"),
                        Sunrise    = Strip(t, "Sunrise"),
                        Dhuhr      = Strip(t, "Dhuhr"),
                        Asr        = Strip(t, "Asr"),
                        Sunset     = Strip(t, "Sunset"),
                        Maghrib    = Strip(t, "Maghrib"),
                        Isha       = Strip(t, "Isha"),
                        Imsak      = Strip(t, "Imsak"),
                        Midnight   = Strip(t, "Midnight"),
                        Firstthird = Strip(t, "Firstthird"),
                        Lastthird  = Strip(t, "Lastthird"),
                    }
                });
            }

            if (days.Count == 0) return null;

            var cache = new PrayerMonthCache
            {
                Year         = year,
                Month        = month,
                City         = city,
                Country      = country,
                TimeZoneId   = timeZoneId,
                CalcMethodId = CalcMethodId,
                FetchedAt    = DateTime.UtcNow,
                Days         = days
            };

            await SaveDiskAsync(cache);
            SetMem(cache);
            return cache;
        }
        catch { return null; }
    }

    // ── Disk helpers ─────────────────────────────────────────────────────────

    private static string DiskPath()
        => Path.Combine(FileSystem.AppDataDirectory, "prayer_data.json");

    private static async Task SaveDiskAsync(PrayerMonthCache cache)
    {
        try
        {
            var json = JsonSerializer.Serialize(cache, _opts);
            await File.WriteAllTextAsync(DiskPath(), json);
        }
        catch { }
    }

    private static async Task<PrayerMonthCache?> LoadDiskAsync(int year, int month, string city, string country)
    {
        try
        {
            var path = DiskPath();
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path);
            var cache = JsonSerializer.Deserialize<PrayerMonthCache>(json, _opts);
            if (cache is null) return null;

            // Invalidate if year, month, city, country or method changed
            if (cache.Year  != year  || cache.Month != month
                || !string.Equals(cache.City,    city,    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(cache.Country, country, StringComparison.OrdinalIgnoreCase)
                || cache.CalcMethodId != CalcMethodId)
                return null;

            return cache;
        }
        catch { return null; }
    }

    public static void ClearDiskCache()
    {
        _mem  = null;
        _mem2 = null;
        try { File.Delete(DiskPath()); } catch { }
    }

    static void SetMem(PrayerMonthCache cache)
    {
        if (_mem is not null && (_mem.Year != cache.Year || _mem.Month != cache.Month))
            _mem2 = _mem;
        _mem = cache;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Strips "(UTC)" / "(EET)" suffix — "06:03 (UTC)" → "06:03"
    private static string Strip(JsonElement timings, string key)
    {
        if (!timings.TryGetProperty(key, out var v)) return "—";
        var s = v.GetString() ?? "—";
        var i = s.IndexOf(' ');
        return i > 0 ? s[..i] : s;
    }

    private static bool TryParseTime(string s, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrEmpty(s) || s == "—") return false;
        if (TimeSpan.TryParseExact(s, @"hh\:mm", null, out result)) return true;
        if (TimeSpan.TryParseExact(s, @"h\:mm",  null, out result)) return true;
        return false;
    }

    // Daily prayer periods in order (Sunrise marks the end of Fajr period)
    private static IEnumerable<(string Name, string Raw)> PrayerList(PrayerDay d)
    {
        yield return ("Fajr",    d.Timings.Fajr);
        yield return ("Sunrise", d.Timings.Sunrise);
        yield return ("Dhuhr",   d.Timings.Dhuhr);
        yield return ("Asr",     d.Timings.Asr);
        yield return ("Maghrib", d.Timings.Maghrib);
        yield return ("Isha",    d.Timings.Isha);
    }
}
