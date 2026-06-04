namespace Modernote.Core.Exceptions;

/// <summary>Thrown when the vault folder is invalid or inaccessible.</summary>
public sealed class VaultException : CoreException
{
    public VaultException(string message) : base(message) { }
    public VaultException(string message, Exception inner) : base(message, inner) { }
}
