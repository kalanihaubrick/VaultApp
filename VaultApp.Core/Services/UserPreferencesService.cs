using System.IO;
using System.Text.Json;

namespace VaultApp.Core.Services;

public class UserPreferencesService
{
    private readonly string _preferencesPath;
    private Preferences _preferences = new();

    public UserPreferencesService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultApp");
        Directory.CreateDirectory(appDataPath);
        _preferencesPath = Path.Combine(appDataPath, "settings.json");
    }

    public string Theme
    {
        get => _preferences.Theme ?? "dark";
        set => _preferences.Theme = value;
    }

    public void Load()
    {
        if (!File.Exists(_preferencesPath))
        {
            _preferences = new();
            return;
        }

        try
        {
            var json = File.ReadAllText(_preferencesPath);
            _preferences = JsonSerializer.Deserialize<Preferences>(json) ?? new();
        }
        catch
        {
            _preferences = new();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_preferences, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_preferencesPath, json);
        }
        catch { }
    }

    private class Preferences
    {
        public string? Theme { get; set; }
    }
}
