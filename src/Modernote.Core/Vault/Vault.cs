using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Modernote.Core.Exceptions;

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

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }
}
