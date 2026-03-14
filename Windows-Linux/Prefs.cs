using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Descreen;

/// <summary>
/// Simple cross-platform key/value store saved to a JSON file in the user's
/// app-data folder. Works identically on Windows and Linux.
/// </summary>
public static class Prefs
{
    private static readonly string _path;
    private static Dictionary<string, string> _data = new();

    static Prefs()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Descreen");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        Load();
    }

    public static string? Get(string key)
        => _data.TryGetValue(key, out var v) ? v : null;

    public static void Set(string key, string value)
    {
        _data[key] = value;
        Save();
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(_path))
                _data = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(_path)) ?? new();
        }
        catch { _data = new(); }
    }

    private static void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_data)); }
        catch { /* silently ignore write errors */ }
    }
}
