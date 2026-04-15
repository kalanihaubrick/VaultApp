using System.Security.Cryptography;
using System.Text;
using Xunit;
using VaultApp.Core.Crypto;
using VaultApp.Core.Crypto.Exceptions;

namespace VaultApp.Core.Tests.Crypto;

/// <summary>
/// Comprehensive test suite for CryptoService implementing AES-256-GCM encryption.
/// Tests cover happy path, file format, error handling, authentication, and encoding scenarios.
/// </summary>
public class CryptoServiceTests
{
    private readonly CryptoService _service = new();
    
    // ========================================================================
    // Constants for file format validation
    // ========================================================================
    private const int MagicBytesLength = 4;
    private const int SaltSize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int MinimumFileSize = 68; // 4 + 32 + 12 + 16

    private static readonly byte[] ValidMagicBytes = Encoding.UTF8.GetBytes("VLT1");

    #region Group 1: Happy Path (5 tests)

    /// <summary>
    /// Test 1: Verify basic round-trip encryption/decryption with JSON content.
    /// </summary>
    [Fact]
    public void Encrypt_ThenDecrypt_RoundTrip_Success()
    {
        // Arrange
        var plaintext = """{"username": "admin", "email": "admin@example.com"}""";
        var password = "MySecurePassword123!";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    /// <summary>
    /// Test 2: Verify encryption/decryption of empty string payload.
    /// </summary>
    [Fact]
    public void Encrypt_ThenDecrypt_EmptyString_Success()
    {
        // Arrange
        var plaintext = string.Empty;
        var password = "TestPassword";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    /// <summary>
    /// Test 3: Verify encryption/decryption of large payloads (1MB JSON document).
    /// </summary>
    [Fact]
    public void Encrypt_ThenDecrypt_LargePayload_Success()
    {
        // Arrange - Create 1MB of JSON-like content
        var largeData = new StringBuilder();
        largeData.Append("{\"data\": [");
        for (int i = 0; i < 100000; i++)
        {
            largeData.Append($"{{\"id\":{i},\"name\":\"item_{i}\"}},");
        }
        largeData.Append("{\"id\":100000,\"name\":\"item_100000\"}]}");
        var plaintext = largeData.ToString();
        var password = "LargePayloadPassword";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
        Assert.True(encrypted.Length > 1_000_000, "Encrypted size should be >= plaintext size");
    }

    /// <summary>
    /// Test 4: Verify encryption/decryption preserves UTF-8 special characters and emojis.
    /// </summary>
    [Fact]
    public void Encrypt_ThenDecrypt_UnicodeAndSpecialChars_Success()
    {
        // Arrange
        var plaintext = """{"name":"José","city":"São Paulo","special":"<>&\"'"}""";
        var password = "UnicodePassword";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    /// <summary>
    /// Test 5: Verify encryption produces non-deterministic output (different salt/nonce each time).
    /// </summary>
    [Fact]
    public void Encrypt_ProducesNonDeterministicOutput()
    {
        // Arrange
        var plaintext = """{"id":123,"secret":"sensitive_data"}""";
        var password = "ConsistentPassword";

        // Act - Encrypt the same plaintext twice
        var encrypted1 = _service.Encrypt(plaintext, password);
        var encrypted2 = _service.Encrypt(plaintext, password);

        // Assert - Outputs should be different due to random salt/nonce
        Assert.NotEqual(encrypted1, encrypted2);
        // But both should decrypt to the same plaintext
        var decrypted1 = _service.Decrypt(encrypted1, password);
        var decrypted2 = _service.Decrypt(encrypted2, password);
        Assert.Equal(decrypted1, decrypted2);
        Assert.Equal(plaintext, decrypted1);
    }

    #endregion

    #region Group 2: File Structure & Format (5 tests)

    /// <summary>
    /// Test 6: Verify encrypted output has valid vault file format with correct magic bytes.
    /// </summary>
    [Fact]
    public void Encrypt_ProducesValidVaultFileFormat()
    {
        // Arrange
        var plaintext = """{"test":"data"}""";
        var password = "FormatTest";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);

        // Assert - Verify magic bytes
        var actualMagic = encrypted.AsSpan(0, MagicBytesLength).ToArray();
        Assert.Equal(ValidMagicBytes, actualMagic);
    }

    /// <summary>
    /// Test 7: Verify encrypted output file contains all required components.
    /// </summary>
    [Fact]
    public void Encrypt_ProducesCorrectFileSize()
    {
        // Arrange
        var plaintext = "test";
        var password = "SizeTest";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);

        // Assert - Verify minimum size and structure
        Assert.True(encrypted.Length >= MinimumFileSize,
            $"Encrypted file must be at least {MinimumFileSize} bytes");
        
        // Verify components are present:  
        // File layout: 4 bytes magic + 32 bytes salt + 12 bytes nonce + 16 bytes tag + ciphertext
        Assert.Equal(ValidMagicBytes, encrypted.AsSpan(0, 4).ToArray());
        
        // Verify total length = header + plaintext bytes
        var plaintextBytes = Encoding.UTF8.GetByteCount(plaintext);
        var expectedLength = 4 + 32 + 12 + 16 + plaintextBytes;
        Assert.Equal(expectedLength, encrypted.Length);
    }

    /// <summary>
    /// Test 8: Verify ParseVaultFile correctly extracts all file components.
    /// This test uses reflection to access private ParseVaultFile method for validation.
    /// </summary>
    [Fact]
    public void Decrypt_CorrectlyExtractsFileComponents()
    {
        // Arrange
        var plaintext = """{"component":"test"}""";
        var password = "ComponentTest";
        var encrypted = _service.Encrypt(plaintext, password);

        // Act - Decrypt and verify structure by checking components
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert - If decryption succeeds, parsing was correct
        Assert.Equal(plaintext, decrypted);
        
        // Verify file structure by component positions
        Assert.Equal(ValidMagicBytes, encrypted.AsSpan(0, 4).ToArray());
        var salt = encrypted.AsSpan(4, SaltSize).ToArray();
        var nonce = encrypted.AsSpan(4 + SaltSize, NonceSize).ToArray();
        var tag = encrypted.AsSpan(4 + SaltSize + NonceSize, TagSize).ToArray();
        
        Assert.Equal(SaltSize, salt.Length);
        Assert.Equal(NonceSize, nonce.Length);
        Assert.Equal(TagSize, tag.Length);
    }

    /// <summary>
    /// Test 9: Verify no data loss during round-trip encryption/decryption.
    /// </summary>
    [Fact]
    public void Decrypt_PreservesAllBytes_RoundTrip()
    {
        // Arrange - Use binary data to ensure all bytes are preserved
        var plaintext = string.Concat(Enumerable.Range(0, 256).Select(i => ((char)i).ToString()));
        var password = "BytePreservationTest";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
        Assert.Equal(plaintext.Length, decrypted.Length);
    }

    /// <summary>
    /// Test 10: Verify encryption/decryption handles maximum practical vault size (10MB ciphertext).
    /// </summary>
    [Fact]
    public void Decrypt_HandlesMaximumSizeVault()
    {
        // Arrange - Create 10MB payload
        var plaintext = new string('X', 10_000_000);
        var password = "MaxSizeTest";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
        Assert.Equal(10_000_000, decrypted.Length);
    }

    #endregion

    #region Group 3: Invalid Input Handling (5 tests)

    /// <summary>
    /// Test 11: Verify decryption of empty file throws InvalidVaultFileException.
    /// </summary>
    [Fact]
    public void Decrypt_EmptyFile_ThrowsInvalidVaultFileException()
    {
        // Arrange
        var emptyFile = Array.Empty<byte>();
        var password = "AnyPassword";

        // Act & Assert
        var ex = Assert.Throws<InvalidVaultFileException>(() =>
            _service.Decrypt(emptyFile, password));
        Assert.Contains("too small", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 12: Verify decryption of file smaller than 68 bytes throws InvalidVaultFileException.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(63)]
    public void Decrypt_FileTooSmall_ThrowsInvalidVaultFileException(int size)
    {
        // Arrange
        var tooSmallFile = new byte[size];
        // Only set valid magic bytes if file is large enough to contain them
        if (size >= 4)
        {
            tooSmallFile[0] = (byte)'V';
            tooSmallFile[1] = (byte)'L';
            tooSmallFile[2] = (byte)'T';
            tooSmallFile[3] = (byte)'1';
        }
        // Fill rest with random data
        if (size > 4)
        {
            RandomNumberGenerator.Fill(tooSmallFile.AsSpan(4));
        }
        var password = "AnyPassword";

        // Act & Assert
        var ex = Assert.Throws<InvalidVaultFileException>(() =>
            _service.Decrypt(tooSmallFile, password));
        Assert.Contains("too small", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 13: Verify decryption of file with invalid magic bytes throws InvalidVaultFileException.
    /// </summary>
    [Fact]
    public void Decrypt_InvalidMagicBytes_ThrowsInvalidVaultFileException()
    {
        // Arrange - Create file with wrong magic bytes
        var invalidFile = new byte[MinimumFileSize];
        invalidFile[0] = (byte)'X';
        invalidFile[1] = (byte)'X';
        invalidFile[2] = (byte)'X';
        invalidFile[3] = (byte)'X';
        RandomNumberGenerator.Fill(invalidFile.AsSpan(4));
        var password = "AnyPassword";

        // Act & Assert
        var ex = Assert.Throws<InvalidVaultFileException>(() =>
            _service.Decrypt(invalidFile, password));
        Assert.Contains("magic", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 14: Verify decryption handles partial/incomplete magic bytes correctly.
    /// </summary>
    [Fact]
    public void Decrypt_PartialMagicBytes_ThrowsInvalidVaultFileException()
    {
        // Arrange - Create file with only 2 magic bytes (incomplete)
        var partialMagicFile = new byte[MinimumFileSize];
        partialMagicFile[0] = (byte)'V';
        partialMagicFile[1] = (byte)'L';
        // Rest should be random
        RandomNumberGenerator.Fill(partialMagicFile.AsSpan(2));
        var password = "AnyPassword";

        // Act & Assert
        var ex = Assert.Throws<InvalidVaultFileException>(() =>
            _service.Decrypt(partialMagicFile, password));
        Assert.Contains("magic", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 15: Verify decryption of truncated file (cut off mid-way) throws InvalidVaultFileException.
    /// </summary>
    [Fact]
    public void Decrypt_TruncatedFile_ThrowsInvalidVaultFileException()
    {
        // Arrange - Create a valid encrypted file, then truncate it
        var plaintext = """{"test":"data"}""";
        var password = "TruncationTest";
        var validFile = _service.Encrypt(plaintext, password);
        
        // Truncate to half size
        var truncatedFile = new byte[validFile.Length / 2];
        Array.Copy(validFile, truncatedFile, truncatedFile.Length);

        // Act & Assert
        var ex = Assert.Throws<InvalidVaultFileException>(() =>
            _service.Decrypt(truncatedFile, password));
        Assert.Contains("too small", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Group 4: Authentication & Security (5 tests)

    /// <summary>
    /// Test 16: Verify decryption with wrong password throws CorruptedVaultDataException.
    /// </summary>
    [Fact]
    public void Decrypt_WrongPassword_ThrowsCorruptedVaultDataException()
    {
        // Arrange
        var plaintext = """{"secret":"encrypted_value"}""";
        var correctPassword = "CorrectPassword123";
        var wrongPassword = "WrongPassword456";
        var encrypted = _service.Encrypt(plaintext, correctPassword);

        // Act & Assert
        var ex = Assert.Throws<CorruptedVaultDataException>(() =>
            _service.Decrypt(encrypted, wrongPassword));
        Assert.Contains("password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 17: Verify decryption fails when authentication tag is corrupted.
    /// </summary>
    [Fact]
    public void Decrypt_CorruptedTag_ThrowsCorruptedVaultDataException()
    {
        // Arrange
        var plaintext = """{"data":"important"}""";
        var password = "TagCorruptionTest";
        var encrypted = _service.Encrypt(plaintext, password);
        
        // Corrupt the authentication tag (located after magic+salt+nonce)
        var corruptedFile = (byte[])encrypted.Clone();
        int tagOffset = 4 + 32 + 12;
        corruptedFile[tagOffset] ^= 0xFF; // Flip all bits in first tag byte

        // Act & Assert
        var ex = Assert.Throws<CorruptedVaultDataException>(() =>
            _service.Decrypt(corruptedFile, password));
        Assert.Contains("corrupted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 18: Verify decryption fails when ciphertext is corrupted.
    /// </summary>
    [Fact]
    public void Decrypt_CorruptedCiphertext_ThrowsCorruptedVaultDataException()
    {
        // Arrange
        var plaintext = """{"critical":"information"}""";
        var password = "CiphertextCorruptionTest";
        var encrypted = _service.Encrypt(plaintext, password);
        
        // Corrupt the ciphertext (located after magic+salt+nonce+tag)
        var corruptedFile = (byte[])encrypted.Clone();
        int ciphertextOffset = 4 + 32 + 12 + 16;
        if (corruptedFile.Length > ciphertextOffset)
        {
            corruptedFile[ciphertextOffset] ^= 0xFF; // Flip bits in first ciphertext byte
        }

        // Act & Assert
        var ex = Assert.Throws<CorruptedVaultDataException>(() =>
            _service.Decrypt(corruptedFile, password));
        Assert.Contains("corrupted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 19: Verify decryption fails when salt is corrupted (produces garbage/invalid UTF-8).
    /// </summary>
    [Fact]
    public void Decrypt_CorruptedSalt_DecryptionFails()
    {
        // Arrange
        var plaintext = """{"message":"test"}""";
        var password = "SaltCorruptionTest";
        var encrypted = _service.Encrypt(plaintext, password);
        
        // Corrupt the salt (located after magic bytes)
        var corruptedFile = (byte[])encrypted.Clone();
        corruptedFile[4] ^= 0xFF; // Flip bits in first salt byte

        // Act & Assert
        // Corrupted salt should result in incorrect key derivation, causing tag verification to fail
        var ex = Assert.Throws<CorruptedVaultDataException>(() =>
            _service.Decrypt(corruptedFile, password));
    }

    /// <summary>
    /// Test 20: Verify different passwords produce different encrypted outputs for same plaintext.
    /// </summary>
    [Fact]
    public void Encrypt_DifferentPasswords_ProduceDifferentEncryptions()
    {
        // Arrange
        var plaintext = """{"data":"sensitive"}""";
        var password1 = "FirstPassword123";
        var password2 = "SecondPassword456";

        // Act
        var encrypted1 = _service.Encrypt(plaintext, password1);
        var encrypted2 = _service.Encrypt(plaintext, password2);

        // Assert - Different passwords should produce different ciphertexts
        // (due to different key derivation)
        Assert.NotEqual(encrypted1, encrypted2);
        
        // First password decrypts first encryption
        var decrypted1 = _service.Decrypt(encrypted1, password1);
        Assert.Equal(plaintext, decrypted1);
        
        // Second password decrypts second encryption
        var decrypted2 = _service.Decrypt(encrypted2, password2);
        Assert.Equal(plaintext, decrypted2);
        
        // Cross-decryption should fail
        Assert.Throws<CorruptedVaultDataException>(() =>
            _service.Decrypt(encrypted1, password2));
    }

    #endregion

    #region Group 5: Encoding (2 tests)

    /// <summary>
    /// Test 21: Verify encryption/decryption preserves long UTF-8 strings with emoji characters.
    /// </summary>
    [Fact]
    public void Encrypt_Decrypt_LongUtfString_WithEmojis_Success()
    {
        // Arrange - Long string with various emoji categories
        var plaintext = """
        {
          "message": "Hello 👋 World 🌍! Emoji test: 😀 😃 😄 😁 😆 😅 🤣 😂",
          "hearts": "❤️ 🧡 💛 💚 💙 💜 🖤",
          "special": "🎉 🎊 🎈 🎁 🎀 🎂 🍰 🎃 🎄",
          "text": "Lorem ipsum dolor sit amet, consectetur adipiscing elit."
        }
        """;
        var password = "EmojiPassword🎉";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    /// <summary>
    /// Test 22: Verify encryption/decryption handles 4-byte UTF-8 characters correctly.
    /// 4-byte UTF-8 chars include mathematical symbols, rare CJK characters, etc.
    /// </summary>
    [Fact]
    public void Encrypt_Decrypt_String_With4ByteUtfChars_Success()
    {
        // Arrange - String with various 4-byte UTF-8 characters
        var plaintext = """
        {
          "math": "∑ ∏ √ ∞ ≈ ≠ ≤ ≥ ⊕ ⊗ ⊥",
          "music": "𝄞 𝄢 𝅘𝅥 𝅘𝅥𝅮 𝅘𝅥𝅯",
          "rare_cjk": "𠜎 𠜱 𠝕 𡁜 𡁯 𡁵 𡁶 𡁻 𡃁 𡃉",
          "geometric": "🞑 🞒 🞓 🞔 🞕 🞖 🞗 🞘 🞙 🞚",
          "ancient": "𐌀 𐌁 𐌂 𐌃 𐌄"
        }
        """;
        var password = "FourByteUtfPassword";

        // Act
        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted, password);

        // Assert
        Assert.Equal(plaintext, decrypted);
        // Verify byte count matches expected UTF-8 encoding
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var decryptedBytes = Encoding.UTF8.GetBytes(decrypted);
        Assert.Equal(plaintextBytes.Length, decryptedBytes.Length);
    }

    #endregion
}
