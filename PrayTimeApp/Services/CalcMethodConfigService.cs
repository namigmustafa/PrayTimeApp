using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Storage;

namespace Nooria.Services;

// Her metodun konfigürasyonu
public class CalcMethodConfig
{
    [JsonPropertyName("shafaq")]   public string Shafaq   { get; set; } = "general";
    [JsonPropertyName("tune")]     public string Tune     { get; set; } = "0,0,0,0,0,0,0,0,0";
    [JsonPropertyName("school")]   public int    School   { get; set; } = 0;
    [JsonPropertyName("midnight")] public int    Midnight { get; set; } = 0;
    [JsonPropertyName("latAdj")]   public int    LatAdj   { get; set; } = 0;
    [JsonPropertyName("calendar")] public string Calendar { get; set; } = "HJCoSA";
}

// JSON dosyasının kök yapısı
public class CalcMethodConfigStore
{
    [JsonPropertyName("selectedMethodId")]
    public int SelectedMethodId { get; set; } = 13;

    [JsonPropertyName("methods")]
    public Dictionary<string, CalcMethodConfig> Methods { get; set; } = new();
}

public static class CalcMethodConfigService
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, "calc_method_config.json");

    private static CalcMethodConfigStore? _store;

    // Tüm metodlar için factory default değerleri
    private static readonly Dictionary<int, CalcMethodConfig> _factoryDefaults = new()
    {
        {  0, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  1, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  2, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  3, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  4, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  5, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  7, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 1, LatAdj = 0, Calendar = "HJCoSA" } },
        {  8, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        {  9, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 10, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 11, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 12, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 13, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 3, Calendar = "HJCoSA" } },
        { 14, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 15, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 16, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 17, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 18, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 19, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 20, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 21, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 22, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 23, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
        { 24, new() { Shafaq = "general", Tune = "1,5,-1,0,1,15,0,-10,9", School = 1, Midnight = 0, LatAdj = 0, Calendar = "UAQ" } },
        { 99, new() { Shafaq = "general", Tune = "0,0,0,0,0,0,0,0,0", School = 0, Midnight = 0, LatAdj = 0, Calendar = "HJCoSA" } },
    };

    // Uygulamanın ilk açılışında çağrılır — dosya yoksa oluşturur
    public static void EnsureInitialized()
    {
        if (_store is not null) return;
        Load();
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _store = JsonSerializer.Deserialize<CalcMethodConfigStore>(json, _opts);
            }
        }
        catch { }

        if (_store is null)
        {
            // İlk açılış: migration veya fresh start
            _store = new CalcMethodConfigStore();
            MigrateFromPreferences();
            Save();
        }
    }

    // Eski Preferences değerlerini JSON'a taşı (upgrade eden kullanıcılar için)
    private static void MigrateFromPreferences()
    {
        // Seçili metodu taşı
        if (Preferences.ContainsKey("calc_method_id"))
        {
            _store!.SelectedMethodId = Preferences.Get("calc_method_id", 13);
        }
        else if (Preferences.ContainsKey("calc_method"))
        {
            var old = Preferences.Get("calc_method", "diyanet");
            _store!.SelectedMethodId = old == "qmi" ? 24 : 13;
        }

        // Eğer eski parametreler kaydedilmişse, seçili metoda yaz
        var currentId = _store!.SelectedMethodId;
        var def = GetDefaultConfig(currentId);
        var migrated = new CalcMethodConfig
        {
            Shafaq   = Preferences.ContainsKey("calc_shafaq")   ? Preferences.Get("calc_shafaq",   def.Shafaq)   : def.Shafaq,
            Tune     = Preferences.ContainsKey("calc_tune")     ? Preferences.Get("calc_tune",     def.Tune)     : def.Tune,
            School   = Preferences.ContainsKey("calc_school")   ? Preferences.Get("calc_school",   def.School)   : def.School,
            Midnight = Preferences.ContainsKey("calc_midnight") ? Preferences.Get("calc_midnight", def.Midnight) : def.Midnight,
            LatAdj   = Preferences.ContainsKey("calc_lat_adj")  ? Preferences.Get("calc_lat_adj",  def.LatAdj)   : def.LatAdj,
            Calendar = Preferences.ContainsKey("calc_calendar") ? Preferences.Get("calc_calendar", def.Calendar) : def.Calendar,
        };
        _store.Methods[currentId.ToString()] = migrated;

        // Eski Preferences anahtarlarını temizle
        foreach (var key in new[] { "calc_method_id", "calc_method", "calc_shafaq", "calc_tune",
                                     "calc_school", "calc_midnight", "calc_lat_adj", "calc_calendar" })
            Preferences.Remove(key);
    }

    private static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_store, _opts);
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }

    // Seçili metod ID
    public static int SelectedMethodId
    {
        get { EnsureInitialized(); return _store!.SelectedMethodId; }
        set { EnsureInitialized(); _store!.SelectedMethodId = value; Save(); }
    }

    // Belirtilen metodun konfigürasyonunu döndür (yoksa factory default)
    public static CalcMethodConfig GetConfig(int methodId)
    {
        EnsureInitialized();
        var key = methodId.ToString();
        if (_store!.Methods.TryGetValue(key, out var cfg))
            return cfg;
        // Henüz kaydedilmemiş → factory default döndür (kaydetme)
        return GetDefaultConfig(methodId);
    }

    // Factory default (değiştirilemez)
    public static CalcMethodConfig GetDefaultConfig(int methodId)
    {
        if (_factoryDefaults.TryGetValue(methodId, out var def))
            return new CalcMethodConfig
            {
                Shafaq   = def.Shafaq,
                Tune     = def.Tune,
                School   = def.School,
                Midnight = def.Midnight,
                LatAdj   = def.LatAdj,
                Calendar = def.Calendar,
            };
        return new CalcMethodConfig();
    }

    // Bir metodun konfigürasyonunu kaydet
    public static void SaveConfig(int methodId, CalcMethodConfig config)
    {
        EnsureInitialized();
        _store!.Methods[methodId.ToString()] = config;
        Save();
    }

    // Seçili metodun anlık konfigürasyonu (MethodParams için)
    public static CalcMethodConfig CurrentConfig => GetConfig(SelectedMethodId);
}
