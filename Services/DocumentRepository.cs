using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using Microsoft.Data.Sqlite;

namespace AvaloniaApp.Services;

/// <summary>
/// Репозиторий для сохранения и загрузки документов и их тел в SQLite.
/// </summary>
public static class DocumentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Сохраняет метаданные и тело документа. При совпадении doc_id обновляет существующую запись.
    /// </summary>
    /// <param name="metadata">Метаданные документа.</param>
    /// <param name="body">Структурированное тело документа.</param>
    /// <returns>Идентификатор сохранённого документа.</returns>
    public static Task<long> SaveAsync(DocumentMetadata metadata, DocumentBody body)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                using var transaction = connection.BeginTransaction();

                var now = DateTime.UtcNow.ToString("O");
                var documentId = UpsertDocument(connection, transaction, metadata, now);

                UpsertDocumentBody(connection, transaction, documentId, body);
                ReplaceChildren(connection, transaction, documentId, body);

                transaction.Commit();

                body.DocumentId = documentId;

                return documentId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DocumentRepository.SaveAsync] {ex}");
                throw;
            }
        });
    }

    /// <summary>
    /// Загружает документ по его коду.
    /// </summary>
    /// <param name="docId">Код документа.</param>
    /// <returns>Метаданные и тело документа или null, если не найден.</returns>
    public static Task<(DocumentMetadata Metadata, DocumentBody Body)?> LoadByDocIdAsync(string docId)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();

                long documentId;
                DocumentMetadata metadata;

                using (var command = new SqliteCommand(
                    @"SELECT id, doc_id, doc_type, title, domain, status, revision,
                              effective_date, author, owner, approver, approver_role,
                              aliases, tags, footer_text, created_at, updated_at
                       FROM documents WHERE doc_id = @docId",
                    connection))
                {
                    command.Parameters.AddWithValue("@docId", docId);
                    using var reader = command.ExecuteReader();
                    if (!reader.Read())
                    {
                        return null;
                    }

                    documentId = reader.GetInt64(0);
                    metadata = ReadMetadata(reader);
                }

                var body = LoadBody(connection, documentId);
                return ((DocumentMetadata Metadata, DocumentBody Body)?)(metadata, body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DocumentRepository.LoadByDocIdAsync] {ex}");
                throw;
            }
        });
    }

    /// <summary>
    /// Возвращает список всех кодов документов, отсортированных по убыванию даты обновления.
    /// </summary>
    /// <returns>Список кодов документов.</returns>
    public static Task<List<string>> ListAllDocIdsAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var list = new List<string>();
                using var connection = SqliteConnectionFactory.Open();
                using var command = new SqliteCommand(
                    "SELECT doc_id FROM documents ORDER BY updated_at DESC",
                    connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
                }

                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DocumentRepository.ListAllDocIdsAsync] {ex}");
                throw;
            }
        });
    }

    /// <summary>
    /// Возвращает краткие сводки всех документов, отсортированных по убыванию даты обновления.
    /// </summary>
    /// <returns>Список сводок документов.</returns>
    public static Task<IReadOnlyList<DocumentSummary>> ListAllSummariesAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var list = new List<DocumentSummary>();
                using var connection = SqliteConnectionFactory.Open();
                using var command = new SqliteCommand(
                    @"SELECT id, doc_id, title, doc_type, revision, status, effective_date, updated_at
                       FROM documents
                       ORDER BY updated_at DESC",
                    connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var id = reader.GetInt64(0);
                    var docId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var docType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                    var revision = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                    var status = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                    var effectiveDate = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                    var updatedAt = reader.IsDBNull(7)
                        ? DateTime.MinValue
                        : ParseDateTime(reader.GetString(7));

                    list.Add(new DocumentSummary(id, docId, title, docType, revision, status, effectiveDate, updatedAt));
                }

                return (IReadOnlyList<DocumentSummary>)list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DocumentRepository.ListAllSummariesAsync] {ex}");
                throw;
            }
        });
    }

    /// <summary>
    /// Удаляет документ и все связанные с ним строки каскадно.
    /// </summary>
    /// <param name="id">Идентификатор документа.</param>
    public static Task DeleteAsync(long id)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                using var command = new SqliteCommand("DELETE FROM documents WHERE id = @id", connection);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DocumentRepository.DeleteAsync] {ex}");
                throw;
            }
        });
    }

    private static long UpsertDocument(SqliteConnection connection, SqliteTransaction transaction, DocumentMetadata metadata, string now)
    {
        long? existingId = null;
        using (var command = new SqliteCommand("SELECT id FROM documents WHERE doc_id = @docId", connection, transaction))
        {
            command.Parameters.AddWithValue("@docId", metadata.DocId);
            var result = command.ExecuteScalar();
            if (result is not null && result != DBNull.Value)
            {
                existingId = Convert.ToInt64(result);
            }
        }

        if (existingId.HasValue)
        {
            using var command = new SqliteCommand(
                @"UPDATE documents SET
                    doc_id = @docId, doc_type = @docType, title = @title, domain = @domain,
                    status = @status, revision = @revision,
                    effective_date = @effectiveDate, author = @author, owner = @owner,
                    approver = @approver, approver_role = @approverRole, aliases = @aliases,
                    tags = @tags, footer_text = @footerText, updated_at = @updatedAt
                  WHERE id = @id",
                connection,
                transaction);

            AddDocumentParameters(command, metadata, now);
            command.Parameters.AddWithValue("@id", existingId.Value);
            command.ExecuteNonQuery();

            return existingId.Value;
        }
        else
        {
            using var command = new SqliteCommand(
                @"INSERT INTO documents (
                    doc_id, doc_type, title, domain, status, revision,
                    effective_date, author, owner, approver, approver_role, aliases, tags,
                    footer_text, created_at, updated_at
                  ) VALUES (
                    @docId, @docType, @title, @domain, @status, @revision,
                    @effectiveDate, @author, @owner, @approver, @approverRole, @aliases, @tags,
                    @footerText, @createdAt, @updatedAt
                  );
                  SELECT last_insert_rowid();",
                connection,
                transaction);

            AddDocumentParameters(command, metadata, now);
            command.Parameters.AddWithValue("@createdAt", now);
            return Convert.ToInt64(command.ExecuteScalar()!);
        }
    }

    private static void AddDocumentParameters(SqliteCommand command, DocumentMetadata metadata, string now)
    {
        command.Parameters.AddWithValue("@docId", metadata.DocId);
        command.Parameters.AddWithValue("@docType", metadata.DocType);
        command.Parameters.AddWithValue("@title", metadata.Title);
        command.Parameters.AddWithValue("@domain", string.IsNullOrWhiteSpace(metadata.DepartmentName) ? DBNull.Value : metadata.DepartmentName);
        command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(metadata.Status) ? DBNull.Value : metadata.Status);
        command.Parameters.AddWithValue("@revision", string.IsNullOrWhiteSpace(metadata.Revision) ? DBNull.Value : metadata.Revision);
        command.Parameters.AddWithValue("@effectiveDate", string.IsNullOrWhiteSpace(metadata.EffectiveDate) ? DBNull.Value : metadata.EffectiveDate);
        command.Parameters.AddWithValue("@author", string.IsNullOrWhiteSpace(metadata.Author) ? DBNull.Value : metadata.Author);
        command.Parameters.AddWithValue("@owner", string.IsNullOrWhiteSpace(metadata.Owner) ? DBNull.Value : metadata.Owner);
        command.Parameters.AddWithValue("@approver", string.IsNullOrWhiteSpace(metadata.Approver) ? DBNull.Value : metadata.Approver);
        command.Parameters.AddWithValue("@approverRole", string.IsNullOrWhiteSpace(metadata.ApproverRole) ? DBNull.Value : metadata.ApproverRole);
        command.Parameters.AddWithValue("@aliases", "[]");
        command.Parameters.AddWithValue("@tags", "[]");
        command.Parameters.AddWithValue("@footerText", string.IsNullOrWhiteSpace(metadata.FooterText) ? DBNull.Value : metadata.FooterText);
        command.Parameters.AddWithValue("@updatedAt", now);
    }

    private static void UpsertDocumentBody(SqliteConnection connection, SqliteTransaction transaction, long documentId, DocumentBody body)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO document_bodies (
                document_id, purpose, scope, primary_training, recurrent_training, parent_document
              ) VALUES (
                @documentId, @purpose, @scope, @primaryTraining, @recurrentTraining, @parentDocument
              )
              ON CONFLICT(document_id) DO UPDATE SET
                purpose = excluded.purpose,
                scope = excluded.scope,
                primary_training = excluded.primary_training,
                recurrent_training = excluded.recurrent_training,
                parent_document = excluded.parent_document",
            connection,
            transaction);

        command.Parameters.AddWithValue("@documentId", documentId);
        command.Parameters.AddWithValue("@purpose", body.Purpose);
        command.Parameters.AddWithValue("@scope", body.Scope);
        command.Parameters.AddWithValue("@primaryTraining", body.PrimaryTraining);
        command.Parameters.AddWithValue("@recurrentTraining", body.RecurrentTraining);
        command.Parameters.AddWithValue("@parentDocument", body.ParentDocument);
        command.ExecuteNonQuery();
    }

    private static void ReplaceChildren(SqliteConnection connection, SqliteTransaction transaction, long documentId, DocumentBody body)
    {
        DeleteChildren(connection, transaction, documentId);

        foreach (var term in body.Terms.OrderBy(t => t.Position))
        {
            using var command = new SqliteCommand(
                "INSERT INTO document_terms (document_id, position, term, definition) VALUES (@documentId, @position, @term, @definition)",
                connection,
                transaction);
            command.Parameters.AddWithValue("@documentId", documentId);
            command.Parameters.AddWithValue("@position", term.Position);
            command.Parameters.AddWithValue("@term", term.Term);
            command.Parameters.AddWithValue("@definition", term.Definition);
            command.ExecuteNonQuery();
        }

        foreach (var section in body.Sections.OrderBy(s => s.Position))
        {
            using var command = new SqliteCommand(
                "INSERT INTO document_sections (document_id, position, title, body) VALUES (@documentId, @position, @title, @body)",
                connection,
                transaction);
            command.Parameters.AddWithValue("@documentId", documentId);
            command.Parameters.AddWithValue("@position", section.Position);
            command.Parameters.AddWithValue("@title", section.Title);
            command.Parameters.AddWithValue("@body", section.Body);
            command.ExecuteNonQuery();
        }

        for (var i = 0; i < body.RelatedDocuments.Count; i++)
        {
            using var command = new SqliteCommand(
                "INSERT INTO document_related_docs (document_id, position, reference) VALUES (@documentId, @position, @reference)",
                connection,
                transaction);
            command.Parameters.AddWithValue("@documentId", documentId);
            command.Parameters.AddWithValue("@position", i);
            command.Parameters.AddWithValue("@reference", body.RelatedDocuments[i].Value);
            command.ExecuteNonQuery();
        }

        foreach (var row in body.ChangeLog.OrderBy(c => c.Position))
        {
            using var command = new SqliteCommand(
                @"INSERT INTO document_change_log (
                    document_id, position, date, revision, author, reviewer, description
                  ) VALUES (
                    @documentId, @position, @date, @revision, @author, @reviewer, @description
                  )",
                connection,
                transaction);
            command.Parameters.AddWithValue("@documentId", documentId);
            command.Parameters.AddWithValue("@position", row.Position);
            command.Parameters.AddWithValue("@date", row.Date);
            command.Parameters.AddWithValue("@revision", row.Revision);
            command.Parameters.AddWithValue("@author", row.Author);
            command.Parameters.AddWithValue("@reviewer", row.Reviewer);
            command.Parameters.AddWithValue("@description", row.Description);
            command.ExecuteNonQuery();
        }
    }

    private static void DeleteChildren(SqliteConnection connection, SqliteTransaction transaction, long documentId)
    {
        var tables = new[]
        {
            "document_terms",
            "document_sections",
            "document_related_docs",
            "document_change_log",
        };

        foreach (var table in tables)
        {
            using var command = new SqliteCommand($"DELETE FROM {table} WHERE document_id = @documentId", connection, transaction);
            command.Parameters.AddWithValue("@documentId", documentId);
            command.ExecuteNonQuery();
        }
    }

    private static DocumentBody LoadBody(SqliteConnection connection, long documentId)
    {
        var body = new DocumentBody { DocumentId = documentId };

        using (var command = new SqliteCommand(
            "SELECT purpose, scope, primary_training, recurrent_training, parent_document FROM document_bodies WHERE document_id = @documentId",
            connection))
        {
            command.Parameters.AddWithValue("@documentId", documentId);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                body.Purpose = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                body.Scope = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                body.PrimaryTraining = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                body.RecurrentTraining = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                body.ParentDocument = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            }
        }

        using (var command = new SqliteCommand(
            "SELECT id, position, term, definition FROM document_terms WHERE document_id = @documentId ORDER BY position",
            connection))
        {
            command.Parameters.AddWithValue("@documentId", documentId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                body.Terms.Add(new TermPair
                {
                    Id = reader.GetInt64(0),
                    Position = reader.GetInt32(1),
                    Term = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Definition = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                });
            }
        }

        using (var command = new SqliteCommand(
            "SELECT id, position, title, body FROM document_sections WHERE document_id = @documentId ORDER BY position",
            connection))
        {
            command.Parameters.AddWithValue("@documentId", documentId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                body.Sections.Add(new BodySection
                {
                    Id = reader.GetInt64(0),
                    Position = reader.GetInt32(1),
                    Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Body = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                });
            }
        }

        using (var command = new SqliteCommand(
            "SELECT id, position, reference FROM document_related_docs WHERE document_id = @documentId ORDER BY position",
            connection))
        {
            command.Parameters.AddWithValue("@documentId", documentId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                body.RelatedDocuments.Add(new StringItem { Value = reader.IsDBNull(2) ? string.Empty : reader.GetString(2) });
            }
        }

        using (var command = new SqliteCommand(
            "SELECT id, position, date, revision, author, reviewer, description FROM document_change_log WHERE document_id = @documentId ORDER BY position",
            connection))
        {
            command.Parameters.AddWithValue("@documentId", documentId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                body.ChangeLog.Add(new ChangeLogRow
                {
                    Id = reader.GetInt64(0),
                    Position = reader.GetInt32(1),
                    Date = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Revision = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Author = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Reviewer = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Description = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                });
            }
        }

        return body;
    }

    private static DateTime ParseDateTime(string value)
    {
        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime;
        }

        return DateTime.MinValue;
    }

    private static DocumentMetadata ReadMetadata(SqliteDataReader reader)
    {
        return new DocumentMetadata
        {
            DocId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            DocType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Title = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            DepartmentName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Status = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Revision = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            EffectiveDate = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Author = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            Owner = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Approver = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            ApproverRole = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            FooterText = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
        };
    }

    private static string SerializeCollection(IEnumerable<string> values)
    {
        var list = values?.ToList() ?? new List<string>();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    private static ObservableCollection<string> DeserializeCollection(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ObservableCollection<string>();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return list is null
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(list);
        }
        catch (JsonException)
        {
            return new ObservableCollection<string>();
        }
    }
}
