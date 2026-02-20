using System.Globalization;
using System.Text.Json;

namespace PrayTimeApp.Services;

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
    public int    Year       { get; set; }
    public int    Month      { get; set; }
    public string City       { get; set; } = "";
    public string Country    { get; set; } = "";
    public string TimeZoneId { get; set; } = "";   // IANA id, e.g. "Asia/Baku"
    public DateTime FetchedAt { get; set; }
    public List<PrayerDay> Days { get; set; } = new();
}

// ── Service ───────────────────────────────────────────────────────────────────

public static class PrayerTimesService
{
    private static PrayerMonthCache? _mem;

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
            && string.Equals(_mem.City, city, StringComparison.OrdinalIgnoreCase))
            return _mem;

        if (!forceRefresh)
        {
            var disk = await LoadDiskAsync(year, month, city, country);
            if (disk is not null) { _mem = disk; return disk; }
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
    // Returns (prayerName, targetDateTime) or null if no data.
    public static (string Name, DateTime Target)? NextPrayer(
        PrayerDay? today, PrayerDay? tomorrow, string timeZoneId = "")
    {
        if (today is null) return null;

        var now  = GetCityNow(timeZoneId);
        var date = now.Date;

        foreach (var (name, raw) in PrayerList(today))
        {
            if (!TryParseTime(raw, out var t)) continue;
            var dt = date + t;
            if (dt > now) return (name, dt);
        }

        // All done for today — use tomorrow's Fajr
        if (tomorrow is not null && TryParseTime(tomorrow.Timings.Fajr, out var fajr))
            return ("Fajr", date.AddDays(1) + fajr);

        return null;
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
                      $"?latitude={latS}&longitude={lonS}" +
                      $"&method=13&shafaq=general&tune=13&calendarMethod=DIYANET";
            }
            else
            {
                url = $"https://api.aladhan.com/v1/calendarByCity/{year}/{month}" +
                      $"?city={Uri.EscapeDataString(city)}" +
                      $"&country={Uri.EscapeDataString(country)}" +
                      $"&method=13&shafaq=general&tune=13&calendarMethod=DIYANET";
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
                Year       = year,
                Month      = month,
                City       = city,
                Country    = country,
                TimeZoneId = timeZoneId,
                FetchedAt  = DateTime.UtcNow,
                Days       = days
            };

            await SaveDiskAsync(cache);
            _mem = cache;
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

            // Invalidate if year, month, city or country changed
            if (cache.Year  != year  || cache.Month != month
                || !string.Equals(cache.City,    city,    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(cache.Country, country, StringComparison.OrdinalIgnoreCase))
                return null;

            return cache;
        }
        catch { return null; }
    }

    public static void ClearDiskCache()
    {
        _mem = null;
        try { File.Delete(DiskPath()); } catch { }
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

    // The 5 canonical daily prayers in order
    private static IEnumerable<(string Name, string Raw)> PrayerList(PrayerDay d)
    {
        yield return ("Fajr",    d.Timings.Fajr);
        yield return ("Dhuhr",   d.Timings.Dhuhr);
        yield return ("Asr",     d.Timings.Asr);
        yield return ("Maghrib", d.Timings.Maghrib);
        yield return ("Isha",    d.Timings.Isha);
    }
}
