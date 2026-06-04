using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Modernote.Protocol;

namespace Modernote.Core.Search;

/// <summary>Search vault objects via FTS5 with LIKE fallback.</summary>
public sealed class SearchService
{
    private readonly SqliteConnection _connection;

    public SearchService(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Search objects via FTS5. Falls back to LIKE if no FTS hits.</summary>
    public List<SearchResultDto> Search(string query, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query) || limit <= 0)
            return new List<SearchResultDto>();

        var ftsResults = SearchFts(query, limit);
        if (ftsResults.Count > 0)
            return ftsResults;

        // Fallback to LIKE
        return SearchLike(query, limit);
    }

    private List<SearchResultDto> SearchFts(string query, int limit)
    {
        var results = new List<SearchResultDto>();
        var ftsQuery = QuoteFtsQuery(query);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT object_id, snippet(object_fts, 1, '<mark>', '</mark>', '...', 12)
            FROM object_fts
            WHERE object_fts MATCH $q
            LIMIT $lim";
        cmd.Parameters.AddWithValue("$q", ftsQuery);
        cmd.Parameters.AddWithValue("$lim", limit);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var snippet = reader.GetString(1);
            var obj = ResolveObject(id);
            if (obj != null)
                results.Add(new SearchResultDto(obj, snippet, 0.0));
        }
        return results;
    }

    private List<SearchResultDto> SearchLike(string query, int limit)
    {
        var results = new List<SearchResultDto>();
        var pattern = $"%{query}%";
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT object_id, body FROM object_text
            WHERE title LIKE $p OR body LIKE $p
            LIMIT $lim";
        cmd.Parameters.AddWithValue("$p", pattern);
        cmd.Parameters.AddWithValue("$lim", limit);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var body = reader.GetString(1);
            var obj = ResolveObject(id);
            if (obj != null)
            {
                var snippet = ExtractSnippet(body, query, 80);
                results.Add(new SearchResultDto(obj, snippet, 0.0));
            }
        }
        return results;
    }

    private static string QuoteFtsQuery(string query)
    {
        return string.Join(' ', query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => "\"" + t.Replace("\"", "\"\"") + "\""));
    }

    private static string ExtractSnippet(string body, string query, int maxLen)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        var idx = body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return body.Substring(0, Math.Min(maxLen, body.Length));
        var start = Math.Max(0, idx - 30);
        var end = Math.Min(body.Length, idx + query.Length + 50);
        return (start > 0 ? "..." : "") + body.Substring(start, end - start) + (end < body.Length ? "..." : "");
    }

    private ObjectDto? ResolveObject(Guid id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at FROM objects WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new ObjectDto(
            Guid.Parse(reader.GetString(0)),
            Enum.Parse<ObjectKind>(reader.GetString(1), ignoreCase: true),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetInt64(6),
            DateTimeOffset.Parse(reader.GetString(7)),
            DateTimeOffset.Parse(reader.GetString(8)));
    }
}
