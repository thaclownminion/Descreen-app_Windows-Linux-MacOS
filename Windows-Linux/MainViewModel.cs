using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace Descreen;

public partial class MainViewModel : ObservableObject
{
    private readonly TimerManager _timer;

    [ObservableProperty] private string _timeLeftText   = "Calculating...";
    [ObservableProperty] private string _focusStatusText = "Focus Mode: Off";
    [ObservableProperty] private string _focusButtonText = "Enable Focus Mode";
    [ObservableProperty] private bool   _showWarning    = false;
    [ObservableProperty] private string _warningTitle   = "";
    [ObservableProperty] private string _warningSubtitle = "";
    [ObservableProperty] private bool   _showCountdown  = false;
    [ObservableProperty] private string _countdownText  = "";

    public MainViewModel(TimerManager timer)
    {
        _timer = timer;

        _timer.OnTimeUpdate = remaining => UI(() =>
        {
            int m = (int)remaining / 60, s = (int)remaining % 60;
            TimeLeftText = $"Next break in:  {m}:{s:D2}";
        });

        _timer.OnFocusUpdate = remaining => UI(() =>
        {
            FocusStatusText = remaining > 0
                ? $"Focus Mode: {(int)remaining / 60}:{(int)remaining % 60:D2} left"
                : "Focus Mode: Off";
        });

        _timer.OnSettingsChange   = () => UI(UpdateFocusButton);
        _timer.OnTimerStateChange = () => UI(UpdateFocusButton);

        _timer.OnBreakWarning = mins => UI(() =>
        {
            // Fire a real OS notification — appears in notification centre / top bar
            string title = mins <= 1 ? "Break Starting Soon!" : "Break Coming Soon";
            string body  = $"Your eye break starts in {mins} minute{(mins == 1 ? "" : "s")}";
            Notifier.Send(title, body);
        });

        _timer.OnBreakCountdown = secs => UI(() =>
        {
            // Live countdown stays in-app since OS notifications can't update in real time
            WarningTitle    = "Break Starting Soon!";
            WarningSubtitle = "Get ready to rest your eyes";
            CountdownText   = secs.ToString();
            ShowCountdown   = true;
            ShowWarning     = true;

            if (secs == 0)
                DispatcherTimer.RunOnce(() => ShowWarning = false, TimeSpan.FromMilliseconds(500));
        });

        _timer.OnBreakEnd = () => UI(() => ShowWarning = false);
    }

    private void UpdateFocusButton()
    {
        FocusButtonText = _timer.IsFocusModeActive ? "Disable Focus Mode" : "Enable Focus Mode";
    }

    [RelayCommand]
    private void ToggleFocusMode()
    {
        if (_timer.IsFocusModeActive) { _timer.EndFocusMode(); return; }

        if (_timer.FocusCooldownEnabled)
        {
            var raw = Prefs.Get("lastFocusEnd");
            if (long.TryParse(raw, out var ticks))
            {
                double elapsed  = (DateTime.Now - new DateTime(ticks)).TotalSeconds;
                double cooldown = _timer.FocusCooldownMinutes * 60;
                if (elapsed < cooldown)
                {
                    double rem = cooldown - elapsed;
                    // Show cooldown message in warning banner
                    WarningTitle    = "Focus Mode Cooldown";
                    WarningSubtitle = $"Please wait {(int)rem / 60}:{(int)rem % 60:D2} before enabling again";
                    ShowCountdown   = false;
                    ShowWarning     = true;
                    DispatcherTimer.RunOnce(() => ShowWarning = false, TimeSpan.FromSeconds(4));
                    return;
                }
            }
        }
        _timer.StartFocusMode();
    }

    [RelayCommand]
    private void TakeBreakNow() => _timer.TriggerBreakNow();

    [RelayCommand]
    private void OpenSettings()
    {
        var win = new SettingsWindow(_timer);
        win.Show();
    }

    public TimerManager Timer => _timer;

    private static void UI(Action a) =>
        Dispatcher.UIThread.Post(a, DispatcherPriority.Normal);
}
