using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace Nooria.Platforms.Android;

[BroadcastReceiver(Label = "Nooria Prayer Widget", Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_info")]
public class PrayerWidget : AppWidgetProvider
{
    internal const string PrefsName    = "nooria_widget";
    internal const string KeyCurName   = "cur_name";
    internal const string KeyNextName  = "next_name";
    internal const string KeyNextTime  = "next_time";
    internal const string KeyNextUtcMs = "next_utc_ms";

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null) return;
        foreach (var id in appWidgetIds)
            UpdateWidget(context, appWidgetManager, id);
    }

    public static void UpdateWidget(Context context, AppWidgetManager manager, int widgetId)
    {
        var prefs    = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
        string curName   = prefs?.GetString(KeyCurName,  "—") ?? "—";
        string nextName  = prefs?.GetString(KeyNextName, "—") ?? "—";
        string nextTime  = prefs?.GetString(KeyNextTime, "—") ?? "—";
        long   nextUtcMs = prefs?.GetLong(KeyNextUtcMs, 0)   ?? 0;

        // Calculate countdown from stored UTC timestamp
        string countdown = "—";
        if (nextUtcMs > 0)
        {
            var remaining = DateTimeOffset.FromUnixTimeMilliseconds(nextUtcMs) - DateTimeOffset.UtcNow;
            countdown = remaining.TotalSeconds > 0
                ? remaining.TotalHours >= 1
                    ? $"in {(int)remaining.TotalHours}h {remaining.Minutes}min"
                    : $"in {(int)remaining.TotalMinutes}min"
                : "now";
        }

        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_prayer);
        views.SetTextViewText(Resource.Id.widget_current_prayer, curName);
        views.SetTextViewText(Resource.Id.widget_next_name,      nextName);
        views.SetTextViewText(Resource.Id.widget_next_time,      nextTime);
        views.SetTextViewText(Resource.Id.widget_countdown,      countdown);

        // Tap widget → open app
        var intent  = new Intent(context, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        var pending = PendingIntent.GetActivity(
            context, 0, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        views.SetOnClickPendingIntent(Resource.Id.widget_root, pending);

        manager.UpdateAppWidget(widgetId, views);
    }

    /// <summary>Called from MainPage after prayer times load — writes data and refreshes all widgets.</summary>
    public static void WriteDataAndUpdate(
        Context context,
        string curName, string nextName, string nextTime, long nextUtcMs)
    {
        var editor = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)?.Edit();
        editor?.PutString(KeyCurName,  curName);
        editor?.PutString(KeyNextName, nextName);
        editor?.PutString(KeyNextTime, nextTime);
        editor?.PutLong(KeyNextUtcMs,  nextUtcMs);
        editor?.Apply();

        var manager  = AppWidgetManager.GetInstance(context);
        if (manager is null) return;
        var provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(PrayerWidget)));
        var ids      = manager.GetAppWidgetIds(provider);
        if (ids is null || ids.Length == 0) return;
        foreach (var id in ids)
            UpdateWidget(context, manager, id);
    }
}
