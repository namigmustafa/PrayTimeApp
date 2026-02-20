using System.Globalization;
using System.Text.Json;

namespace PrayTimeApp.Services;

public record LocationInfo
{
    // GPS
    public double Latitude       { get; init; }
    public double Longitude      { get; init; }
    public double? AccuracyM    { get; init; }
    public double? AltitudeM    { get; init; }
    public double? SpeedMps     { get; init; }
    public DateTimeOffset FixTime { get; init; }

    // Geocoding
    public string CityLabel     { get; init; } = string.Empty;  // e.g. "BAKU, AZ"
    public string City          { get; init; } = string.Empty;
    public string District      { get; init; } = string.Empty;
    public string State         { get; init; } = string.Empty;
    public string Country       { get; init; } = string.Empty;
    public string CountryCode   { get; init; } = string.Empty;
    public string PostalCode    { get; init; } = string.Empty;
    public string Road          { get; init; } = string.Empty;
    public string DisplayName   { get; init; } = string.Empty;

    public static readonly LocationInfo Empty = new() { CityLabel = "—" };
}

public static class LocationService
{
    private static LocationInfo? _cached;

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    static LocationService()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("PrayTimeApp/1.0");
    }

    public static async Task<LocationInfo> GetLocationInfoAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cached is not null)
            return _cached;

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return LocationInfo.Empty with { CityLabel = "NO PERMISSION" };

            // forceRefresh → OS önbelleğini atla, direkt taze fix iste
            Location? loc;
            if (forceRefresh)
            {
                loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout         = TimeSpan.FromSeconds(15)
                });
            }
            else
            {
                loc = await Geolocation.Default.GetLastKnownLocationAsync()
                   ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest
                      {
                          DesiredAccuracy = GeolocationAccuracy.Medium,
                          Timeout         = TimeSpan.FromSeconds(10)
                      });
            }

            if (loc is null)
                return LocationInfo.Empty with { CityLabel = "LOCATION UNKNOWN" };

            var geo = await ReverseGeocodeAsync(loc.Latitude, loc.Longitude);

            _cached = geo with
            {
                Latitude   = loc.Latitude,
                Longitude  = loc.Longitude,
                AccuracyM  = loc.Accuracy,
                AltitudeM  = loc.Altitude,
                SpeedMps   = loc.Speed,
                FixTime    = loc.Timestamp
            };

            return _cached;
        }
        catch
        {
            return LocationInfo.Empty with { CityLabel = "LOCATION UNKNOWN" };
        }
    }

    // Convenience for pages that only need the header label
    public static async Task<string> GetCityNameAsync(bool forceRefresh = false)
        => (await GetLocationInfoAsync(forceRefresh)).CityLabel;

    public static void ClearCache() => _cached = null;

    // ── Location threshold (km) — configurable from Settings ─────────────────
    public static int ThresholdKm
    {
        get => Preferences.Get("location_threshold_km", 30);
        set => Preferences.Set("location_threshold_km", Math.Clamp(value, 10, 200));
    }

    // ── Last-fetch coordinates (used for distance comparison) ─────────────────
    public static void SaveFetchCoords(double lat, double lon, string city, string country)
    {
        Preferences.Set("fetch_lat",     lat);
        Preferences.Set("fetch_lon",     lon);
        Preferences.Set("fetch_city",    city);
        Preferences.Set("fetch_country", country);
    }

    public static (double Lat, double Lon, string City, string Country)? GetFetchCoords()
    {
        var lat     = Preferences.Get("fetch_lat",     double.NaN);
        var lon     = Preferences.Get("fetch_lon",     double.NaN);
        var city    = Preferences.Get("fetch_city",    "");
        var country = Preferences.Get("fetch_country", "");

        if (double.IsNaN(lat) || string.IsNullOrEmpty(city)) return null;
        return (lat, lon, city, country);
    }

    // ── Haversine distance formula ────────────────────────────────────────────
    public static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ── Manual city override ──────────────────────────────────────────────────

    public static bool IsManualLocation => Preferences.Get("use_manual_city", false);

    public static void SaveManualLocation(double lat, double lon, string city, string country, string countryCode)
    {
        Preferences.Set("use_manual_city",     true);
        Preferences.Set("manual_lat",          lat);
        Preferences.Set("manual_lon",          lon);
        Preferences.Set("manual_city",         city);
        Preferences.Set("manual_country",      country);
        Preferences.Set("manual_country_code", countryCode);
    }

    public static LocationInfo? GetManualLocation()
    {
        if (!IsManualLocation) return null;
        var city    = Preferences.Get("manual_city",         "");
        var country = Preferences.Get("manual_country",      "");
        var cc      = Preferences.Get("manual_country_code", "");
        var lat     = Preferences.Get("manual_lat",          0.0);
        var lon     = Preferences.Get("manual_lon",          0.0);
        if (string.IsNullOrEmpty(city)) return null;
        return new LocationInfo
        {
            City        = city,
            Country     = country,
            CountryCode = cc,
            Latitude    = lat,
            Longitude   = lon,
            CityLabel   = string.IsNullOrEmpty(cc)
                          ? city.ToUpperInvariant()
                          : $"{city.ToUpperInvariant()}, {cc.ToUpperInvariant()}"
        };
    }

    public static void ClearManualLocation()
    {
        Preferences.Remove("use_manual_city");
        Preferences.Remove("manual_lat");
        Preferences.Remove("manual_lon");
        Preferences.Remove("manual_city");
        Preferences.Remove("manual_country");
        Preferences.Remove("manual_country_code");
    }

    // ── Forward geocoding (city name search via Nominatim) ────────────────────

    public static async Task<List<LocationInfo>> SearchCitiesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return new();
        try
        {
            var url = "https://nominatim.openstreetmap.org/search"
                    + $"?q={Uri.EscapeDataString(query)}"
                    + "&format=json&addressdetails=1&limit=7&featuretype=city";

            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<LocationInfo>();

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("address", out var addr)) continue;

                var city = "";
                foreach (var key in new[] { "city", "town", "village", "municipality" })
                    if (addr.TryGetProperty(key, out var v) && !string.IsNullOrEmpty(v.GetString()))
                    { city = v.GetString()!; break; }
                if (string.IsNullOrEmpty(city)) continue;

                var country = addr.TryGetProperty("country",      out var c)   ? c.GetString()   ?? "" : "";
                var cc      = addr.TryGetProperty("country_code", out var cce) ? cce.GetString() ?? "" : "";
                cc = cc.ToUpperInvariant();

                // deduplicate by city+country
                if (!seen.Add($"{city}|{country}")) continue;

                var latStr = el.TryGetProperty("lat", out var la) ? la.GetString() : null;
                var lonStr = el.TryGetProperty("lon", out var lo) ? lo.GetString() : null;
                if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) continue;
                if (!double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) continue;

                results.Add(new LocationInfo
                {
                    City        = city,
                    Country     = country,
                    CountryCode = cc,
                    Latitude    = lat,
                    Longitude   = lon,
                    CityLabel   = string.IsNullOrEmpty(cc)
                                  ? city.ToUpperInvariant()
                                  : $"{city.ToUpperInvariant()}, {cc}"
                });
            }
            return results;
        }
        catch { return new(); }
    }

    // ── Nominatim reverse geocoding ──────────────────────────────────────────
    private static async Task<LocationInfo> ReverseGeocodeAsync(double lat, double lon)
    {
        var latS = lat.ToString(CultureInfo.InvariantCulture);
        var lonS = lon.ToString(CultureInfo.InvariantCulture);
        var url  = $"https://nominatim.openstreetmap.org/reverse?lat={latS}&lon={lonS}&format=json";

        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var displayName = root.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? "" : "";

        if (!root.TryGetProperty("address", out var addr))
            return new LocationInfo { CityLabel = "UNKNOWN", DisplayName = displayName };

        string Get(params string[] keys)
        {
            foreach (var k in keys)
                if (addr.TryGetProperty(k, out var v) && !string.IsNullOrWhiteSpace(v.GetString()))
                    return v.GetString()!;
            return "";
        }

        var city        = Get("city", "town", "village", "municipality");
        var district    = Get("suburb", "neighbourhood", "quarter", "district");
        var state       = Get("state", "province", "region");
        var country     = Get("country");
        var countryCode = Get("country_code").ToUpperInvariant();
        var postal      = Get("postcode");
        var road        = Get("road", "pedestrian", "footway");

        var cityLabel = string.IsNullOrWhiteSpace(city) ? "UNKNOWN"
            : string.IsNullOrWhiteSpace(countryCode) ? city.ToUpperInvariant()
            : $"{city.ToUpperInvariant()}, {countryCode}";

        return new LocationInfo
        {
            CityLabel   = cityLabel,
            City        = city,
            District    = district,
            State       = state,
            Country     = country,
            CountryCode = countryCode,
            PostalCode  = postal,
            Road        = road,
            DisplayName = displayName,
            FixTime     = DateTimeOffset.Now
        };
    }
}
