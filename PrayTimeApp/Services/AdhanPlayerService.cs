namespace PrayTimeApp.Services;

public static class AdhanPlayerService
{
    // ── Shared playback state ─────────────────────────────────────────────────

    public static bool IsPlaying { get; private set; }
    public static event Action<bool>? PlayingChanged;

    internal static void SetPlaying(bool value)
    {
        if (IsPlaying == value) return;
        IsPlaying = value;
        MainThread.BeginInvokeOnMainThread(() => PlayingChanged?.Invoke(value));
    }

    // ── Stop (called from app UI or notification action) ─────────────────────

    public static void StopNow()
    {
#if ANDROID
        var ctx = Android.App.Application.Context;
        ctx.StopService(new Android.Content.Intent(ctx, typeof(AdhanForegroundService)));
#endif
        // iOS: sounds are <30 s notification sounds — nothing to stop
    }

#if ANDROID
    // ── AlarmManager scheduling ───────────────────────────────────────────────

    // Use a high base offset so IDs never clash with Plugin.LocalNotification IDs
    const int IdOffset = 50000;

    public static void ScheduleAdhan(int baseId, DateTime at, string prayerName, string soundFile)
    {
        if (string.IsNullOrEmpty(soundFile) || at <= DateTime.Now) return;

        var ctx = Android.App.Application.Context;
        var mgr = (Android.App.AlarmManager)ctx.GetSystemService(Android.Content.Context.AlarmService)!;

        var intent = new Android.Content.Intent(ctx, typeof(AdhanReceiver));
        intent.SetAction(AdhanReceiver.ActionFire);
        intent.PutExtra(AdhanReceiver.ExtraSoundFile, soundFile);
        intent.PutExtra(AdhanReceiver.ExtraPrayerName, prayerName);

        var flags = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M
            ? Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable
            : Android.App.PendingIntentFlags.UpdateCurrent;

        var pi = Android.App.PendingIntent.GetBroadcast(ctx, IdOffset + baseId, intent, flags)!;
        var ms = new DateTimeOffset(at).ToUnixTimeMilliseconds();

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            mgr.SetExactAndAllowWhileIdle(Android.App.AlarmType.RtcWakeup, ms, pi);
        else
            mgr.SetExact(Android.App.AlarmType.RtcWakeup, ms, pi);

        FileLogger.Log($"AdhanSchedule id={IdOffset + baseId} at={at:HH:mm:ss} prayer={prayerName} sound={soundFile}");
    }

    public static void CancelAll()
    {
        var ctx = Android.App.Application.Context;
        var mgr = (Android.App.AlarmManager)ctx.GetSystemService(Android.Content.Context.AlarmService)!;

        var flags = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M
            ? Android.App.PendingIntentFlags.NoCreate | Android.App.PendingIntentFlags.Immutable
            : Android.App.PendingIntentFlags.NoCreate;

        // 7 days × 5 prayers — SlotOn offset is +2, matching ScheduleAdhan calls
        for (int day = 0; day < 7; day++)
        {
            for (int p = 0; p < 5; p++)
            {
                int id     = IdOffset + day * 100 + p * 10 + 2; // +2 = SlotOn
                var intent = new Android.Content.Intent(ctx, typeof(AdhanReceiver));
                intent.SetAction(AdhanReceiver.ActionFire);
                var pi = Android.App.PendingIntent.GetBroadcast(ctx, id, intent, flags);
                if (pi != null)
                    mgr.Cancel(pi);
            }
        }
        FileLogger.Log("AdhanSchedule: cancelled all alarms");
    }
#endif
}
