using System;
using System.Collections.Generic;
using System.IO;
using Modernote.Core.Exceptions;
using Xunit;

namespace Modernote.Core.Tests;

public class VaultTests : IDisposable
{
    private readonly string _tempDir;

    public VaultTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // SQLite WAL can briefly lock the journal/shm files after dispose.
            // Swallow cleanup failures — the tests themselves passed.
        }
    }

    [Fact]
    public void OpenOrCreate_CreatesDirectoryStructure()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        Assert.True(Directory.Exists(vault.NotesPath));
        Assert.True(Directory.Exists(vault.AssetsPath));
        Assert.True(Directory.Exists(vault.ModernotePath));
        Assert.True(File.Exists(vault.DatabasePath));
    }

    [Fact]
    public void OpenOrCreate_EmptyPath_Throws()
    {
        Assert.Throws<VaultNotFoundException>(() => Modernote.Core.Vault.Vault.OpenOrCreate(""));
    }

    [Fact]
    public void OpenOrCreate_DoubleCall_IsIdempotent()
    {
        using var v1 = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        v1.Dispose();
        using var v2 = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        Assert.NotNull(v2);
    }

    [Fact]
    public void EnsureSchema_AllTablesExist()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        using var cmd = vault.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' OR type='view' ORDER BY name";
        var tables = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) tables.Add(reader.GetString(0));

        Assert.Contains("objects", tables);
        Assert.Contains("object_text", tables);
        Assert.Contains("object_fts", tables);
        Assert.Contains("tags", tables);
        Assert.Contains("object_tags", tables);
        Assert.Contains("links", tables);
    }

    [Fact]
    public void Connection_HasWalMode()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        using var cmd = vault.Connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode";
        var mode = (string)(cmd.ExecuteScalar() ?? "");
        Assert.Equal("wal", mode);
    }

    [Fact]
    public void Connection_HasForeignKeysEnabled()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        using var cmd = vault.Connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys";
        var enabled = (long)(cmd.ExecuteScalar() ?? 0L);
        Assert.Equal(1, enabled);
    }

    [Fact]
    public void OpenOrCreate_RootPath_IsAbsolute()
    {
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
        Assert.True(Path.IsPathRooted(vault.Root));
    }

    [Fact]
    public void OpenOrCreate_SubDirectory_Works()
    {
        var subdir = Path.Combine(_tempDir, "my-vault");
        using var vault = Modernote.Core.Vault.Vault.OpenOrCreate(subdir);
        Assert.True(File.Exists(Path.Combine(subdir, ".modernote", "index.sqlite")));
    }
}
