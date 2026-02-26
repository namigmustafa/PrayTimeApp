#if ANDROID
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
#endif

#if IOS
using UserNotifications;
using Foundation;
#endif

namespace PrayTimeApp.Services;

public static class NotificationService
{
    // ── Android channel IDs ────────────────────────────────────────────────────
    internal const string ChannelDefault    = "prayer_default";
    internal const string ChannelSilent     = "prayer_silent";
    internal const string ChannelBayati     = "prayer_bayati";
    internal const string ChannelApple      = "prayer_apple";
    internal const string ChannelEarlyRiser = "prayer_early_riser";
    internal const string ChannelIphone     = "prayer_iphone_alarm";
    internal const string ChannelRevelation = "prayer_revelation";
    internal const string ChannelAppleHard  = "prayer_apple_hard";
    internal const string ChannelAranan     = "prayer_aranan";
    internal const string ChannelEzan       = "prayer_ezan";

    const int SlotBefore = 1;
    const int SlotOn     = 2;
    const int SlotEnds   = 3;

    static readonly string[] Tones =
        ["Default", "Adhan Bayati", "Apple", "Early Riser", "iPhone Alarm",
         "Revelation", "Apple Hard", "Silent"];

    // ── Permission ────────────────────────────────────────────────────────────

    public static async Task RequestPermissionAsync()
    {
#if IOS
        await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert |
            UNAuthorizationOptions.Sound |
            UNAuthorizationOptions.Badge);
#elif ANDROID
        await LocalNotificationCenter.Current.RequestNotificationPermission();
#else
        await Task.CompletedTask;
#endif
    }

    // ── Play sound immediately (no notification) — diagnostic only ───────────

#if IOS
    static AVFoundation.AVAudioPlayer? _player;

    public static bool IsSoundPlaying => _player?.Playing == true;

    public static void StopSoundNow()
    {
        _player?.Stop();
        _player = null;
    }

    public static void PlaySoundNow(string tone)
    {
        StopSoundNow();

        var fileName = ToneToSoundFileIos(tone);
        if (fileName == null)
        {
            FileLogger.Log("PlaySoundNow: no file for this tone");
            return;
        }

        var name = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var ext  = System.IO.Path.GetExtension(fileName).TrimStart('.');
        var path = Foundation.NSBundle.MainBundle.PathForResource(name, ext);
        FileLogger.Log($"PlaySoundNow: file={fileName} path={path ?? "NOT FOUND"}");

        if (path != null)
        {
            _player = AVFoundation.AVAudioPlayer.FromUrl(
                Foundation.NSUrl.FromFilename(path), out var err);
            if (_player != null) { _player.PrepareToPlay(); _player.Play(); }
            else FileLogger.Log($"PlaySoundNow: error={err?.LocalizedDescription}");
        }
    }
#elif ANDROID
    static Android.Media.MediaPlayer? _player;

    public static bool IsSoundPlaying => _player?.IsPlaying == true;

    public static void StopSoundNow()
    {
        if (_player == null) return;
        if (_player.IsPlaying) _player.Stop();
        _player.Release();
        _player = null;
    }

    public static void PlaySoundNow(string tone)
    {
        StopSoundNow();
        var fileName = ToneToSoundFile(tone);
        if (fileName == null)
        {
            FileLogger.Log($"PlaySoundNow Android: no raw file for tone '{tone}'");
            return;
        }
        var ctx   = Android.App.Application.Context;
        var resId = ctx.Resources?.GetIdentifier(fileName, "raw", ctx.PackageName) ?? 0;
        if (resId == 0)
        {
            FileLogger.Log($"PlaySoundNow Android: raw resource '{fileName}' not found");
            return;
        }
        _player = Android.Media.MediaPlayer.Create(ctx, resId);
        if (_player == null)
        {
            FileLogger.Log($"PlaySoundNow Android: MediaPlayer.Create returned null");
            return;
        }
        _player.Completion += (_, _) => StopSoundNow();
        _player.Start();
        FileLogger.Log($"PlaySoundNow Android: playing '{fileName}'");
    }
#else
    public static bool IsSoundPlaying => false;
    public static void StopSoundNow() { }
    public static void PlaySoundNow(string tone) { }
