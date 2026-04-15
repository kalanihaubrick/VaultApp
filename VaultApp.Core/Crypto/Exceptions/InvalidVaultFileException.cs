namespace VaultApp.Core.Crypto.Exceptions;

/// <summary>
/// Thrown when a vault file has invalid format or structure.
/// </summary>
public class InvalidVaultFileException : Exception
{
    public InvalidVaultFileException(string message) : base(message) { }

    public InvalidVaultFileException(string message, Exception innerException) 
        : base(message, innerException) { }
}

