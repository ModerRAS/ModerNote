namespace Modernote.Core.Vault;

internal static class SchemaConstants
{
    public const string CreateTablesSql = @"
        CREATE TABLE IF NOT EXISTS objects (
            id TEXT PRIMARY KEY,
            kind TEXT NOT NULL,
            logical_path TEXT NOT NULL UNIQUE,
            display_name TEXT NOT NULL,
            content_hash TEXT NOT NULL,
            mime TEXT NOT NULL,
            size_bytes INTEGER NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            source_mtime_ms INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS object_text (
            object_id TEXT PRIMARY KEY,
            title TEXT NOT NULL,
            body TEXT NOT NULL,
            extractor TEXT NOT NULL,
            status TEXT NOT NULL,
            error TEXT,
            FOREIGN KEY(object_id) REFERENCES objects(id) ON DELETE CASCADE
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS object_fts USING fts5(
            object_id UNINDEXED,
            title,
            body,
            path
        );

        CREATE TABLE IF NOT EXISTS tags (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS object_tags (
            object_id TEXT NOT NULL,
            tag_id TEXT NOT NULL,
            PRIMARY KEY(object_id, tag_id)
        );

        CREATE TABLE IF NOT EXISTS links (
            id TEXT PRIMARY KEY,
            from_object_id TEXT NOT NULL,
            to_object_id TEXT,
            link_kind TEXT NOT NULL,
            target TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_objects_kind ON objects(kind);
        CREATE INDEX IF NOT EXISTS idx_objects_updated ON objects(updated_at);
    ";
}
