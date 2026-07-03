using System;
using Microsoft.Data.Sqlite;

namespace AvaloniaApp.Services;

/// <summary>
/// Создание и миграция схемы SQLite для хранения документов и их тел.
/// </summary>
public static class DocumentSchema
{
    private const string DocumentsTableSql = @"
CREATE TABLE IF NOT EXISTS documents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    doc_id TEXT UNIQUE NOT NULL,
    doc_type TEXT NOT NULL,
    title TEXT NOT NULL,
    domain TEXT,
    status TEXT,
    revision TEXT,
    effective_date TEXT,
    author TEXT,
    owner TEXT,
    approver TEXT,
    approver_role TEXT,
    aliases TEXT,
    tags TEXT,
    version TEXT,
    footer_text TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);";

    private const string DocumentBodiesTableSql = @"
CREATE TABLE IF NOT EXISTS document_bodies (
    document_id INTEGER PRIMARY KEY REFERENCES documents(id) ON DELETE CASCADE,
    purpose TEXT NOT NULL DEFAULT '',
    scope TEXT NOT NULL DEFAULT '',
    primary_training TEXT NOT NULL DEFAULT '',
    recurrent_training TEXT NOT NULL DEFAULT '',
    parent_document TEXT NOT NULL DEFAULT ''
);";

    private const string DocumentTermsTableSql = @"
CREATE TABLE IF NOT EXISTS document_terms (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    position INTEGER NOT NULL,
    term TEXT NOT NULL,
    definition TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_document_terms_doc ON document_terms(document_id, position);";

    private const string DocumentSectionsTableSql = @"
CREATE TABLE IF NOT EXISTS document_sections (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    position INTEGER NOT NULL,
    title TEXT NOT NULL,
    body TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_document_sections_doc ON document_sections(document_id, position);";

    private const string DocumentRelatedDocsTableSql = @"
CREATE TABLE IF NOT EXISTS document_related_docs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    position INTEGER NOT NULL,
    reference TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_document_related_docs_doc ON document_related_docs(document_id, position);";

    private const string DocumentChangeLogTableSql = @"
CREATE TABLE IF NOT EXISTS document_change_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    position INTEGER NOT NULL,
    date TEXT NOT NULL,
    revision TEXT NOT NULL DEFAULT '',
    author TEXT NOT NULL DEFAULT '',
    reviewer TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT ''
);
CREATE INDEX IF NOT EXISTS idx_document_change_log_doc ON document_change_log(document_id, position);";

    private const string ReferenceTablesSql = @"
CREATE TABLE IF NOT EXISTS doc_type_registry (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    label_ru TEXT NOT NULL,
    description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS org_structure (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    domain TEXT NOT NULL UNIQUE,
    is_root INTEGER NOT NULL DEFAULT 0,
    position TEXT NOT NULL DEFAULT '',
    comps TEXT,
    parent_code TEXT
);";

    /// <summary>
    /// Создаёт таблицы документов, если они ещё не существуют.
    /// Выполняет идемпотентные миграции для добавления колонок, добавленных после v1.0.
    /// </summary>
    public static void EnsureCreated()
    {
        using var connection = SqliteConnectionFactory.Open();
        EnsureCreated(connection);
    }

    /// <summary>
    /// Идемпотентная инициализация схемы на уже открытом соединении.
    /// </summary>
    public static void EnsureCreated(SqliteConnection connection)
    {
        using (var command = new SqliteCommand(DocumentsTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new SqliteCommand(DocumentBodiesTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new SqliteCommand(DocumentTermsTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new SqliteCommand(DocumentSectionsTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new SqliteCommand(DocumentRelatedDocsTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new SqliteCommand(DocumentChangeLogTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        ApplyMigrations(connection);

        // Таблицы справочников (seed reference data).
        using (var command = new SqliteCommand(ReferenceTablesSql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Идемпотентные миграции для существующих БД.
    /// </summary>
    private static void ApplyMigrations(SqliteConnection connection)
    {
        // v1.x → footer_text (редактируемый нижний колонтитул).
        EnsureColumn(connection, "documents", "footer_text", "TEXT");
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string columnType)
    {
        using var check = new SqliteCommand(
            $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'",
            connection);
        var exists = Convert.ToInt64(check.ExecuteScalar()) > 0;
        if (exists)
        {
            return;
        }

        using var add = new SqliteCommand(
            $"ALTER TABLE {table} ADD COLUMN {column} {columnType}",
            connection);
        add.ExecuteNonQuery();
    }
}
