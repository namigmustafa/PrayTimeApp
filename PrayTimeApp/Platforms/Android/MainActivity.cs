using Android.App;
using Android.Content.PM;
using Android.OS;
using Google.Android.Material.BottomNavigation;

namespace PrayTimeApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();
            // Defer until after the view hierarchy is fully laid out
            Window?.DecorView.Post(() => FixBottomNavTextSize(Window.DecorView));
        }

        // Walk the view tree to find BottomNavigationView and lock both
        // active and inactive text appearances to the same fixed size.
        private static void FixBottomNavTextSize(Android.Views.View? view)
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
