using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Modernote.Core.Tests;

public class Fts5AvailabilityTests
{
    [Fact]
    public void Fts5_VirtualTable_CanBeCreated()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        // Try to create an FTS5 virtual table
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('hello world')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('foo bar')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('searchable content')";
        cmd.ExecuteNonQuery();

        // Query with FTS5 MATCH
        cmd.CommandText = "SELECT content FROM test_fts WHERE test_fts MATCH 'hello'";
        using var reader = cmd.ExecuteReader();
        var results = new System.Collections.Generic.List<string>();
        while (reader.Read())
        {
            results.Add(reader.GetString(0));
        }

        Assert.Single(results);
        Assert.Equal("hello world", results[0]);
    }

    [Fact]
    public void Fts5_MultipleWords_AND_Search()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('apple banana cherry')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('apple date')";
        cmd.ExecuteNonQuery();

        // Both words must be present (AND search)
        cmd.CommandText = "SELECT content FROM test_fts WHERE test_fts MATCH 'apple banana'";
        using var reader = cmd.ExecuteReader();
        var results = new System.Collections.Generic.List<string>();
        while (reader.Read()) results.Add(reader.GetString(0));

        Assert.Single(results);
        Assert.Equal("apple banana cherry", results[0]);
    }

    [Fact]
    public void Fts5_ChineseText_Searchable()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        using var cmd = conn.CreateCommand();
        // unicode61 is the default FTS5 tokenizer
        cmd.CommandText = "CREATE VIRTUAL TABLE test_fts USING fts5(content)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('你好世界')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test_fts(content) VALUES ('再见')";
        cmd.ExecuteNonQuery();

        // FTS5 stores original content, so LIKE works on content column
        // Note: unicode61 tokenizer does not tokenize CJK into individual
        // tokens in the e_sqlite3 bundle; use LIKE for CJK search.
        cmd.CommandText = "SELECT content FROM test_fts WHERE test_fts MATCH '你*'";
        using var reader = cmd.ExecuteReader();
        var results = new System.Collections.Generic.List<string>();
        while (reader.Read()) results.Add(reader.GetString(0));

        Assert.Single(results);
        Assert.Equal("你好世界", results[0]);
    }
}
