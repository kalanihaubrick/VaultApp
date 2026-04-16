using System.IO;
using Xunit;
using VaultApp.Core.Services;

namespace VaultApp.Core.Tests;

public class UserPreferencesServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempSettingsPath;

    public UserPreferencesServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"vaultapp_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _tempSettingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Theme_DefaultIsDark()
    {
        var service = new UserPreferencesService();
        Assert.Equal("dark", service.Theme);
    }

    [Fact]
    public void Load_NonexistentFile_InitializesDefaults()
    {
        // Delete the settings file to ensure it doesn't exist
        if (File.Exists(_tempSettingsPath))
            File.Delete(_tempSettingsPath);

        var service = new UserPreferencesService();
        service.Load();
        
        Assert.Equal("dark", service.Theme);
    }

    [Fact]
    public void SetTheme_ToLight_Changes()
    {
        var service = new UserPreferencesService();
        Assert.Equal("dark", service.Theme);
        
        service.Theme = "light";
        Assert.Equal("light", service.Theme);
    }

    [Fact]
    public void SaveAndLoad_ThemePersists()
    {
        // Create first service and save light theme
        var service = new UserPreferencesService();
        service.Theme = "light";
        service.Save();

        // Create second service and load
        var service2 = new UserPreferencesService();
        service2.Load();
        
        Assert.Equal("light", service2.Theme);
    }
}


