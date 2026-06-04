using System;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Data.Sqlite;
using Modernote.Core.Exceptions;
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

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }
}
