using System.Security.Cryptography;
using System.Text;
using VaultApp.Core.Crypto.Exceptions;

namespace VaultApp.Core.Crypto;

/// <summary>
/// Implements AES-256-GCM encryption/decryption for vault files.
///
/// File Format:
/// [4 bytes]  magic number  → "VLT1"
/// [32 bytes] salt          → PBKDF2 salt
/// [12 bytes] nonce         → AES-GCM nonce
/// [16 bytes] tag           → AES-GCM authentication tag
/// [N bytes]  ciphertext    → Encrypted JSON content
/// </summary>
public class CryptoService : ICryptoService
{
    // ========================================================================
    // AES-GCM Parameters
    // ========================================================================
    private const int NonceSize    = 12;   // AES-GCM standard
    private const int TagSize      = 16;   // 128-bit authentication tag
    private const int KeySize      = 32;   // AES-256

    // ========================================================================
    // PBKDF2 Parameters
    // ========================================================================
    private const int SaltSize     = 32;
    private const int Pbkdf2Iters  = 600_000; // OWASP 2023 recommendation

    // ========================================================================
    // File Format Constants
    // ========================================================================
    private const int MinimumFileSize = 4 + 32 + 12 + 16; // magic + salt + nonce + tag (minimum)
    private static readonly byte[] MagicBytes = "VLT1"u8.ToArray();

    /// <summary>
    /// Encrypts plaintext to encrypted vault file bytes.
    /// </summary>
    /// <param name="plaintext">The plaintext content (typically JSON)</param>
    /// <param name="masterPassword">The master password for key derivation</param>
    /// <returns>Encrypted bytes with embedded salt, nonce, tag, and ciphertext</returns>
    public byte[] Encrypt(string plaintext, string masterPassword)
    {
        var salt  = RandomBytes(SaltSize);
        var nonce = RandomBytes(NonceSize);
        var key   = DeriveKey(masterPassword, salt);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext     = new byte[plaintextBytes.Length];
        var tag            = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return BuildVaultFile(salt, nonce, tag, ciphertext);
    }

    /// <summary>
    /// Decrypts vault file bytes back to plaintext.
    /// </summary>
    /// <param name="fileBytes">The encrypted vault file bytes</param>
    /// <param name="masterPassword">The master password for key derivation</param>
    /// <returns>Decrypted plaintext content (typically JSON)</returns>
    /// <exception cref="InvalidVaultFileException">Thrown when file format is invalid</exception>
    /// <exception cref="CorruptedVaultDataException">Thrown when authentication tag fails (wrong password or corrupted data)</exception>
    public string Decrypt(byte[] fileBytes, string masterPassword)
    {
        var (salt, nonce, tag, ciphertext) = ParseVaultFile(fileBytes);

        var key       = DeriveKey(masterPassword, salt);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);

        try
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new CorruptedVaultDataException(
                "Master password is incorrect or vault file is corrupted.", ex);
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    // ========================================================================
    // Private Helpers
    // ========================================================================

    /// <summary>
    /// Derives a cryptographic key from a password using PBKDF2-SHA256.
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
                password:      password,
                salt:          salt,
                iterations:    Pbkdf2Iters,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength:  KeySize);

    /// <summary>
    /// Generates cryptographically secure random bytes.
    /// </summary>
    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    /// <summary>
    /// Builds a complete vault file with magic bytes, salt, nonce, tag, and ciphertext.
    /// </summary>
    private static byte[] BuildVaultFile(byte[] salt, byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        using var ms = new MemoryStream();
        ms.Write(MagicBytes);
        ms.Write(salt);
        ms.Write(nonce);
        ms.Write(tag);
        ms.Write(ciphertext);
        return ms.ToArray();
    }

    /// <summary>
    /// Parses a vault file and extracts salt, nonce, tag, and ciphertext.
    /// </summary>
    /// <exception cref="InvalidVaultFileException">Thrown when file format is invalid or truncated</exception>
    private static (byte[] salt, byte[] nonce, byte[] tag, byte[] ciphertext) ParseVaultFile(byte[] fileBytes)
    {
        ValidateFileStructure(fileBytes);

        var offset = 0;
        
        offset += MagicBytes.Length;
        var salt       = fileBytes[offset..(offset + SaltSize)];
        
        offset += SaltSize;
        var nonce      = fileBytes[offset..(offset + NonceSize)];
        
        offset += NonceSize;
        var tag        = fileBytes[offset..(offset + TagSize)];
        
        offset += TagSize;
        var ciphertext = fileBytes[offset..];

        return (salt, nonce, tag, ciphertext);
    }

    /// <summary>
    /// Validates vault file structure before parsing.
    /// </summary>
    /// <exception cref="InvalidVaultFileException">Thrown when file is too small or has invalid magic bytes</exception>
    private static void ValidateFileStructure(byte[] fileBytes)
    {
        if (fileBytes.Length < MinimumFileSize)
        {
            throw new InvalidVaultFileException(
                $"Vault file is too small. Expected at least {MinimumFileSize} bytes, got {fileBytes.Length}.");
        }

        if (!fileBytes[..MagicBytes.Length].SequenceEqual(MagicBytes))
        {
            throw new InvalidVaultFileException(
                "File is not a valid vault. Invalid magic bytes.");
        }
    }
}
