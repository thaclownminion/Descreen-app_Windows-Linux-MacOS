using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Descreen;

public partial class SettingsWindow : Window
{
    private readonly TimerManager _timer;
    private DispatcherTimer? _quitCountdownTimer;
    private int  _quitCountdownRemaining;
    private bool _quitCountdownActive;
    private bool _allowedToQuit = false;

    public SettingsWindow(TimerManager timer)
    {
        InitializeComponent();
        _timer = timer;
        LoadValuesIntoUI();
        WireEvents();
    }

    // ── Load current settings into every control ──────────────────────────────
    private void LoadValuesIntoUI()
    {
        // General
        BreakMessageBox.Text     = _timer.BreakMessage;
        LaunchAtLoginCheck.IsChecked = IsLaunchAtLoginEnabled();

        // Timers
        WorkSlider.Value  = Math.Clamp(_timer.WorkInterval / 60,  1, 120);
        BreakSlider.Value = Math.Clamp(_timer.BreakDuration,      10, 300);
        FocusSlider.Value = Math.Clamp(_timer.FocusDuration / 60, 15, 180);
        UpdateSliderLabels();

        // Notifications
        NotificationsCheck.IsChecked = _timer.NotificationsEnabled;
        Notif5.IsChecked = _timer.NotificationTiming.Contains(5);
        Notif2.IsChecked = _timer.NotificationTiming.Contains(2);
        Notif1.IsChecked = _timer.NotificationTiming.Contains(1);

        // Schedule
        ScheduleCheck.IsChecked  = _timer.ScheduleEnabled;
        MondayCheck.IsChecked    = _timer.MondayEnabled;
        TuesdayCheck.IsChecked   = _timer.TuesdayEnabled;
        WednesdayCheck.IsChecked = _timer.WednesdayEnabled;
        ThursdayCheck.IsChecked  = _timer.ThursdayEnabled;
        FridayCheck.IsChecked    = _timer.FridayEnabled;
        SaturdayCheck.IsChecked  = _timer.SaturdayEnabled;
        SundayCheck.IsChecked    = _timer.SundayEnabled;

        // Advanced
        CooldownCheck.IsChecked     = _timer.FocusCooldownEnabled;
        CooldownSlider.Value        = _timer.FocusCooldownMinutes;
        CooldownValueLabel.Text     = $"{_timer.FocusCooldownMinutes} minutes";
        QuitDelayCheck.IsChecked    = _timer.QuitDelayEnabled;
        QuitDelaySlider.Value       = _timer.QuitDelaySeconds;
        QuitDelayValueLabel.Text    = $"{_timer.QuitDelaySeconds} seconds";

        if (_timer.QuitDelayEnabled) BeginQuitCountdown();
    }

    private void UpdateSliderLabels()
    {
        WorkValueLabel.Text  = $"{(int)WorkSlider.Value} minutes";
        BreakValueLabel.Text = $"{(int)BreakSlider.Value} seconds";
        FocusValueLabel.Text = $"{(int)FocusSlider.Value} minutes";
    }

