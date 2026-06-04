using System;
using System.IO;
using System.Threading.Tasks;
using Modernote.Core.Import;
using Xunit;

namespace Modernote.Core.Tests;

public class FileHasherTests
{
    [Fact]
    public void ComputeSha256_SameContent_SameHash()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hash-test-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "hello");
        try
        {
            var h1 = FileHasher.ComputeSha256(path);
            var h2 = FileHasher.ComputeSha256(path);
            Assert.Equal(h1, h2);
            // Known SHA-256 of "hello" (no newline)
            Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", h1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ComputeSha256_DifferentContent_DifferentHash()
    {
        var pathA = Path.Combine(Path.GetTempPath(), $"a-{Guid.NewGuid():N}.txt");
        var pathB = Path.Combine(Path.GetTempPath(), $"b-{Guid.NewGuid():N}.txt");
        File.WriteAllText(pathA, "alpha");
        File.WriteAllText(pathB, "beta");
        try
        {
            Assert.NotEqual(FileHasher.ComputeSha256(pathA), FileHasher.ComputeSha256(pathB));
        }
        finally
        {
            File.Delete(pathA);
            File.Delete(pathB);
        }
    }

    [Fact]
    public async Task ComputeSha256Async_ProducesSameHashAsSync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"async-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "async test content");
        try
        {
            var sync = FileHasher.ComputeSha256(path);
            var async = await FileHasher.ComputeSha256Async(path);
            Assert.Equal(sync, async);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ComputeSha256_NonExistentFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            FileHasher.ComputeSha256(Path.Combine(Path.GetTempPath(), "nonexistent.txt")));
    }

    [Fact]
    public void ComputeSha256_LargeFile_StreamsCorrectly()
    {
        // Create a 1MB file
        var path = Path.Combine(Path.GetTempPath(), $"large-{Guid.NewGuid():N}.bin");
        var data = new byte[1024 * 1024];
        new Random(42).NextBytes(data);
        File.WriteAllBytes(path, data);
        try
        {
            var hash = FileHasher.ComputeSha256(path);
            Assert.Equal(64, hash.Length); // SHA-256 hex is 64 chars
            Assert.Matches("^[0-9a-f]{64}$", hash);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
