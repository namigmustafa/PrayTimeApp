using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
using Plugin.LocalNotification;
#elif IOS
using Plugin.Firebase.Core.Platforms.iOS;
#endif

namespace Nooria
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
#if ANDROID
                .UseLocalNotification()
#endif
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android => android.OnCreate((activity, _) =>
                        CrossFirebase.Initialize(activity, () => Platform.CurrentActivity!)));
#elif IOS
                    events.AddiOS(iOS => iOS.WillFinishLaunching((_, __) =>
                    {
                        CrossFirebase.Initialize();
                        return false;
                    }));
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if IOS
            EntryHandler.Mapper.AppendToMapping("TransparentEntry", (handler, view) =>
            {
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            });

            // Each ContentPage's native UIView starts white before MAUI applies the
            // XAML background colour — pre-set it dark to prevent a flash.
            PageHandler.Mapper.AppendToMapping("DarkPageBackground", (handler, view) =>
            {
                handler.PlatformView.BackgroundColor = UIKit.UIColor.FromRGB(0x0D, 0x16, 0x23);
            });

            // Each Shell tab is wrapped in a UINavigationController whose bar defaults
            // to white. Even with NavBarIsVisible=False it can flash at the top during
            // transitions. Set the global UINavigationBar appearance to match AppBg.
            var navAppearance = new UIKit.UINavigationBarAppearance();
            navAppearance.ConfigureWithOpaqueBackground();
            navAppearance.BackgroundColor = UIKit.UIColor.FromRGB(0x0D, 0x16, 0x23);
            navAppearance.ShadowColor     = UIKit.UIColor.Clear;
            UIKit.UINavigationBar.Appearance.StandardAppearance   = navAppearance;
            UIKit.UINavigationBar.Appearance.ScrollEdgeAppearance = navAppearance;
            UIKit.UINavigationBar.Appearance.CompactAppearance    = navAppearance;
#endif

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
