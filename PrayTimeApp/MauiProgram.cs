using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace PrayTimeApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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
