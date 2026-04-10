using System.IO;
using System.Windows;
using VaultApp.Core.Services;
using VaultApp.ViewModels;

namespace VaultApp;

public partial class App : Application
{
    public static VaultService VaultService { get; private set; } = null!;
    public static ClipboardService ClipboardService { get; private set; } = null!;
    public static PasswordGeneratorService GeneratorService { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var vaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultApp",
            "vault.dat");

        Directory.CreateDirectory(Path.GetDirectoryName(vaultPath)!);

        var storage = new StorageService(vaultPath);
        VaultService = new VaultService(storage);
        ClipboardService = new ClipboardService();
        GeneratorService = new PasswordGeneratorService();

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