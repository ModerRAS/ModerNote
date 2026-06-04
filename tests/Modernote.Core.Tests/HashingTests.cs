using System.IO;
using Modernote.Core.Import;
using Xunit;

namespace Modernote.Core.Tests;

public class HashingTests : System.IDisposable
{
    private readonly string _tempDir;

    public HashingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-hash-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void ComputeSha256_SameFile_SameHash()
    {
        var file = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(file, "hello world");
        var hash1 = HashCalculator.ComputeSha256(file);
        var hash2 = HashCalculator.ComputeSha256(file);
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 hex is 64 chars
    }

    [Fact]
    public void ComputeSha256_KnownContent_MatchesExpected()
    {
        var file = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(file, "hello world");
        var hash = HashCalculator.ComputeSha256(file);
        // SHA-256("hello world") = b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9
        Assert.StartsWith("b94d27b9", hash);
    }

    [Fact]
    public void ComputeSha256_DifferentContent_DifferentHash()
    {
        var file1 = Path.Combine(_tempDir, "a.txt");
        var file2 = Path.Combine(_tempDir, "b.txt");
        File.WriteAllText(file1, "aaa");
        File.WriteAllText(file2, "bbb");
        Assert.NotEqual(HashCalculator.ComputeSha256(file1), HashCalculator.ComputeSha256(file2));
    }
}
