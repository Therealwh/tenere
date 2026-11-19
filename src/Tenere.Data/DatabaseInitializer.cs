using Microsoft.Data.Sqlite;

namespace Tenere.Data;

/// <summary>
/// Creates the Tenere SQLite schema. Safe to call repeatedly.
/// </summary>
public static class DatabaseInitializer
{
    private const string SchemaSql = """
        PRAGMA journal_mode=WAL;
        PRAGMA synchronous=NORMAL;
        PRAGMA foreign_keys=ON;
        PRAGMA recursive_triggers=ON;

        CREATE TABLE IF NOT EXISTS collections (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            name        TEXT NOT NULL,
            parent_id   INTEGER REFERENCES collections(id) ON DELETE SET NULL,
            color       TEXT,
            created_at  TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS clips (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            content_type  INTEGER NOT NULL,
            title         TEXT,
            plain_text    TEXT,
            html          TEXT,
            image_path    TEXT,
            source_app    TEXT,
            created_at    TEXT NOT NULL,
            pinned        INTEGER NOT NULL DEFAULT 0,
            collection_id INTEGER REFERENCES collections(id) ON DELETE SET NULL,
            sensitivity   TEXT NOT NULL DEFAULT 'normal',
            expires_at    TEXT,
            ocr_status    INTEGER NOT NULL DEFAULT 0,
            ocr_text      TEXT,
            content_hash  TEXT NOT NULL,
            tags          TEXT NOT NULL DEFAULT ''
        );

        CREATE TABLE IF NOT EXISTS clip_formats (
            clip_id INTEGER NOT NULL REFERENCES clips(id) ON DELETE CASCADE,
            format  TEXT NOT NULL,
            data    BLOB NOT NULL,
            PRIMARY KEY (clip_id, format)
        );

        CREATE TABLE IF NOT EXISTS tags (
            id    INTEGER PRIMARY KEY AUTOINCREMENT,
            name  TEXT NOT NULL UNIQUE,
            color TEXT
        );

        CREATE TABLE IF NOT EXISTS clip_tags (
            clip_id INTEGER NOT NULL REFERENCES clips(id) ON DELETE CASCADE,
            tag_id  INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
            PRIMARY KEY (clip_id, tag_id)
        );

        CREATE TABLE IF NOT EXISTS transform_chains (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            name        TEXT NOT NULL,
            description TEXT,
            created_at  TEXT NOT NULL,
            enabled     INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS chain_steps (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            chain_id   INTEGER NOT NULL REFERENCES transform_chains(id) ON DELETE CASCADE,
            step_order INTEGER NOT NULL,
            step_type  TEXT NOT NULL,
            config     TEXT,
            UNIQUE (chain_id, step_order)
        );

        CREATE TABLE IF NOT EXISTS app_rules (
            id        INTEGER PRIMARY KEY AUTOINCREMENT,
            app_name  TEXT NOT NULL,
            rule_type TEXT NOT NULL,
            value     TEXT,
            enabled   INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS excluded_apps (
            app_name TEXT PRIMARY KEY
        );

        CREATE TABLE IF NOT EXISTS ocr_languages (
            code    TEXT PRIMARY KEY,
            name    TEXT NOT NULL,
            enabled INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS settings (
            key   TEXT PRIMARY KEY,
            value TEXT
        );

        CREATE INDEX IF NOT EXISTS ix_clips_created_at   ON clips(created_at);
        CREATE INDEX IF NOT EXISTS ix_clips_content_hash ON clips(content_hash);
        CREATE INDEX IF NOT EXISTS ix_clips_collection_id ON clips(collection_id);
        CREATE INDEX IF NOT EXISTS ix_clips_pinned       ON clips(pinned);

        CREATE VIRTUAL TABLE IF NOT EXISTS clip_fts USING fts5(
            plain_text,
            ocr_text,
            title,
            source_app,
            tags,
            content='clips',
            content_rowid='id',
            tokenize='unicode61 remove_diacritics 2'
        );

        CREATE TRIGGER IF NOT EXISTS clips_ai AFTER INSERT ON clips BEGIN
            INSERT INTO clip_fts(rowid, plain_text, ocr_text, title, source_app, tags)
            VALUES (new.id, new.plain_text, new.ocr_text, new.title, new.source_app, new.tags);
        END;

        CREATE TRIGGER IF NOT EXISTS clips_ad AFTER DELETE ON clips BEGIN
            INSERT INTO clip_fts(clip_fts, rowid, plain_text, ocr_text, title, source_app, tags)
            VALUES ('delete', old.id, old.plain_text, old.ocr_text, old.title, old.source_app, old.tags);
        END;

        CREATE TRIGGER IF NOT EXISTS clips_au AFTER UPDATE ON clips BEGIN
            INSERT INTO clip_fts(clip_fts, rowid, plain_text, ocr_text, title, source_app, tags)
            VALUES ('delete', old.id, old.plain_text, old.ocr_text, old.title, old.source_app, old.tags);
            INSERT INTO clip_fts(rowid, plain_text, ocr_text, title, source_app, tags)
            VALUES (new.id, new.plain_text, new.ocr_text, new.title, new.source_app, new.tags);
        END;

        CREATE TRIGGER IF NOT EXISTS clip_tags_ai AFTER INSERT ON clip_tags BEGIN
            UPDATE clips SET tags = COALESCE(
                (SELECT group_concat(t.name, ' ')
                 FROM clip_tags ct JOIN tags t ON t.id = ct.tag_id
                 WHERE ct.clip_id = new.clip_id), '')
            WHERE id = new.clip_id;
        END;

        CREATE TRIGGER IF NOT EXISTS clip_tags_ad AFTER DELETE ON clip_tags BEGIN
            UPDATE clips SET tags = COALESCE(
                (SELECT group_concat(t.name, ' ')
                 FROM clip_tags ct JOIN tags t ON t.id = ct.tag_id
                 WHERE ct.clip_id = old.clip_id), '')
            WHERE id = old.clip_id;
        END;

        CREATE TRIGGER IF NOT EXISTS tags_au AFTER UPDATE OF name ON tags BEGIN
            UPDATE clips SET tags = COALESCE(
                (SELECT group_concat(t.name, ' ')
                 FROM clip_tags ct JOIN tags t ON t.id = ct.tag_id
                 WHERE ct.clip_id = clips.id), '')
            WHERE id IN (SELECT clip_id FROM clip_tags WHERE tag_id = new.id);
        END;
        """;

    internal static async Task InitializeAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(ct);
    }
}