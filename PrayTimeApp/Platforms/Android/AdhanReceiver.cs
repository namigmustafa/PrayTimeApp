using Android.App;
using Android.Content;
using Android.OS;

namespace Nooria;

[BroadcastReceiver(Exported = false, Enabled = true)]
public class AdhanReceiver : BroadcastReceiver
{
    public const string ActionFire = "com.companyname.praytimeapp.ADHAN_FIRE";
    public const string ActionStop = "com.companyname.praytimeapp.ADHAN_STOP";

    public const string ExtraSoundFile  = "sound_file";
    public const string ExtraPrayerName = "prayer_name";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;

        if (intent?.Action == ActionStop)
        {
            Services.FileLogger.Log("AdhanReceiver: stop requested");
            context.StopService(new Intent(context, typeof(AdhanForegroundService)));
            return;
        }

        if (intent?.Action == ActionFire)
        {
            var soundFile  = intent.GetStringExtra(ExtraSoundFile);
            var prayerName = intent.GetStringExtra(ExtraPrayerName) ?? "Prayer";
            Services.FileLogger.Log($"AdhanReceiver: fire prayer={prayerName} sound={soundFile}");

            var svcIntent = new Intent(context, typeof(AdhanForegroundService));
            svcIntent.PutExtra(AdhanForegroundService.ExtraSoundFile, soundFile);
            svcIntent.PutExtra(AdhanForegroundService.ExtraPrayerName, prayerName);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                context.StartForegroundService(svcIntent);
            else
                context.StartService(svcIntent);
        }
    }
}
