using System;
using System.IO;
using Modernote.Core.Search;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Core.Tests;

public class ImportAndSearchTests : IDisposable
{
    private readonly string _vaultDir;
    private readonly string _srcDir;
    private readonly global::Modernote.Core.Vault.Vault _vault;

    public ImportAndSearchTests()
    {
        _vaultDir = Path.Combine(Path.GetTempPath(), "modernote-imp-" + Guid.NewGuid().ToString("N"));
        _srcDir = Path.Combine(Path.GetTempPath(), "modernote-src-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_vaultDir);
        Directory.CreateDirectory(_srcDir);
        _vault = global::Modernote.Core.Vault.Vault.OpenOrCreate(_vaultDir);
    }

    public void Dispose()
    {
        _vault.Dispose();
        try { Directory.Delete(_vaultDir, recursive: true); } catch { }
        try { Directory.Delete(_srcDir, recursive: true); } catch { }
    }

    // === T14 ===

    [Fact]
    public void ImportFile_BasicImageImport()
    {
        var src = Path.Combine(_srcDir, "photo.jpg");
        File.WriteAllText(src, "fake-jpg");
        var obj = _vault.ImportFile(src, null);
        Assert.StartsWith("assets/", obj.LogicalPath);
        Assert.True(File.Exists(Path.Combine(_vault.AssetsPath, "photo.jpg")));
    }

    [Fact]
    public void ImportFile_WithFolder_Subfolder()
    {
        var src = Path.Combine(_srcDir, "doc.pdf");
        File.WriteAllText(src, "fake");
        var obj = _vault.ImportFile(src, "papers");
        Assert.Contains("papers", obj.LogicalPath);
        Assert.True(Directory.Exists(Path.Combine(_vault.AssetsPath, "papers")));
    }

    [Fact]
    public void ImportFile_Duplicate_AutoNumbers()
    {
        var src = Path.Combine(_srcDir, "data.png");
        File.WriteAllBytes(src, new byte[] { 1, 2, 3 });
        // First import
        _vault.ImportFile(src, null);
        // Copy to a new source path and import again
        var src2 = Path.Combine(_srcDir, "data2.png");
        File.Copy(src, src2);
        var obj2 = _vault.ImportFile(src2, null);
        Assert.NotEqual("assets/data.png", obj2.LogicalPath);
    }

    [Fact]
    public void ImportFile_NonExistent_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _vault.ImportFile(Path.Combine(_srcDir, "nope.txt"), null));
    }

    // === T15 ===

    [Fact]
    public void Search_EmptyQuery_NoResults()
    {
        var svc = new SearchService(_vault.Connection);
        Assert.Empty(svc.Search(""));
    }

    [Fact]
    public void Search_Fts_FindsByTitle()
    {
        IndexTestDocument("Samsung SSD Review", "The 980 PRO is fast");
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("Samsung");
        Assert.Single(results);
    }

    [Fact]
    public void Search_Fts_FindsByBody()
    {
        IndexTestDocument("Test Note", "This document discusses quantum mechanics");
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("quantum");
        Assert.Single(results);
    }

    [Fact]
    public void Search_LikeFallback_AfterFtsEmpty()
    {
        // Insert ONLY in object_text (not in object_fts) to simulate fallback
        var id = Guid.NewGuid();
        using (var cmd = _vault.Connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO objects (id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at, source_mtime_ms)
                VALUES ($id, 'text', 'notes/only.txt', 'only.txt', 'hash', 'text/plain', 100, '2025-01-01T00:00:00Z', '2025-01-01T00:00:00Z', 0)";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
            cmd.CommandText = @"
                INSERT INTO object_text (object_id, title, body, extractor, status)
                VALUES ($id, 'Only', 'fallback_term_xyz content', 'plain_text', 'ok')";
            cmd.ExecuteNonQuery();
        }
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("fallback_term_xyz");
        Assert.Single(results);
    }

    [Fact]
    public void Search_RespectsLimit()
    {
        for (int i = 0; i < 10; i++)
            IndexTestDocument($"Doc {i}", "common keyword content");
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("common", 3);
        Assert.True(results.Count <= 3);
    }

    private void IndexTestDocument(string title, string body)
    {
        var id = Guid.NewGuid();
        using var cmd = _vault.Connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO objects (id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at, source_mtime_ms)
            VALUES ($id, 'text', $path, $name, 'hash', 'text/plain', 100, '2025-01-01T00:00:00Z', '2025-01-01T00:00:00Z', 0)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$path", $"notes/{title}.txt");
        cmd.Parameters.AddWithValue("$name", title);
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"
            INSERT INTO object_text (object_id, title, body, extractor, status)
            VALUES ($id, $title, $body, 'plain_text', 'ok')";
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$body", body);
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"
            INSERT INTO object_fts (object_id, title, body, path) VALUES ($id, $title, $body, $path)";
        cmd.ExecuteNonQuery();
    }
}
