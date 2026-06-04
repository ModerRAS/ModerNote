using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Modernote.Core.Tests;

public class Fts5Tests
{
    [Fact]
    public void Fts5_VirtualTable_Creates()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();
        // No exception = pass
    }

    [Fact]
    public void Fts5_InsertAndMatch_FindsResults()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ($c)";
        cmd.Parameters.AddWithValue("$c", "Samsung SSD 980 PRO review");
        cmd.ExecuteNonQuery();

        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$c", "Western Digital HDD comparison");
        cmd.ExecuteNonQuery();

        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$c", "Crucial DDR5 memory");
        cmd.ExecuteNonQuery();

        // Search
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT rowid, content FROM test_fts WHERE test_fts MATCH $q";
        cmd.Parameters.AddWithValue("$q", "SSD");
        using var reader = cmd.ExecuteReader();
        var results = new System.Collections.Generic.List<(long RowId, string Content)>();
        while (reader.Read())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1)));
        }

        Assert.Single(results);
        Assert.Contains("SSD", results[0].Content);
    }

    [Fact]
    public void Fts5_MultiWordSearch_FindsAll()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ($c)";
        cmd.Parameters.AddWithValue("$c", "Samsung SSD");
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$c", "Western Digital");
        cmd.ExecuteNonQuery();

        // Search for two terms
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT count(*) FROM test_fts WHERE test_fts MATCH $q";
        cmd.Parameters.AddWithValue("$q", "Samsung OR Western");
        var count = (long)(cmd.ExecuteScalar() ?? 0L);
        Assert.Equal(2, count);
    }

    [Fact]
    public void Fts5_SnippetFunction_ReturnsHighlightedText()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ($c)";
        cmd.Parameters.AddWithValue("$c", "The quick brown fox jumps over the lazy dog");
        cmd.ExecuteNonQuery();

        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT snippet(test_fts, 0, '[', ']', '...', 8) FROM test_fts WHERE test_fts MATCH $q";
        cmd.Parameters.AddWithValue("$q", "fox");
        var snippet = (string)(cmd.ExecuteScalar() ?? "");
        Assert.Contains("[fox]", snippet);
    }
}
