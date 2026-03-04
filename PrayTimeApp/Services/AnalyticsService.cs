#if ANDROID || IOS
using Plugin.Firebase.Analytics;
#endif

namespace Nooria.Services;

public static class AnalyticsService
{
    public static void TrackScreen(string screenName)
    {
#if ANDROID || IOS
        try { CrossFirebaseAnalytics.Current.LogEvent("screen_view", ("screen_name", screenName)); }
        catch { /* analytics must never crash the app */ }
#endif
    }

    public static void TrackEvent(string eventName, IDictionary<string, object>? parameters = null)
    {
#if ANDROID || IOS
        try { CrossFirebaseAnalytics.Current.LogEvent(eventName, parameters); }
        catch { /* analytics must never crash the app */ }
#endif
    }
}
