using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Google.Android.Material.BottomNavigation;

namespace Nooria
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Match status bar to app background and use light icons on dark bg
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0D1623"));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var flags = (int)Window!.DecorView.SystemUiVisibility;
                flags &= ~(int)Android.Views.SystemUiFlags.LightStatusBar; // white icons
                Window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)flags;
            }

            CreateNotificationChannels();

            // Android 13+: POST_NOTIFICATIONS runtime permission
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                RequestPermissions([Android.Manifest.Permission.PostNotifications], 0);

            // Android 12+: exact alarms must be granted by the user
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var alarmMgr = (AlarmManager)GetSystemService(AlarmService)!;
                if (!alarmMgr.CanScheduleExactAlarms())
                    StartActivity(new Intent(Settings.ActionRequestScheduleExactAlarm));
            }
        }

        void CreateNotificationChannels()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var mgr = (NotificationManager)GetSystemService(Android.Content.Context.NotificationService)!;

            // v2: recreate channels with AudioUsageKind.Alarm so they bypass silent/vibrate mode.
            const string prefKey = "notif_channels_v";
            if (Microsoft.Maui.Storage.Preferences.Get(prefKey, 0) < 2)
            {
                foreach (var id in new[]
                {
                    "prayer_default", "prayer_silent", "prayer_bayati", "prayer_apple",
                    "prayer_early_riser", "prayer_iphone_alarm", "prayer_revelation",
                    "prayer_apple_hard", "prayer_aranan", "prayer_ezan"
                })
                    mgr.DeleteNotificationChannel(id);
                Microsoft.Maui.Storage.Preferences.Set(prefKey, 2);
            }

            // Default sound channel — alarm usage bypasses silent mode
            var defaultAttrs = new Android.Media.AudioAttributes.Builder()
                .SetUsage(Android.Media.AudioUsageKind.Alarm)
                .SetContentType(Android.Media.AudioContentType.Music)
                .Build()!;
            var defaultCh = new NotificationChannel(
                "prayer_default", "Prayer Alarms", NotificationImportance.High)
                { Description = "Prayer time alerts" };
            defaultCh.SetSound(
                Android.Media.RingtoneManager.GetDefaultUri(Android.Media.RingtoneType.Alarm),
                defaultAttrs);
            mgr.CreateNotificationChannel(defaultCh);

            // Silent channel
            var silent = new NotificationChannel(
                "prayer_silent", "Prayer Alarms (Silent)", NotificationImportance.Low)
                { Description = "Silent prayer alerts" };
            silent.SetSound(null, null);
            mgr.CreateNotificationChannel(silent);

            // Per-tone channels
            CreateAdhanChannel(mgr, "prayer_bayati",       "Prayer (Adhan Bayati)",  "adhan_bayati");
            CreateAdhanChannel(mgr, "prayer_apple",        "Prayer (Apple)",          "apple");
            CreateAdhanChannel(mgr, "prayer_early_riser",  "Prayer (Early Riser)",    "early_riser");
            CreateAdhanChannel(mgr, "prayer_iphone_alarm", "Prayer (iPhone Alarm)",   "iphone_alarm_music");
            CreateAdhanChannel(mgr, "prayer_revelation",   "Prayer (Revelation)",     "revelation");
            CreateAdhanChannel(mgr, "prayer_apple_hard",   "Prayer (Apple Hard)",     "apple_android_hard");
            CreateAdhanChannel(mgr, "prayer_aranan",       "Prayer (Aranan Zil)",     "aranan_zil_sesi");
            CreateAdhanChannel(mgr, "prayer_ezan",         "Prayer (Ezan 1)",         "ezan_1");
        }

        void CreateAdhanChannel(NotificationManager mgr, string id, string name, string soundFile)
        {
            var ch = new NotificationChannel(id, name, NotificationImportance.High);
            int resId = Resources!.GetIdentifier(soundFile, "raw", PackageName);
            if (resId != 0)
            {
                var uri   = Android.Net.Uri.Parse($"android.resource://{PackageName}/raw/{soundFile}");
                var attrs = new Android.Media.AudioAttributes.Builder()
                    .SetUsage(Android.Media.AudioUsageKind.Alarm)       // bypasses silent/vibrate mode
                    .SetContentType(Android.Media.AudioContentType.Music)
                    .Build()!;
                ch.SetSound(uri, attrs);
            }
            mgr.CreateNotificationChannel(ch);
        }

        protected override void OnResume()
        {
            base.OnResume();
            Window?.DecorView.Post(() => FixBottomNavTextSize(Window.DecorView));
        }

        static void FixBottomNavTextSize(Android.Views.View? view)
        {
            if (view is BottomNavigationView bnv)
            {
                bnv.ItemTextAppearanceActive   = Resource.Style.AppBottomNavText;
                bnv.ItemTextAppearanceInactive = Resource.Style.AppBottomNavText;
                return;
            }
            if (view is Android.Views.ViewGroup group)
                for (int i = 0; i < group.ChildCount; i++)
                    FixBottomNavTextSize(group.GetChildAt(i));
        }
    }
}
