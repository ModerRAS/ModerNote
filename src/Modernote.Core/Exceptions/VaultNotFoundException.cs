using System;

namespace Modernote.Core.Exceptions;

/// <summary>Thrown when a vault cannot be created or opened at the given path.</summary>
public sealed class VaultNotFoundException : Exception
{
    public string Path { get; }

    public VaultNotFoundException(string path) : base($"Vault not found at '{path}'")
    {
        Path = path;
    }
}
