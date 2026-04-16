using System.IO;
using System.Windows;
using VaultApp.Core.Crypto;
using VaultApp.Core.Services;
using VaultApp.Services;
using VaultApp.ViewModels;

namespace VaultApp;

public partial class App : Application
{
    public static VaultService VaultService { get; private set; } = null!;
    public static ClipboardService ClipboardService { get; private set; } = null!;
    public static PasswordGeneratorService GeneratorService { get; private set; } = null!;
    public static ThemeService ThemeService { get; private set; } = null!;
    public static AppViewModel AppViewModel { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var vaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultApp",
            "vault.dat");

        Directory.CreateDirectory(Path.GetDirectoryName(vaultPath)!);

        var cryptoService = new CryptoService();
        var storage = new StorageService(vaultPath, cryptoService);
        VaultService = new VaultService(storage);
        ClipboardService = new ClipboardService();
        GeneratorService = new PasswordGeneratorService();

        var userPreferences = new UserPreferencesService();
        ThemeService = new ThemeService(userPreferences);
        ThemeService.LoadPersistedTheme();

        AppViewModel = new AppViewModel(ThemeService);

        VaultService.VaultLocked += OnVaultLocked;

        OpenLoginWindow();
    }

    private void OnVaultLocked(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            foreach (Window w in Windows)
                w.Close();

            OpenLoginWindow();
        });
    }

    private static void OpenLoginWindow()
    {
        var login = new Views.LoginWindow
        {
            DataContext = new LoginViewModel()
        };
        login.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        VaultService?.Lock();
        base.OnExit(e);
    }
}