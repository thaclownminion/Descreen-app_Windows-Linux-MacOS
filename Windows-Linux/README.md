# Descreen — Windows & Linux

Eye break reminder app. Sits in your **taskbar** as a regular window.

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) — needed on whichever machine you build on

---

## Build on macOS (your Intel MacBook) → run on Windows or Linux

Install .NET 8 SDK, then open Terminal in this folder.

**For Windows:**
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./build/windows
```

**For Linux (x64):**
```
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./build/linux
```

Both commands cross-compile from your Mac. The output files are:
- `build/windows/Descreen.exe`
- `build/linux/Descreen`

Copy these to the target machine and run them — no .NET install needed there.

---

## Build directly on Windows

1. Install .NET 8 SDK from https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. Open Command Prompt in this folder
3. Run the app directly:
   ```
   dotnet run
   ```
   Or build a standalone .exe:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./build/windows
   ```

---

## Build directly on Linux

1. Install .NET 8 SDK:
   ```
   sudo apt install dotnet-sdk-8.0
   ```
   (On Fedora/RHEL: `sudo dnf install dotnet-sdk-8.0`)

2. Open a terminal in this folder and run:
   ```
   dotnet run
   ```
   Or build a standalone binary:
   ```
   dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./build/linux
   chmod +x ./build/linux/Descreen
   ./build/linux/Descreen
   ```

---

## Features

- Main window lives in the taskbar — no system tray needed
- Full-screen break overlay on all monitors
- Focus Mode with optional cooldown
- Weekly schedule (breaks only on selected days)
- In-app warning banner or system notifications before breaks
- Quit protection with optional delay countdown
- Launch at login (Windows registry / Linux autostart .desktop file)
- Settings persist across restarts

---

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point |
| `App.axaml/.cs` | App bootstrap |
| `MainWindow.axaml/.cs` | Main window (taskbar) |
| `MainViewModel.cs` | Main window logic |
| `BreakOverlayWindow.axaml/.cs` | Full-screen break overlay |
| `SettingsWindow.axaml/.cs` | Settings (5 tabs) |
| `TimerManager.cs` | All timer logic |
| `Prefs.cs` | Cross-platform settings storage |
