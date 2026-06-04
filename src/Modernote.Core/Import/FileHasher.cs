using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Modernote.Core.Import;

/// <summary>Computes SHA-256 hashes for files using streaming for large files.</summary>
public static class FileHasher
{
    private const int BufferSize = 8192;

    /// <summary>Compute SHA-256 of a file as lowercase hex string.</summary>
    public static string ComputeSha256(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Async version of ComputeSha256.</summary>
    public static async Task<string> ComputeSha256Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
