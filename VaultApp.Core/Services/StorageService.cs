using System.Text.Json;
using VaultApp.Core.Crypto;
using VaultApp.Core.Models;

namespace VaultApp.Core.Services;

public class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public string VaultFilePath { get; }

    public StorageService(string vaultFilePath)
    {
        VaultFilePath = vaultFilePath;
    }

    public bool VaultExists() => File.Exists(VaultFilePath);

    public void Save(VaultData data, string masterPassword)
    {
        var json       = JsonSerializer.Serialize(data, JsonOptions);
        var encrypted  = CryptoService.Encrypt(json, masterPassword);

        // Escreve em arquivo temporário e faz swap atômico
        var tmpPath = VaultFilePath + ".tmp";
        File.WriteAllBytes(tmpPath, encrypted);
        File.Move(tmpPath, VaultFilePath, overwrite: true);
    }

    public VaultData Load(string masterPassword)
    {
        var fileBytes = File.ReadAllBytes(VaultFilePath);
        var json      = CryptoService.Decrypt(fileBytes, masterPassword);
        return JsonSerializer.Deserialize<VaultData>(json, JsonOptions)
               ?? throw new InvalidDataException("Vault vazio ou inválido.");
    }
}
