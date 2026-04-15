namespace VaultApp.Core.Crypto;

/// <summary>
/// Abstraction for vault encryption/decryption operations.
/// Provides interface for implementing various encryption strategies.
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Encrypts plaintext string to encrypted bytes suitable for vault storage.
    /// </summary>
    /// <param name="plaintext">The plaintext content (typically JSON)</param>
    /// <param name="masterPassword">The master password for key derivation</param>
    /// <returns>Encrypted bytes with embedded salt, nonce, tag, and ciphertext</returns>
    byte[] Encrypt(string plaintext, string masterPassword);

    /// <summary>
    /// Decrypts vault file bytes back to plaintext string.
    /// </summary>
    /// <param name="fileBytes">The encrypted vault file bytes</param>
    /// <param name="masterPassword">The master password for key derivation</param>
    /// <returns>Decrypted plaintext content (typically JSON)</returns>
    /// <exception cref="InvalidVaultFileException">Thrown when file format is invalid</exception>
    /// <exception cref="CorruptedVaultDataException">Thrown when authentication tag fails (wrong password or corrupted data)</exception>
    string Decrypt(byte[] fileBytes, string masterPassword);
}
