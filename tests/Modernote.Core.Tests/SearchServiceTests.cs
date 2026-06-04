using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Modernote.Core.Search;
using Modernote.Protocol;
using Xunit;

namespace Modernote.Core.Tests;

public class SearchServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly global::Modernote.Core.Vault.Vault _vault;

    public SearchServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modernote-search-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _vault = global::Modernote.Core.Vault.Vault.OpenOrCreate(_tempDir);
    }

    public void Dispose()
    {
        _vault.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        var svc = new SearchService(_vault.Connection);
            Assert.Empty(svc.Search(""));
            Assert.Empty(svc.Search("   "));
    }

    [Fact]
    public void Search_FindsByTitle()
    {
        // Index a test document
        IndexDocument("Samsung SSD Review", "This is a review of the Samsung SSD 980 PRO.");
        IndexDocument("WD HDD", "Western Digital hard drive comparison");

        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("Samsung", 10);
        Assert.Single(results);
        Assert.Contains("Samsung", results[0].Snippet);
    }

    [Fact]
    public void Search_MultipleResults()
    {
        IndexDocument("Apple Phone", "iPhone review");
        IndexDocument("Apple Watch", "Watch review");
        IndexDocument("Samsung", "Galaxy phone");

        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("Apple", 10);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_NoResults_LikeFallbackAlsoEmpty()
    {
        IndexDocument("Test", "Some content here");
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("nonexistent_term_xyz", 10);
        Assert.Empty(results);
    }

    [Fact]
    public void Search_LikeFallback_AfterFtsEmpty()
    {
        // Test LIKE fallback by directly inserting into object_text
        using (var cmd = _vault.Connection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO objects (id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at, source_mtime_ms)
                VALUES ('00000000-0000-0000-0000-000000000001', 'text', 'notes/test.txt', 'test.txt', 'hash', 'text/plain', 100, '2025-01-01T00:00:00Z', '2025-01-01T00:00:00Z', 0)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = @"
                INSERT INTO object_text (object_id, title, body, extractor, status)
                VALUES ('00000000-0000-0000-0000-000000000001', 'Manual Entry', 'contains fallback_term_xyz for testing', 'plain_text', 'ok')";
            cmd.ExecuteNonQuery();
        }

        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("fallback_term_xyz", 10);
        Assert.Single(results);
    }

    [Fact]
    public void Search_QuotesSpecialChars()
    {
        IndexDocument("Code C#", "C# is a programming language");
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("C#", 10);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Search_LimitRespected()
    {
        for (int i = 0; i < 5; i++)
            IndexDocument($"Test Doc {i}", "common keyword here");
        var svc = new SearchService(_vault.Connection);
        var results = svc.Search("common", 3);
        Assert.True(results.Count <= 3);
    }

    private void IndexDocument(string title, string body)
    {
        var id = Guid.NewGuid();
        using var cmd = _vault.Connection.CreateCommand();
        var logicalPath = "notes/" + Guid.NewGuid().ToString("N") + ".xml";
        cmd.CommandText = @"
            INSERT INTO objects (id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at, source_mtime_ms)
            VALUES ($id, 'xmlnote', $path, $title, 'hash', 'text/xml', 100, '2025-01-01T00:00:00Z', '2025-01-01T00:00:00Z', 0)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$path", logicalPath);
        cmd.Parameters.AddWithValue("$title", title);
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            INSERT INTO object_fts (object_id, title, body, path) VALUES ($id, $title, $body, $path)";
        cmd.Parameters.AddWithValue("$body", body);
        cmd.ExecuteNonQuery();
    }
}
