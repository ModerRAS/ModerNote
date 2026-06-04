using System;
using System.IO;
using Xunit;

namespace Modernote.Core.Tests;

public class VaultScanTests : IDisposable
{
    private readonly string _tempDir;

    public VaultScanTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Scan_EmptyVault_ReturnsZero()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        var result = vault.Scan();
        Assert.Equal(0, result.ObjectsIndexed);
    }

    [Fact]
    public void Scan_WithFiles_IndexesAll()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        File.WriteAllText(Path.Combine(vault.NotesPath, "note1.xml"), "<document version='1'><p>test</p></document>");
        File.WriteAllText(Path.Combine(vault.NotesPath, "note2.xml"), "<document version='1'><p>test</p></document>");
        File.WriteAllText(Path.Combine(vault.AssetsPath, "image.png"), "fake-png-bytes");
        var result = vault.Scan();
        Assert.Equal(3, result.ObjectsIndexed);
    }

    [Fact]
    public void Scan_Idempotent_SameFileTwiceNoDuplicate()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        File.WriteAllText(Path.Combine(vault.NotesPath, "note.xml"), "<document version='1'><p>x</p></document>");
        vault.Scan();
        var result2 = vault.Scan();
        Assert.Equal(1, result2.ObjectsIndexed);
    }

    [Fact]
    public void Scan_IndexesNestedFiles()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        var subDir = Path.Combine(vault.NotesPath, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "deep.xml"), "<document version='1'><p>deep</p></document>");
        var result = vault.Scan();
        Assert.Equal(1, result.ObjectsIndexed);
    }

    [Fact]
    public void Scan_SetsCorrectMetadata()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        File.WriteAllText(Path.Combine(vault.AssetsPath, "test.pdf"), "fake-pdf");
        vault.Scan();
        using var cmd = vault.Connection.CreateCommand();
        cmd.CommandText = "SELECT kind, mime, display_name FROM objects WHERE logical_path LIKE 'assets/%'";
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("pdf", reader.GetString(0));
        Assert.Equal("application/pdf", reader.GetString(1));
        Assert.Equal("test.pdf", reader.GetString(2));
    }

    [Fact]
    public void Scan_ComputesHash()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        File.WriteAllText(Path.Combine(vault.AssetsPath, "h.txt"), "hash me");
        vault.Scan();
        using var cmd = vault.Connection.CreateCommand();
        cmd.CommandText = "SELECT content_hash FROM objects";
        var hash = (string)(cmd.ExecuteScalar() ?? "");
        Assert.Equal(64, hash.Length); // SHA-256 hex
    }
}
