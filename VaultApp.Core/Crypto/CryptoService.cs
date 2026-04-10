using System.Security.Cryptography;
using System.Text;

namespace VaultApp.Core.Crypto;

/// <summary>
/// Responsável por toda criptografia do vault.
///
/// Formato do arquivo vault.dat:
/// [4 bytes]  magic number  → "VLT1"
/// [32 bytes] salt          → PBKDF2 salt
/// [12 bytes] nonce         → AES-GCM nonce
/// [16 bytes] tag           → AES-GCM authentication tag
/// [N bytes]  ciphertext    → JSON criptografado
/// </summary>
public static class CryptoService
{
    private const int SaltSize     = 32;
    private const int NonceSize    = 12;   // AES-GCM padrão
    private const int TagSize      = 16;   // 128-bit tag
    private const int KeySize      = 32;   // AES-256
    private const int Pbkdf2Iters  = 600_000; // OWASP 2023 recommendation
    private static readonly byte[] MagicBytes = "VLT1"u8.ToArray();

    // -------------------------------------------------------------------------
    // Pública: criptografa JSON → bytes do arquivo
    // -------------------------------------------------------------------------
    public static byte[] Encrypt(string plaintext, string masterPassword)
    {
        var salt  = RandomBytes(SaltSize);
        var nonce = RandomBytes(NonceSize);
        var key   = DeriveKey(masterPassword, salt);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext     = new byte[plaintextBytes.Length];
        var tag            = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Monta o arquivo: magic + salt + nonce + tag + ciphertext
        using var ms = new MemoryStream();
        ms.Write(MagicBytes);
        ms.Write(salt);
        ms.Write(nonce);
        ms.Write(tag);
        ms.Write(ciphertext);
        return ms.ToArray();
    }

    // -------------------------------------------------------------------------
    // Pública: bytes do arquivo → JSON decifrado
    // -------------------------------------------------------------------------
    public static string Decrypt(byte[] fileBytes, string masterPassword)
    {
        ValidateMagic(fileBytes);

        var offset     = MagicBytes.Length;
        var salt       = fileBytes[offset..(offset + SaltSize)];       offset += SaltSize;
        var nonce      = fileBytes[offset..(offset + NonceSize)];      offset += NonceSize;
        var tag        = fileBytes[offset..(offset + TagSize)];        offset += TagSize;
        var ciphertext = fileBytes[offset..];

        var key       = DeriveKey(masterPassword, salt);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);

        try
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException)
        {
            // Tag inválida = senha errada ou arquivo corrompido
            throw new UnauthorizedAccessException("Senha master incorreta ou arquivo corrompido.");
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------
    private static byte[] DeriveKey(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
               password:      password,
               salt:          salt,
               iterations:    Pbkdf2Iters,
               hashAlgorithm: HashAlgorithmName.SHA256,
               outputLength:  KeySize);

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    private static void ValidateMagic(byte[] fileBytes)
    {
        if (fileBytes.Length < MagicBytes.Length ||
            !fileBytes[..MagicBytes.Length].SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException("Arquivo não é um vault válido.");
        }
    }
}
