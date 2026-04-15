using System.Text.Json;
using VaultApp.Core.Crypto;
using VaultApp.Core.Models;

namespace VaultApp.Core.Services;

/// <summary>
/// Handles vault file persistence with encryption/decryption.
/// </summary>
public class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly ICryptoService _cryptoService;

    public string VaultFilePath { get; }

    /// <summary>
    /// Initializes a new StorageService with dependency injection.
    /// </summary>
    /// <param name="vaultFilePath">Path to the vault file</param>
    /// <param name="cryptoService">Crypto service for encryption/decryption</param>
    public StorageService(string vaultFilePath, ICryptoService cryptoService)
    {
        VaultFilePath = vaultFilePath;
        _cryptoService = cryptoService;
    }

    public bool VaultExists() => File.Exists(VaultFilePath);

    /// <summary>
    /// Saves vault data to encrypted file with atomic swap.
    /// </summary>
    public void Save(VaultData data, string masterPassword)
    {
        var json       = JsonSerializer.Serialize(data, JsonOptions);
        var encrypted  = _cryptoService.Encrypt(json, masterPassword);

        var tmpPath = VaultFilePath + ".tmp";
        File.WriteAllBytes(tmpPath, encrypted);
        File.Move(tmpPath, VaultFilePath, overwrite: true);
    }

    /// <summary>
    /// Loads and decrypts vault data from file.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when vault file does not exist</exception>
    /// <exception cref="Crypto.Exceptions.CorruptedVaultDataException">Thrown when password is incorrect or file is corrupted</exception>
    public VaultData Load(string masterPassword)
    {
        var fileBytes = File.ReadAllBytes(VaultFilePath);
        var json      = _cryptoService.Decrypt(fileBytes, masterPassword);
        return JsonSerializer.Deserialize<VaultData>(json, JsonOptions)
               ?? throw new InvalidDataException("Vault is empty or invalid.");
    }
}
