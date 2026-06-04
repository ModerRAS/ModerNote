using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using Modernote.Core.Exceptions;
using Modernote.Core.Extraction;
using Modernote.Core.Import;
using Modernote.Protocol;

namespace Modernote.Core.Vault;

/// <summary>Represents an open vault. Provides access to notes, assets, and the index database.</summary>
public sealed class Vault : IDisposable
{
    public string Root { get; }
    private SqliteConnection? _connection;
    private bool _disposed;

    private Vault(string root)
    {
        Root = Path.GetFullPath(root);
    }

    public string NotesPath => Path.Combine(Root, "notes");
    public string AssetsPath => Path.Combine(Root, "assets");
    public string ModernotePath => Path.Combine(Root, ".modernote");
    public string DatabasePath => Path.Combine(ModernotePath, "index.sqlite");

    /// <summary>Open an existing vault or create a new one at the given path.</summary>
    public static Vault OpenOrCreate(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new VaultNotFoundException("(empty path)");

        var vault = new Vault(rootPath);
        vault.EnsureDirectoryStructure();
        vault.OpenDatabase();
        vault.EnsureSchema();
        return vault;
    }

    private void EnsureDirectoryStructure()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(NotesPath);
        Directory.CreateDirectory(AssetsPath);
        Directory.CreateDirectory(ModernotePath);
    }

    private void OpenDatabase()
    {
        var connStr = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
        _connection = new SqliteConnection(connStr);
        _connection.Open();

        // Enable WAL mode for better concurrency
        using var walCmd = _connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL";
        walCmd.ExecuteNonQuery();
    }

    /// <summary>Create the schema if it doesn't exist. Idempotent.</summary>
    private void EnsureSchema()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = SchemaConstants.CreateTablesSql;
        cmd.ExecuteNonQuery();
    }

    public SqliteConnection Connection
    {
        get
        {
            if (_disposed || _connection == null)
                throw new ObjectDisposedException(nameof(Vault));
            return _connection;
        }
    }

    /// <summary>Recursively scan notes/ and assets/ folders and index all files.</summary>
    public ScanSummary Scan()
    {
        int indexed = 0;
        foreach (var baseDir in new[] { NotesPath, AssetsPath })
        {
            if (!Directory.Exists(baseDir)) continue;
            foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    IndexFile(file);
                    indexed++;
                }
                catch
                {
                    // Skip files we can't read; continue scanning
                }
            }
        }
        return new ScanSummary(indexed);
    }

    private void IndexFile(string absolutePath)
    {
        var relative = Path.GetRelativePath(Root, absolutePath).Replace('\\', '/');
        var info = new FileInfo(absolutePath);
        var hash = FileHasher.ComputeSha256(absolutePath);
        var kind = MetadataExtractor.DetectKind(relative, absolutePath);
        var mime = MetadataExtractor.DetectMime(kind, absolutePath);
        var now = DateTimeOffset.UtcNow;
        var displayName = Path.GetFileName(absolutePath);
        var sizeBytes = info.Length;
        var mtimeMs = (long)(info.LastWriteTimeUtc - DateTimeOffset.UnixEpoch).TotalMilliseconds;

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO objects (id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at, source_mtime_ms)
            VALUES ($id, $kind, $path, $name, $hash, $mime, $size, $created, $updated, $mtime)
            ON CONFLICT(logical_path) DO UPDATE SET
                kind = excluded.kind,
                display_name = excluded.display_name,
                content_hash = excluded.content_hash,
                mime = excluded.mime,
                size_bytes = excluded.size_bytes,
                updated_at = excluded.updated_at,
                source_mtime_ms = excluded.source_mtime_ms
        ";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("$kind", kind.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("$path", relative);
        cmd.Parameters.AddWithValue("$name", displayName);
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$mime", mime);
        cmd.Parameters.AddWithValue("$size", sizeBytes);
        cmd.Parameters.AddWithValue("$created", now.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", now.ToString("O"));
        cmd.Parameters.AddWithValue("$mtime", mtimeMs);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Create a new XML note file and index it.</summary>
    public ObjectDto CreateXmlNote(string title, string? folder)
    {
        var safeName = SanitizeFilename(title);
        var filename = $"{safeName}.xml";
        var logicalPath = UniqueLogicalPath("notes", folder, filename);
        var absolutePath = Path.Combine(Root, logicalPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var initialXml = "<document version=\"1\"><h1>" + WebUtility.HtmlEncode(title) + "</h1></document>";
        File.WriteAllText(absolutePath, initialXml);
        var dto = UpsertObject(logicalPath);
        RefreshTextIndex(dto.Id);
        return dto;
    }

    /// <summary>Import a file (copy) into the vault's assets/ directory and index it.</summary>
    public ObjectDto ImportFile(string sourcePath, string? targetFolder)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file not found", sourcePath);

        var filename = Path.GetFileName(sourcePath);
        var logicalPath = UniqueLogicalPath("assets", targetFolder, filename);
        var absolutePath = Path.Combine(Root, logicalPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        File.Copy(sourcePath, absolutePath, overwrite: false);
        return UpsertObject(logicalPath);
    }

    /// <summary>Save XML content to an existing note.</summary>
    public ObjectDto SaveNoteXml(Guid objectId, string xml)
    {
        var obj = ResolveObject(objectId)
            ?? throw new ObjectNotFoundException(objectId);
        if (obj.Kind != ObjectKind.XmlNote)
            throw new VaultException($"Object {objectId} is not an XML note");
        var absolutePath = Path.Combine(Root, obj.LogicalPath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(absolutePath, xml);
        return UpsertObject(obj.LogicalPath);
    }

    /// <summary>Load an XML note's content.</summary>
    public (ObjectDto Object, string Xml) LoadNoteXml(Guid objectId)
    {
        var obj = ResolveObject(objectId)
            ?? throw new ObjectNotFoundException(objectId);
        if (obj.Kind != ObjectKind.XmlNote)
            throw new VaultException($"Object {objectId} is not an XML note");
        var absolutePath = Path.Combine(Root, obj.LogicalPath.Replace('/', Path.DirectorySeparatorChar));
        return (obj, File.ReadAllText(absolutePath));
    }

    /// <summary>Resolve an object by its GUID.</summary>
    public ObjectDto? ResolveObject(Guid objectId)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"SELECT id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at
                            FROM objects WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", objectId.ToString());
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadObject(reader);
    }

    /// <summary>List all indexed objects, ordered by logical path.</summary>
    public List<ObjectDto> ListObjects()
    {
        var list = new List<ObjectDto>();
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"SELECT id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at
                            FROM objects ORDER BY logical_path";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(ReadObject(reader));
        return list;
    }

    /// <summary>Upsert an object by its logical path.</summary>
    public ObjectDto UpsertObject(string logicalPath)
    {
        var absolutePath = Path.Combine(Root, logicalPath.Replace('/', Path.DirectorySeparatorChar));
        var hash = FileHasher.ComputeSha256(absolutePath);
        var kind = MetadataExtractor.DetectKind(logicalPath, absolutePath);
        var mime = MetadataExtractor.DetectMime(kind, absolutePath);
        var info = new FileInfo(absolutePath);
        var now = DateTimeOffset.UtcNow;

        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO objects (id, kind, logical_path, display_name, content_hash, mime, size_bytes, created_at, updated_at, source_mtime_ms)
            VALUES ($id, $kind, $path, $name, $hash, $mime, $size, $created, $updated, $mtime)
            ON CONFLICT(logical_path) DO UPDATE SET
                kind = excluded.kind,
                display_name = excluded.display_name,
                content_hash = excluded.content_hash,
                mime = excluded.mime,
                size_bytes = excluded.size_bytes,
                updated_at = excluded.updated_at,
                source_mtime_ms = excluded.source_mtime_ms
            RETURNING id";
        var id = Guid.NewGuid();
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$kind", kind.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("$path", logicalPath);
        cmd.Parameters.AddWithValue("$name", info.Name);
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$mime", mime);
        cmd.Parameters.AddWithValue("$size", info.Length);
        cmd.Parameters.AddWithValue("$created", now.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", now.ToString("O"));
        cmd.Parameters.AddWithValue("$mtime", (info.LastWriteTimeUtc - DateTimeOffset.UnixEpoch).TotalMilliseconds);
        var result = cmd.ExecuteScalar();
        if (result is string existingId && Guid.TryParse(existingId, out var existing))
            id = existing;
        return ResolveObject(id) ?? throw new VaultException("Upsert failed");
    }

    private string UniqueLogicalPath(string baseName, string? folder, string filename)
    {
        var folderParts = (folder ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        var basePath = string.IsNullOrEmpty(folder)
            ? $"{baseName}/{filename}"
            : $"{baseName}/{string.Join('/', folderParts)}/{filename}";
        if (!File.Exists(Path.Combine(Root, basePath.Replace('/', Path.DirectorySeparatorChar))))
            return basePath;
        var stem = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename);
        for (int i = 2; ; i++)
        {
            var candidate = string.IsNullOrEmpty(folder)
                ? $"{baseName}/{stem} ({i}){ext}"
                : $"{baseName}/{string.Join('/', folderParts)}/{stem} ({i}){ext}";
            if (!File.Exists(Path.Combine(Root, candidate.Replace('/', Path.DirectorySeparatorChar))))
                return candidate;
        }
    }

    private static string SanitizeFilename(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(value.Select(c => invalid.Contains(c) || c == ':' || c == '"' ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Untitled" : clean.Trim();
    }

    private static ObjectDto ReadObject(Microsoft.Data.Sqlite.SqliteDataReader r)
    {
        var id = Guid.Parse(r.GetString(0));
        var kind = Enum.Parse<ObjectKind>(r.GetString(1), ignoreCase: true);
        var logicalPath = r.GetString(2);
        var displayName = r.GetString(3);
        var hash = r.GetString(4);
        var mime = r.GetString(5);
        var size = r.GetInt64(6);
        var created = DateTimeOffset.Parse(r.GetString(7));
        var updated = DateTimeOffset.Parse(r.GetString(8));
        return new ObjectDto(id, kind, logicalPath, displayName, hash, mime, size, created, updated);
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    // === T17: Tags CRUD ===

    public TagDto AddTag(Guid objectId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new VaultException("Tag name cannot be empty");
        var tagId = FindOrCreateTag(name);
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO object_tags (object_id, tag_id) VALUES ($oid, $tid)";
        cmd.Parameters.AddWithValue("$oid", objectId.ToString());
        cmd.Parameters.AddWithValue("$tid", tagId.ToString());
        cmd.ExecuteNonQuery();
        return new TagDto(tagId, name);
    }

    public void RemoveTag(Guid objectId, string name)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM object_tags WHERE object_id = $oid AND tag_id IN
            (SELECT id FROM tags WHERE name = $name)";
        cmd.Parameters.AddWithValue("$oid", objectId.ToString());
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public List<TagDto> GetTags(Guid objectId)
    {
        var list = new List<TagDto>();
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT t.id, t.name FROM tags t
            JOIN object_tags ot ON ot.tag_id = t.id
            WHERE ot.object_id = $oid
            ORDER BY t.name";
        cmd.Parameters.AddWithValue("$oid", objectId.ToString());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new TagDto(Guid.Parse(reader.GetString(0)), reader.GetString(1)));
        return list;
    }

    private Guid FindOrCreateTag(string name)
    {
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id FROM tags WHERE name = $n";
            cmd.Parameters.AddWithValue("$n", name);
            var existing = cmd.ExecuteScalar() as string;
            if (existing != null) return Guid.Parse(existing);
        }
        var id = Guid.NewGuid();
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO tags (id, name) VALUES ($id, $n) ON CONFLICT(name) DO NOTHING";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.Parameters.AddWithValue("$n", name);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id FROM tags WHERE name = $n";
            cmd.Parameters.AddWithValue("$n", name);
            return Guid.Parse((string)cmd.ExecuteScalar()!);
        }
    }

    // === T17: Links CRUD ===

    public LinkDto AddLink(Guid fromId, Guid? toId, string kind, string target)
    {
        var id = Guid.NewGuid();
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO links (id, from_object_id, to_object_id, link_kind, target)
            VALUES ($id, $from, $to, $k, $t)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$from", fromId.ToString());
        cmd.Parameters.AddWithValue("$to", (object?)toId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$k", kind);
        cmd.Parameters.AddWithValue("$t", target);
        cmd.ExecuteNonQuery();
        return new LinkDto(id, fromId, toId, kind, target);
    }

    public List<LinkDto> GetLinks(Guid objectId)
    {
        var list = new List<LinkDto>();
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = "SELECT id, from_object_id, to_object_id, link_kind, target FROM links WHERE from_object_id = $oid";
        cmd.Parameters.AddWithValue("$oid", objectId.ToString());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var toStr = reader.IsDBNull(2) ? null : reader.GetString(2);
            list.Add(new LinkDto(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                toStr != null ? Guid.Parse(toStr) : null,
                reader.GetString(3),
                reader.GetString(4)));
        }
        return list;
    }

    // === T18: Text Index Refresh ===

    public void RefreshTextIndex(Guid objectId)
    {
        var obj = ResolveObject(objectId);
        if (obj == null) throw new ObjectNotFoundException(objectId);

        var absolutePath = Path.Combine(Root, obj.LogicalPath.Replace('/', Path.DirectorySeparatorChar));
        var (body, extractor, error) = ExtractAndCache(obj, absolutePath);

        // Extract title from content for XmlNote, otherwise use filename
        string title;
        if (obj.Kind == ObjectKind.XmlNote && error == null)
        {
            title = ExtractTitleFromXml(absolutePath) ?? Path.GetFileNameWithoutExtension(obj.LogicalPath);
        }
        else
        {
            title = Path.GetFileNameWithoutExtension(obj.LogicalPath);
        }

        // Delete old entries
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM object_text WHERE object_id = $id";
            cmd.Parameters.AddWithValue("$id", objectId.ToString());
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DELETE FROM object_fts WHERE object_id = $id";
            cmd.ExecuteNonQuery();
        }

        // Insert new object_text entry
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = @"INSERT INTO object_text (object_id, title, body, extractor, status, error)
                VALUES ($id, $t, $b, $e, $s, $err)";
            cmd.Parameters.AddWithValue("$id", objectId.ToString());
            cmd.Parameters.AddWithValue("$t", title);
            cmd.Parameters.AddWithValue("$b", body);
            cmd.Parameters.AddWithValue("$e", extractor);
            cmd.Parameters.AddWithValue("$s", error == null ? "ok" : "failed");
            cmd.Parameters.AddWithValue("$err", (object?)error ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // Insert new object_fts entry
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = @"INSERT INTO object_fts (object_id, title, body, path) VALUES ($id, $t, $b, $p)";
            cmd.Parameters.AddWithValue("$id", objectId.ToString());
            cmd.Parameters.AddWithValue("$t", title);
            cmd.Parameters.AddWithValue("$b", body);
            cmd.Parameters.AddWithValue("$p", obj.LogicalPath);
            cmd.ExecuteNonQuery();
        }
    }

    private (string body, string extractor, string? error) ExtractAndCache(ObjectDto obj, string path)
    {
        if (!File.Exists(path))
            return (string.Empty, "none", "File not found");
        var result = TextExtractor.Extract(obj.Kind, path);
        return (result.Body, result.Extractor, result.Error);
    }

    private static string? ExtractTitleFromXml(string path)
    {
        try
        {
            var doc = XDocument.Load(path);
            var h1 = doc.Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "h1", StringComparison.OrdinalIgnoreCase));
            return h1?.Value?.Trim();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }
}


