namespace VaultApp.Core.Crypto.Exceptions;

/// <summary>
/// Thrown when vault data authentication fails.
/// This can indicate either an incorrect master password or corrupted vault data.
/// </summary>
public class CorruptedVaultDataException : Exception
{
    public CorruptedVaultDataException(string message) : base(message) { }

    public CorruptedVaultDataException(string message, Exception innerException) 
        : base(message, innerException) { }
}

