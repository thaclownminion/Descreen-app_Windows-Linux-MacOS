using System;
using System.Collections.Generic;
using System.Timers;

namespace Descreen;

public class TimerManager
{
    // ── Settings ──────────────────────────────────────────────────────────────
    public double WorkInterval  { get; set; } = 20 * 60;
    public double BreakDuration { get; set; } = 20;
    public double FocusDuration { get; set; } = 60 * 60;
    public bool   IsFocusModeActive { get; private set; } = false;
    public string BreakMessage { get; set; } = "Time for a break!";

    // Schedule
    public bool ScheduleEnabled  { get; set; } = false;
    public bool MondayEnabled    { get; set; } = true;
    public bool TuesdayEnabled   { get; set; } = true;
    public bool WednesdayEnabled { get; set; } = true;
    public bool ThursdayEnabled  { get; set; } = true;
    public bool FridayEnabled    { get; set; } = true;
    public bool SaturdayEnabled  { get; set; } = false;
    public bool SundayEnabled    { get; set; } = false;

    // Notifications
    public bool NotificationsEnabled   { get; set; } = true;
    public bool UseSystemNotifications { get; set; } = true;
    public List<int> NotificationTiming { get; set; } = new() { 5, 2, 1 };

    // Quit delay
    public bool QuitDelayEnabled  { get; set; } = false;
    public int  QuitDelaySeconds  { get; set; } = 10;

    // Focus cooldown
    public bool FocusCooldownEnabled  { get; set; } = false;
    public int  FocusCooldownMinutes  { get; set; } = 30;

    // ── Callbacks (may fire on timer thread) ──────────────────────────────────
    public Action? OnBreakStart;
    public Action? OnBreakEnd;
    public Action<double>? OnTimeUpdate;
    public Action<double>? OnFocusUpdate;
    public Action? OnSettingsChange;
    public Action? OnTimerStateChange;
    public Action<int>? OnBreakWarning;   // minutes remaining
    public Action<int>? OnBreakCountdown; // seconds remaining ≤ 60

    // ── Private ───────────────────────────────────────────────────────────────
    private Timer? _workTimer, _breakTimer, _focusTimer;
    private double _remainingWork, _remainingBreak, _remainingFocus;
    private bool   _isOnBreak = false;
    private readonly HashSet<int> _notifSent = new();

    public TimerManager()
    {
        LoadSettings();
        _remainingWork = WorkInterval;
    }

    public void Start() => StartWorkTimer();

    // ── Work ──────────────────────────────────────────────────────────────────
    private void StartWorkTimer()
    {
        Kill(ref _workTimer);
        _remainingWork = WorkInterval;
        _notifSent.Clear();

        _workTimer = new Timer(1000) { AutoReset = true };
        _workTimer.Elapsed += (_, _) =>
        {
            if (IsFocusModeActive || !IsTodayEnabled())
            { OnTimeUpdate?.Invoke(_remainingWork); return; }

            _remainingWork -= 1;
            OnTimeUpdate?.Invoke(_remainingWork);
            CheckNotifications();
            if (_remainingWork <= 0) StartBreak();
        };
        _workTimer.Start();
    }

    private void CheckNotifications()
    {
        if (!NotificationsEnabled) return;

        int mins = (int)(_remainingWork / 60);
        int secs = (int)_remainingWork;
        int rem  = (int)(_remainingWork % 60);

        if (rem <= 1)
            foreach (var t in NotificationTiming)
                if (mins == t && _notifSent.Add(t))
                    OnBreakWarning?.Invoke(t);

        if (!UseSystemNotifications && secs <= 60 && secs > 0)
            OnBreakCountdown?.Invoke(secs);

        if (_remainingWork >= WorkInterval) _notifSent.Clear();
    }

    // ── Break ─────────────────────────────────────────────────────────────────
    private void StartBreak()
    {
        Kill(ref _workTimer);
        _isOnBreak = true;
        _remainingBreak = BreakDuration;
        OnBreakStart?.Invoke();

        _breakTimer = new Timer(1000) { AutoReset = true };
        _breakTimer.Elapsed += (_, _) =>
        {
            _remainingBreak -= 1;
            if (_remainingBreak <= 0)
            {
                Kill(ref _breakTimer);
                _isOnBreak = false;
                _remainingBreak = 0;
                OnBreakEnd?.Invoke();
                System.Threading.Thread.Sleep(150);
                StartWorkTimer();
            }
        };
        _breakTimer.Start();
    }

    // ── Focus ─────────────────────────────────────────────────────────────────
    public void StartFocusMode()
    {
        IsFocusModeActive = true;
        _remainingFocus = FocusDuration;
        OnTimerStateChange?.Invoke();

        _focusTimer = new Timer(1000) { AutoReset = true };
        _focusTimer.Elapsed += (_, _) =>
        {
            _remainingFocus -= 1;
            OnFocusUpdate?.Invoke(_remainingFocus);
            if (_remainingFocus <= 0) EndFocusMode();
        };
        _focusTimer.Start();
        OnSettingsChange?.Invoke();
    }

    public void EndFocusMode()
    {
        Kill(ref _focusTimer);
        IsFocusModeActive = false;
        _remainingFocus = 0;
        OnFocusUpdate?.Invoke(0);
        Prefs.Set("lastFocusEnd", DateTime.Now.Ticks.ToString());
        StartWorkTimer();
        OnSettingsChange?.Invoke();
        OnTimerStateChange?.Invoke();
    }

    public void TriggerBreakNow()
    {
        if (!_isOnBreak && !IsFocusModeActive && IsTodayEnabled())
        { Kill(ref _workTimer); StartBreak(); }
    }

