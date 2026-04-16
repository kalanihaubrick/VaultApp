using System.Windows.Input;
using VaultApp.Services;

namespace VaultApp.ViewModels;

public class AppViewModel : BaseViewModel
{
    private readonly ThemeService _themeService;
    private string _currentTheme = "dark";

    public ICommand ToggleThemeCommand { get; }

    public string CurrentTheme
    {
        get => _currentTheme;
        set => SetField(ref _currentTheme, value);
    }

    public AppViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        _currentTheme = _themeService.CurrentTheme;

        ToggleThemeCommand = new RelayCommand(() =>
        {
            var newTheme = _currentTheme == "dark" ? "light" : "dark";
            _themeService.SetTheme(newTheme);
            CurrentTheme = newTheme;
        });

        _themeService.ThemeChanged += (s, e) =>
        {
            CurrentTheme = e.Theme;
        };
    }
}
