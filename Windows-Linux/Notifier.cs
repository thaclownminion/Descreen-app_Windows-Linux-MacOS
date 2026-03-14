using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Descreen;

/// <summary>
/// Sends a native OS notification on Windows and Linux.
/// - Windows: uses PowerShell's BurntToast-free toast via Windows.UI.Notifications
/// - Linux:   uses notify-send (pre-installed on Ubuntu/GNOME)
/// </summary>
public static class Notifier
{
    public static void Send(string title, string body)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                SendLinux(title, body);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                SendWindows(title, body);
        }
        catch
        {
            // Never crash the app over a notification
        }
    }

    // ── Linux ─────────────────────────────────────────────────────────────────
    // notify-send ships with libnotify-bin, which is installed by default
    // on Ubuntu, Fedora, Arch, and most GNOME/KDE desktops.
    private static void SendLinux(string title, string body)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName  = "notify-send",
            Arguments = $"--app-name=\"Descreen\" --icon=dialog-information \"{Escape(title)}\" \"{Escape(body)}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        });
    }

    // ── Windows ───────────────────────────────────────────────────────────────
    // Uses PowerShell to fire a Windows toast notification.
    // Works on Windows 10 and 11 with no extra packages.
    private static void SendWindows(string title, string body)
    {
        // PowerShell script that sends a Windows 10/11 toast notification
        string script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType=WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType=WindowsRuntime] | Out-Null
$template = @""
<toast>
  <visual>
    <binding template='ToastGeneric'>
      <text>{EscapeXml(title)}</text>
      <text>{EscapeXml(body)}</text>
    </binding>
  </visual>
</toast>
""@
$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml($template)
$toast = New-Object Windows.UI.Notifications.ToastNotification $xml
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Descreen').Show($toast)
";

        Process.Start(new ProcessStartInfo
        {
            FileName  = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{script.Replace("\"", "\\\"")}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        });
    }

    private static string Escape(string s)    => s.Replace("\"", "\\\"");
    private static string EscapeXml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
