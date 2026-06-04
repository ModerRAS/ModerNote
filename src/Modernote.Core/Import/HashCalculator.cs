using System;
using System.IO;
using System.Security.Cryptography;

namespace Modernote.Core.Import;

public static class HashCalculator
{
    /// <summary>Compute SHA-256 hash of a file as hex string.</summary>
    public static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
