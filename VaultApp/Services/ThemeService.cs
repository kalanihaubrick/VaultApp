using System.Windows;
using VaultApp.Core.Services;

namespace VaultApp.Services;

public class ThemeService
{
    private readonly UserPreferencesService _preferencesService;
    private string _currentTheme = "dark";

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public string CurrentTheme => _currentTheme;

    public ThemeService(UserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    public void LoadPersistedTheme()
    {
        _preferencesService.Load();
        _currentTheme = _preferencesService.Theme;
        ApplyTheme(_currentTheme);
    }

    public void SetTheme(string theme)
    {
        if (theme != "light" && theme != "dark")
            throw new ArgumentException("Theme must be 'light' or 'dark'");

        if (_currentTheme == theme)
            return;

        _currentTheme = theme;
        _preferencesService.Theme = theme;
        _preferencesService.Save();
        ApplyTheme(theme);

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme));
    }

    private void ApplyTheme(string theme)
    {
        var app = Application.Current;
        var resources = app.Resources;

        System.Windows.Media.Color primary, surface, accent, highlight, textPrimary, textSecondary, danger;

        if (theme == "light")
        {
            primary = new System.Windows.Media.Color { A = 255, R = 0xF5, G = 0xF5, B = 0xF5 };
            surface = new System.Windows.Media.Color { A = 255, R = 0xFF, G = 0xFF, B = 0xFF };
            accent = new System.Windows.Media.Color { A = 255, R = 0xE8, G = 0xF0, B = 0xFF };
            highlight = new System.Windows.Media.Color { A = 255, R = 0xE9, G = 0x45, B = 0x60 };
            textPrimary = new System.Windows.Media.Color { A = 255, R = 0x1A, G = 0x1A, B = 0x2E };
            textSecondary = new System.Windows.Media.Color { A = 255, R = 0x66, G = 0x66, B = 0x66 };
            danger = new System.Windows.Media.Color { A = 255, R = 0xCF, G = 0x66, B = 0x79 };
        }
        else
        {
            primary = new System.Windows.Media.Color { A = 255, R = 0x1A, G = 0x1A, B = 0x2E };
            surface = new System.Windows.Media.Color { A = 255, R = 0x16, G = 0x21, B = 0x3E };
            accent = new System.Windows.Media.Color { A = 255, R = 0x0F, G = 0x34, B = 0x60 };
            highlight = new System.Windows.Media.Color { A = 255, R = 0xE9, G = 0x45, B = 0x60 };
            textPrimary = new System.Windows.Media.Color { A = 255, R = 0xEA, G = 0xEA, B = 0xEA };
            textSecondary = new System.Windows.Media.Color { A = 255, R = 0x9E, G = 0x9E, B = 0x9E };
            danger = new System.Windows.Media.Color { A = 255, R = 0xCF, G = 0x66, B = 0x79 };
        }

        // Atualiza as Colors
        resources["PrimaryColor"] = primary;
        resources["SurfaceColor"] = surface;
        resources["AccentColor"] = accent;
        resources["HighlightColor"] = highlight;
        resources["TextPrimaryColor"] = textPrimary;
        resources["TextSecondaryColor"] = textSecondary;
        resources["DangerColor"] = danger;

        // Recria os Brushes com as novas cores para forçar atualização em tempo real
        resources["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(primary);
        resources["SurfaceBrush"] = new System.Windows.Media.SolidColorBrush(surface);
        resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(accent);
        resources["HighlightBrush"] = new System.Windows.Media.SolidColorBrush(highlight);
        resources["TextPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(textPrimary);
        resources["TextSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(textSecondary);
        resources["DangerBrush"] = new System.Windows.Media.SolidColorBrush(danger);
    }
}

public class ThemeChangedEventArgs(string theme) : EventArgs
{
    public string Theme { get; } = theme;
}
