using Foundation;
using UserNotifications;

namespace PrayTimeApp
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // Strong reference prevents GC collecting the delegate while
        // UNUserNotificationCenter holds only a weak Objective-C reference.
        static PrayerNotifDelegate? s_notifDelegate;

        public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary launchOptions)
        {
            var result = base.FinishedLaunching(application, launchOptions);
            s_notifDelegate = new PrayerNotifDelegate();
            UNUserNotificationCenter.Current.Delegate = s_notifDelegate;
            return result;
        }

        class PrayerNotifDelegate : UNUserNotificationCenterDelegate
        {
            // Show notification with sound when app is in the foreground
            public override void WillPresentNotification(
                UNUserNotificationCenter center,
                UNNotification notification,
                Action<UNNotificationPresentationOptions> completionHandler)
            {
                completionHandler(UNNotificationPresentationOptions.Banner |
                                  UNNotificationPresentationOptions.Sound);
            }

            public override void DidReceiveNotificationResponse(
                UNUserNotificationCenter center,
                UNNotificationResponse response,
                Action completionHandler)
            {
                completionHandler();
            }
        }
    }
}