    // ── Getters ───────────────────────────────────────────────────────────────
    public double GetRemainingBreakTime() => Math.Max(0, _remainingBreak);
    public double GetRemainingWorkTime()  => Math.Max(0, _remainingWork);
    public double GetRemainingFocusTime() => _remainingFocus;

    public void UpdateSettings(double workSec, double breakSec, double focusSec, string msg)
    {
        WorkInterval  = workSec;
        BreakDuration = breakSec;
        FocusDuration = focusSec;
        BreakMessage  = msg;
        SaveSettings();
        OnTimerStateChange?.Invoke();
        OnSettingsChange?.Invoke();
        if (!_isOnBreak) StartWorkTimer();
    }

    // ── Schedule ──────────────────────────────────────────────────────────────
    private bool IsTodayEnabled()
    {
        if (!ScheduleEnabled) return true;
        return DateTime.Now.DayOfWeek switch
        {
            DayOfWeek.Monday    => MondayEnabled,
            DayOfWeek.Tuesday   => TuesdayEnabled,
            DayOfWeek.Wednesday => WednesdayEnabled,
            DayOfWeek.Thursday  => ThursdayEnabled,
            DayOfWeek.Friday    => FridayEnabled,
            DayOfWeek.Saturday  => SaturdayEnabled,
            DayOfWeek.Sunday    => SundayEnabled,
            _ => true
        };
    }

    // ── Persistence (simple cross-platform key/value) ─────────────────────────
    public void SaveSettings()
    {
        Prefs.Set("workInterval",          WorkInterval.ToString());
        Prefs.Set("breakDuration",         BreakDuration.ToString());
        Prefs.Set("focusDuration",         FocusDuration.ToString());
        Prefs.Set("breakMessage",          BreakMessage);
        Prefs.Set("notificationsEnabled",  NotificationsEnabled.ToString());
        Prefs.Set("useSystemNotif",        UseSystemNotifications.ToString());
        Prefs.Set("notifTiming",           string.Join(",", NotificationTiming));
        Prefs.Set("scheduleEnabled",       ScheduleEnabled.ToString());
        Prefs.Set("mondayEnabled",         MondayEnabled.ToString());
        Prefs.Set("tuesdayEnabled",        TuesdayEnabled.ToString());
        Prefs.Set("wednesdayEnabled",      WednesdayEnabled.ToString());
        Prefs.Set("thursdayEnabled",       ThursdayEnabled.ToString());
        Prefs.Set("fridayEnabled",         FridayEnabled.ToString());
        Prefs.Set("saturdayEnabled",       SaturdayEnabled.ToString());
        Prefs.Set("sundayEnabled",         SundayEnabled.ToString());
        Prefs.Set("quitDelayEnabled",      QuitDelayEnabled.ToString());
        Prefs.Set("quitDelaySeconds",      QuitDelaySeconds.ToString());
        Prefs.Set("focusCooldownEnabled",  FocusCooldownEnabled.ToString());
        Prefs.Set("focusCooldownMinutes",  FocusCooldownMinutes.ToString());
    }

    private void LoadSettings()
    {
        if (double.TryParse(Prefs.Get("workInterval"),  out var w) && w > 0) WorkInterval  = w;
        if (double.TryParse(Prefs.Get("breakDuration"), out var b) && b > 0) BreakDuration = b;
        if (double.TryParse(Prefs.Get("focusDuration"), out var f) && f > 0) FocusDuration = f;

        var msg = Prefs.Get("breakMessage");
        if (!string.IsNullOrEmpty(msg)) BreakMessage = msg;

        if (bool.TryParse(Prefs.Get("notificationsEnabled"), out var ne)) NotificationsEnabled   = ne;
        if (bool.TryParse(Prefs.Get("useSystemNotif"),       out var sn)) UseSystemNotifications = sn;

        var timing = Prefs.Get("notifTiming");
        if (!string.IsNullOrEmpty(timing))
        {
            NotificationTiming = new();
            foreach (var p in timing.Split(','))
                if (int.TryParse(p.Trim(), out var v)) NotificationTiming.Add(v);
        }

        if (bool.TryParse(Prefs.Get("scheduleEnabled"),      out var se)) ScheduleEnabled  = se;
        if (bool.TryParse(Prefs.Get("mondayEnabled"),        out var mo)) MondayEnabled    = mo;
        if (bool.TryParse(Prefs.Get("tuesdayEnabled"),       out var tu)) TuesdayEnabled   = tu;
        if (bool.TryParse(Prefs.Get("wednesdayEnabled"),     out var we)) WednesdayEnabled = we;
        if (bool.TryParse(Prefs.Get("thursdayEnabled"),      out var th)) ThursdayEnabled  = th;
        if (bool.TryParse(Prefs.Get("fridayEnabled"),        out var fr)) FridayEnabled    = fr;
        if (bool.TryParse(Prefs.Get("saturdayEnabled"),      out var sa)) SaturdayEnabled  = sa;
        if (bool.TryParse(Prefs.Get("sundayEnabled"),        out var su)) SundayEnabled    = su;
        if (bool.TryParse(Prefs.Get("quitDelayEnabled"),     out var qd)) QuitDelayEnabled = qd;
        if (int.TryParse (Prefs.Get("quitDelaySeconds"),     out var qs) && qs > 0) QuitDelaySeconds = qs;
        if (bool.TryParse(Prefs.Get("focusCooldownEnabled"), out var fc)) FocusCooldownEnabled = fc;
        if (int.TryParse (Prefs.Get("focusCooldownMinutes"), out var fm) && fm > 0) FocusCooldownMinutes = fm;
    }

    private static void Kill(ref Timer? t) { t?.Stop(); t?.Dispose(); t = null; }
}