    // ── Wire all events ───────────────────────────────────────────────────────
    private void WireEvents()
    {
        WorkSlider.PropertyChanged  += (_, e) => { if (e.Property.Name == "Value") UpdateSliderLabels(); };
        BreakSlider.PropertyChanged += (_, e) => { if (e.Property.Name == "Value") UpdateSliderLabels(); };
        FocusSlider.PropertyChanged += (_, e) => { if (e.Property.Name == "Value") UpdateSliderLabels(); };

        CooldownSlider.PropertyChanged += (_, e) =>
        { if (e.Property.Name == "Value") CooldownValueLabel.Text = $"{(int)CooldownSlider.Value} minutes"; };

        QuitDelaySlider.PropertyChanged += (_, e) =>
        { if (e.Property.Name == "Value") QuitDelayValueLabel.Text = $"{(int)QuitDelaySlider.Value} seconds"; };

        ResetMessageBtn.Click += (_, _) => BreakMessageBox.Text = "Time for a break!";
        DonateBtn.Click += (_, _) => OpenUrl("https://buymeacoffee.com/kai_rozema");
        SaveBtn.Click   += (_, _) => SaveSettings();
        QuitBtn.Click   += HandleQuitButton;

        LaunchAtLoginCheck.IsCheckedChanged += (_, _) =>
            SetLaunchAtLogin(LaunchAtLoginCheck.IsChecked == true);
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    private void SaveSettings()
    {
        _timer.NotificationsEnabled   = NotificationsCheck.IsChecked == true;
        _timer.UseSystemNotifications = true; // always system notifications

        var timing = new List<int>();
        if (Notif5.IsChecked == true) timing.Add(5);
        if (Notif2.IsChecked == true) timing.Add(2);
        if (Notif1.IsChecked == true) timing.Add(1);
        _timer.NotificationTiming = timing;

        _timer.ScheduleEnabled  = ScheduleCheck.IsChecked  == true;
        _timer.MondayEnabled    = MondayCheck.IsChecked    == true;
        _timer.TuesdayEnabled   = TuesdayCheck.IsChecked   == true;
        _timer.WednesdayEnabled = WednesdayCheck.IsChecked == true;
        _timer.ThursdayEnabled  = ThursdayCheck.IsChecked  == true;
        _timer.FridayEnabled    = FridayCheck.IsChecked    == true;
        _timer.SaturdayEnabled  = SaturdayCheck.IsChecked  == true;
        _timer.SundayEnabled    = SundayCheck.IsChecked    == true;

        _timer.FocusCooldownEnabled  = CooldownCheck.IsChecked  == true;
        _timer.FocusCooldownMinutes  = (int)CooldownSlider.Value;
        _timer.QuitDelayEnabled      = QuitDelayCheck.IsChecked == true;
        _timer.QuitDelaySeconds      = (int)QuitDelaySlider.Value;

        _timer.UpdateSettings(
            WorkSlider.Value  * 60,
            BreakSlider.Value,
            FocusSlider.Value * 60,
            BreakMessageBox.Text ?? "Time for a break!"
        );

        Hide();
    }

    // ── Quit protection ───────────────────────────────────────────────────────
    private void HandleQuitButton(object? sender, RoutedEventArgs e)
    {
        if (_quitCountdownActive) return;

        if (_timer.QuitDelayEnabled && _quitCountdownRemaining > 0)
            BeginQuitCountdown();
        else
            DoQuit();
    }

    private void DoQuit()
    {
        _allowedToQuit = true;
        // Shutdown Avalonia cleanly first, then force-kill the process
        (App.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
            ?.Shutdown();
        Environment.Exit(0);
    }

    private void BeginQuitCountdown()
    {
        _quitCountdownRemaining = _timer.QuitDelaySeconds;
        _quitCountdownActive = true;
        QuitBtn.IsEnabled = false;
        QuitBtn.Content   = $"Wait {_quitCountdownRemaining}s…";

        _quitCountdownTimer?.Stop();
        _quitCountdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _quitCountdownTimer.Tick += (_, _) =>
        {
            _quitCountdownRemaining--;
            if (_quitCountdownRemaining <= 0)
            {
                _quitCountdownTimer.Stop();
                _quitCountdownActive = false;
                QuitBtn.IsEnabled = true;
                QuitBtn.Content   = "✖  Quit Descreen";
                // Countdown finished — button is now active, user clicks to confirm
            }
            else QuitBtn.Content = $"Wait {_quitCountdownRemaining}s…";
        };
        _quitCountdownTimer.Start();
    }

    // Prevent the X button from closing — just hide (same as macOS)
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowedToQuit)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    // ── Launch at login ───────────────────────────────────────────────────────
    public bool IsLaunchAtLoginEnabled()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("Descreen") != null;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return System.IO.File.Exists(AutostartFilePath());
        }
        return false;
    }

    private void SetLaunchAtLogin(bool enable)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
                key?.SetValue("Descreen", $"\"{Environment.ProcessPath}\"");
            else
                key?.DeleteValue("Descreen", false);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var path = AutostartFilePath();
            if (enable)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                System.IO.File.WriteAllText(path,
                    $"[Desktop Entry]\nType=Application\nName=Descreen\nExec={Environment.ProcessPath}\nX-GNOME-Autostart-enabled=true\n");
            }
            else if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    // ~/.config/autostart/Descreen.desktop — standard on GNOME, KDE, XFCE, etc.
    private static string AutostartFilePath() =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "autostart", "Descreen.desktop");

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
}