#endif

    // ── Test: fires 10 seconds from now ──────────────────────────────────────

    public static async Task ScheduleTestAsync(string tone = "Default")
    {
        var at = DateTime.Now.AddSeconds(10);
        FileLogger.Log($"ScheduleTest: will fire at {at:HH:mm:ss} tone={tone}");
        await PostAsync(9999, at, "Test Alarm 🔔", $"Sound: {tone}", tone);
        FileLogger.Log("ScheduleTest: done");
    }

    // ── Schedule all prayer alarms for the next 7 days ────────────────────────

    public static async Task ScheduleAllAsync(
        PrayerMonthCache? currentCache,
        PrayerMonthCache? nextCache = null)
    {
        CancelAll();

        bool beforeOn = Preferences.Get("notif_before_on", false);
        bool onOn     = Preferences.Get("notif_on_on",     false);
        bool endsOn   = Preferences.Get("notif_ends_on",   false);

        FileLogger.Log($"ScheduleAll: before={beforeOn} on={onOn} ends={endsOn} cache={currentCache?.Days.Count ?? -1}d");

        if (!beforeOn && !onOn && !endsOn)
        {
            FileLogger.Log("ScheduleAll: all off → nothing scheduled");
            return;
        }

        if (currentCache is null)
        {
            FileLogger.Log("ScheduleAll: cache is NULL → cannot schedule");
            return;
        }

#if ANDROID
        var alarmMgr = (Android.App.AlarmManager)Android.App.Application.Context
            .GetSystemService(Android.Content.Context.AlarmService)!;
        bool canExact = Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.S
                     || alarmMgr.CanScheduleExactAlarms();
        FileLogger.Log($"ScheduleAll: canScheduleExactAlarms={canExact}");
        if (!canExact)
        {
            FileLogger.Log("ScheduleAll: EXACT ALARM PERMISSION MISSING — notifications may be delayed");
        }
#endif

        int  beforeH    = Preferences.Get("notif_before_h",    0);
        int  beforeM    = Preferences.Get("notif_before_m",   30);
        var  beforeTone = Preferences.Get("notif_before_tone", "Default");
        var  onTone     = Preferences.Get("notif_on_tone",     "Default");
        int  endsH      = Preferences.Get("notif_ends_h",      0);
        int  endsM      = Preferences.Get("notif_ends_m",     30);
        var  endsTone   = Preferences.Get("notif_ends_tone",   "Default");

        var now = DateTime.Now;
        int scheduled = 0;
        int failed    = 0;

        // ── Special case: before Fajr today (currently in last night's Isha) ───────────────
        // The main loop below starts at dayOffset=0 (today's prayers, starting this evening).
        // If it is currently before Fajr, the active Isha started YESTERDAY and ends at
        // TODAY's Fajr. That Isha is never in the 0-6 loop, so we schedule it here.
        if (endsOn && (endsH > 0 || endsM > 0))
        {
            var todayData = GetDay(now.Date, currentCache, nextCache);
            if (todayData is not null
                && TryParseDateTime(todayData.Timings.Fajr, now.Date, out var todayFajr)
                && now < todayFajr)
            {
                var t = todayFajr.AddHours(-endsH).AddMinutes(-endsM);
                int endsTotal0   = endsH * 60 + endsM;
                int beforeTotal0 = beforeH * 60 + beforeM;
                bool skip0 = beforeOn && (beforeH > 0 || beforeM > 0)
                             && endsTotal0 <= beforeTotal0;
                FileLogger.Log($"EndsIn Isha (tonight→Fajr): todayFajr={todayFajr:HH:mm} t={t:HH:mm} now={now:HH:mm} skip={skip0} schedule={!skip0 && t > now}");
                if (!skip0 && t > now)
                {
                    try
                    {
                        var ishName = LocalizationService.GetString("Prayer_Isha");
                        await PostAsync(
                            800 + SlotEnds,
                            t,
                            LocalizationService.GetString("Notif_Title"),
                            FormatEndsBody(ishName, endsH, endsM),
                            endsTone);
                        scheduled++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        FileLogger.Log($"ScheduleAll ERROR pre-Fajr Isha: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date      = now.Date.AddDays(dayOffset);
            var prayerDay = GetDay(date, currentCache, nextCache);
            if (prayerDay is null) continue;

            (string Name, string Raw)[] prayers =
            [
                ("Fajr",    prayerDay.Timings.Fajr),
                ("Dhuhr",   prayerDay.Timings.Dhuhr),
                ("Asr",     prayerDay.Timings.Asr),
                ("Maghrib", prayerDay.Timings.Maghrib),
                ("Isha",    prayerDay.Timings.Isha),
            ];

            for (int pi = 0; pi < prayers.Length; pi++)
            {
                var (name, raw) = prayers[pi];
                if (!TryParseDateTime(raw, date, out var prayerTime)) continue;

                int baseId    = dayOffset * 100 + pi * 10;
                var localName = LocalizationService.GetString($"Prayer_{name}");

                try
                {
                    if (beforeOn && (beforeH > 0 || beforeM > 0))
                    {
                        var t = prayerTime.AddHours(-beforeH).AddMinutes(-beforeM);
                        if (t > now)
                        {
                            await PostAsync(baseId + SlotBefore, t,
                                LocalizationService.GetString("Notif_Title"),
                                FormatBeforeBody(localName, beforeH, beforeM),
                                beforeTone);
                            scheduled++;
                        }
                    }

                    if (onOn && prayerTime > now)
                    {
#if ANDROID
                        if (IsLongAdhan(onTone))
                        {
                            await PostAsync(baseId + SlotOn, prayerTime,
                                LocalizationService.GetString("Notif_Title"),
                                string.Format(LocalizationService.GetString("Notif_OnBody"), localName),
                                "Silent");
                            var sf = ToneToSoundFile(onTone);
                            if (sf != null)
                                AdhanPlayerService.ScheduleAdhan(
                                    baseId + SlotOn, prayerTime, localName, sf);
                        }
                        else
                        {
                            await PostAsync(baseId + SlotOn, prayerTime,
                                LocalizationService.GetString("Notif_Title"),
                                string.Format(LocalizationService.GetString("Notif_OnBody"), localName),
                                onTone);
                        }
#else
                        await PostAsync(baseId + SlotOn, prayerTime,
                            LocalizationService.GetString("Notif_Title"),
                            string.Format(LocalizationService.GetString("Notif_OnBody"), localName),
                            onTone);
#endif
                        scheduled++;
                    }

                    if (endsOn && (endsH > 0 || endsM > 0))
                    {
                        DateTime endTime = default;
                        bool hasEnd;
                        if (pi < prayers.Length - 1)
                        {
                            hasEnd = TryParseDateTime(prayers[pi + 1].Raw, date, out endTime);
                        }
                        else
                        {
                            var nextDate = date.AddDays(1);
                            var nextDay  = GetDay(nextDate, currentCache, nextCache);
                            hasEnd = nextDay is not null &&
                                     TryParseDateTime(nextDay.Timings.Fajr, nextDate, out endTime);
                        }

                        if (hasEnd)
                        {
                            var t = endTime.AddHours(-endsH).AddMinutes(-endsM);

                            // Skip if "Before" notification fires at the same time or later:
                            // endsOffset <= beforeOffset means "Ends" would fire after (or same as)
                            // the "Before" notification — redundant or wrong order.
                            int endsTotal  = endsH * 60 + endsM;
                            int beforeTotal = beforeH * 60 + beforeM;
                            bool skip = beforeOn && (beforeH > 0 || beforeM > 0)
                                        && endsTotal <= beforeTotal;

                            FileLogger.Log($"EndsIn {name} day+{dayOffset}: endTime={endTime:HH:mm} t={t:HH:mm} now={now:HH:mm} skip={skip} schedule={!skip && t > now}");
                            if (!skip && t > now)
                            {
                                await PostAsync(baseId + SlotEnds, t,
                                    LocalizationService.GetString("Notif_Title"),
                                    FormatEndsBody(localName, endsH, endsM),
                                    endsTone);
                                scheduled++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    FileLogger.Log($"ScheduleAll ERROR day={dayOffset} prayer={name}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        FileLogger.Log($"ScheduleAll: done, {scheduled} scheduled, {failed} failed");
    }

    // ── Cancel all pending alarms ─────────────────────────────────────────────

    static void CancelAll()
    {
#if IOS
        UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
        UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
#elif ANDROID
        LocalNotificationCenter.Current.CancelAll();
        AdhanPlayerService.CancelAll();
#endif
    }

    // ── Post a single alarm ───────────────────────────────────────────────────

    static async Task PostAsync(int id, DateTime at, string title, string body, string tone)
    {
        FileLogger.Log($"PostAsync id={id} at={at:HH:mm:ss} tone={tone}");
        if (at <= DateTime.Now) return;

        bool silent = tone == "Silent";

#if IOS
        var secondsUntil = (at - DateTime.Now).TotalSeconds;
        if (secondsUntil <= 0) return;

        var soundFile = ToneToSoundFileIos(tone);
        var content   = new UNMutableNotificationContent
        {
            Title = title,
            Body  = body,
            Sound = silent      ? null
                  : soundFile != null ? UNNotificationSound.GetSound(soundFile)
                  :                     UNNotificationSound.Default,
        };

        var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(secondsUntil, false);
        var request = UNNotificationRequest.FromIdentifier(id.ToString(), content, trigger);

        var tcs = new TaskCompletionSource<bool>();
        UNUserNotificationCenter.Current.AddNotificationRequest(request, err =>
        {
            if (err != null) FileLogger.Log($"iOS id={id} ERROR: {err.LocalizedDescription}");
            else             FileLogger.Log($"iOS id={id} SUCCESS");
            tcs.TrySetResult(err is null);
        });
        await tcs.Task;

#elif ANDROID
        var channelId = ToneToChannel(tone);
        var soundFile = silent ? null : ToneToSoundFile(tone);

        await LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = id,
            Title          = title,
            Description    = body,
            Silent         = silent,
            Sound          = soundFile,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = at,
                RepeatType = NotificationRepeat.No,
            },
            Android = new AndroidOptions { ChannelId = channelId },
        });
#else
        await Task.CompletedTask;
#endif
    }

    // ── iOS helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the actual file name used on the current platform,
    /// or an empty string for Default / Silent / unmapped tones.
    /// </summary>
    public static string GetDisplayFileName(string tone)
    {
#if IOS
        return ToneToSoundFileIos(tone) ?? "";
#elif ANDROID
        var raw = ToneToSoundFile(tone);
        if (raw == null) return "";
        return raw == "ezan_1" ? "ezan_1.wav" : raw + ".mp3";
#else
        return "";
#endif
    }

#if IOS
    static string? ToneToSoundFileIos(string tone) => tone switch
    {
        "Adhan Bayati" => "adhan_bayati2.wav",
        "Apple"        => "apple.wav",
        "Early Riser"  => "early_riser.wav",
        "iPhone Alarm" => "iphone_alarm_music.wav",
        "Revelation"   => "revelation.wav",
        "Apple Hard"   => "apple_android_hard.wav",
        "Aranan Zil"   => "aranan_zil_sesi.wav",
        _              => null,   // Default, Ezan 1, Silent → system default or null
    };
#else
    static string? ToneToSoundFileIos(string tone) => null;
#endif

    // ── Android helpers ───────────────────────────────────────────────────────

    static string ToneToChannel(string tone) => tone switch
    {
        "Adhan Bayati" => ChannelBayati,
        "Apple"        => ChannelApple,
        "Early Riser"  => ChannelEarlyRiser,
        "iPhone Alarm" => ChannelIphone,
        "Revelation"   => ChannelRevelation,
        "Apple Hard"   => ChannelAppleHard,
        "Aranan Zil"   => ChannelAranan,
        "Ezan 1"       => ChannelEzan,
        "Silent"       => ChannelSilent,
        _              => ChannelDefault,
    };

    static string? ToneToSoundFile(string tone) => tone switch
    {
        "Adhan Bayati" => "adhan_bayati",
        "Apple"        => "apple",
        "Early Riser"  => "early_riser",
        "iPhone Alarm" => "iphone_alarm_music",
        "Revelation"   => "revelation",
        "Apple Hard"   => "apple_android_hard",
        "Aranan Zil"   => "aranan_zil_sesi",
        "Ezan 1"       => "ezan_1",
        _              => null,
    };

    // ── Shared helpers ────────────────────────────────────────────────────────

    static PrayerDay? GetDay(DateTime date, PrayerMonthCache? cur, PrayerMonthCache? next)
    {
        if (cur?.Year == date.Year && cur.Month == date.Month)
            return cur.Days.FirstOrDefault(d => d.Day == date.Day);
        if (next?.Year == date.Year && next.Month == date.Month)
            return next.Days.FirstOrDefault(d => d.Day == date.Day);
        return null;
    }

    static bool TryParseDateTime(string raw, DateTime date, out DateTime result)
    {
        result = default;
        if (string.IsNullOrEmpty(raw) || raw == "—") return false;
        if (TimeSpan.TryParseExact(raw, @"hh\:mm", null, out var ts)
            || TimeSpan.TryParseExact(raw, @"h\:mm",  null, out ts))
        {
            result = date + ts;
            return true;
        }
        return false;
    }

    // Only Ezan 1 uses the ForegroundService path on Android (long adhan ~2 min)
    static bool IsLongAdhan(string tone) => tone == "Ezan 1";

    static string FormatOffset(int h, int m)
    {
        if (h == 0) return $"{m} min";
        if (m == 0) return $"{h} h";
        return $"{h} h {m} min";
    }

    static string FormatBeforeBody(string localName, int h, int m)
    {
        if (h > 0)
            return string.Format(LocalizationService.GetString("Notif_BeforeBody_H"), localName, h, m);
        return string.Format(LocalizationService.GetString("Notif_BeforeBody"), localName, m);
    }

    static string FormatEndsBody(string localName, int h, int m)
    {
        if (h > 0)
            return string.Format(LocalizationService.GetString("Notif_EndsBody_H"), localName, h, m);
        return string.Format(LocalizationService.GetString("Notif_EndsBody"), localName, m);
    }
}
