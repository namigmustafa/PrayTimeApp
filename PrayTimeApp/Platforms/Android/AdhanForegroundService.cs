using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using Nooria.Services;

namespace Nooria;

// ForegroundServiceType = TypeMediaPlayback (2)
[Service(Exported = false, ForegroundServiceType = (Android.Content.PM.ForegroundService)2)]
public class AdhanForegroundService : Service
{
    public const string ExtraSoundFile  = "sound_file";
    public const string ExtraPrayerName = "prayer_name";

    private const int    NotifId   = 88888;
    private const string ChannelId = "adhan_playing";

    private MediaPlayer? _player;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        FileLogger.Log("AdhanForegroundService: OnStartCommand");

        var soundFile  = intent?.GetStringExtra(ExtraSoundFile);
        var prayerName = intent?.GetStringExtra(ExtraPrayerName) ?? "Prayer";

        EnsureChannel();

        // ── "Stop Adhan" notification action (broadcast → receiver) ──────────
        var stopIntent = new Intent(this, typeof(AdhanReceiver));
        stopIntent.SetAction(AdhanReceiver.ActionStop);
        var piFlags = Build.VERSION.SdkInt >= BuildVersionCodes.M
            ? PendingIntentFlags.Immutable
            : (PendingIntentFlags)0;
        var stopPi = PendingIntent.GetBroadcast(this, 0, stopIntent, piFlags)!;

        // ── Tap notification → open app ───────────────────────────────────────
        var openIntent = PackageManager!.GetLaunchIntentForPackage(PackageName!)!;
        openIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var openPi = PendingIntent.GetActivity(this, 1, openIntent,
            Build.VERSION.SdkInt >= BuildVersionCodes.M ? PendingIntentFlags.Immutable : 0)!;

        var notif = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(prayerName)
            .SetContentText("Adhan is playing")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
            .SetOngoing(true)
            .SetContentIntent(openPi)
            .AddAction(Android.Resource.Drawable.IcMediaPause, "Stop Adhan", stopPi)
            .Build()!;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(NotifId, notif, (Android.Content.PM.ForegroundService)2);
        else
            StartForeground(NotifId, notif);

        PlayAudio(soundFile);
        AdhanPlayerService.SetPlaying(true);

        return StartCommandResult.NotSticky;
    }

    private void PlayAudio(string? soundFile)
    {
        _player?.Release();
        _player = null;

        if (string.IsNullOrEmpty(soundFile))
        {
            FileLogger.Log("AdhanForegroundService: no sound file specified");
            StopSelf();
            return;
        }

        var resId = Resources!.GetIdentifier(soundFile, "raw", PackageName);
        if (resId == 0)
        {
            FileLogger.Log($"AdhanForegroundService: raw/{soundFile} not found");
            StopSelf();
            return;
        }

        // Use AudioUsageKind.Alarm so playback bypasses silent/vibrate mode
        var alarmAttrs = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Alarm)
            .SetContentType(AudioContentType.Music)
            .Build()!;

        _player = new MediaPlayer();
        _player.SetAudioAttributes(alarmAttrs);
        var uri = Android.Net.Uri.Parse($"android.resource://{PackageName}/{resId}");
        _player.SetDataSource(this, uri);
        _player.Prepare();

        if (_player == null)
        {
            FileLogger.Log("AdhanForegroundService: MediaPlayer setup failed");
            StopSelf();
            return;
        }

        // Keep CPU awake so audio doesn't cut when screen turns off
        _player.SetWakeMode(this, Android.OS.WakeLockFlags.Partial);

        _player.Completion += (_, _) =>
        {
            FileLogger.Log("AdhanForegroundService: playback complete");
            StopSelf();
        };

        _player.Start();
        FileLogger.Log($"AdhanForegroundService: playing raw/{soundFile}");
    }

    public override void OnDestroy()
    {
        FileLogger.Log("AdhanForegroundService: OnDestroy");
        _player?.Stop();
        _player?.Release();
        _player = null;
        AdhanPlayerService.SetPlaying(false);
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var mgr = (NotificationManager)GetSystemService(NotificationService)!;
        if (mgr.GetNotificationChannel(ChannelId) != null) return;
        var ch = new NotificationChannel(ChannelId, "Adhan Playing", NotificationImportance.Low);
        ch.SetSound(null, null);
        mgr.CreateNotificationChannel(ch);
    }
}
