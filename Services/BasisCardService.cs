using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using Microsoft.Data.Sqlite;

namespace AvaloniaApp.Services;

/// <summary>
/// CRUD-операции с карточками оснований.
/// </summary>
public static class BasisCardService
{
    private static readonly string[] RequiredColumns =
    {
        "doc_type_code",
        "domain_code",
        "proposed_doc_id",
        "title",
        "author",
        "owner",
        "status",
        "sealed",
    };

    public static Task<long> CreateAsync(BasisCard card)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                EnsureSchema(connection);

                using var command = new SqliteCommand(
                    @"INSERT INTO basis_cards (
                        doc_type_code, domain_code, proposed_doc_id, linked_doc_id,
                        title, author, owner, status, sealed
                      ) VALUES (
                        @docTypeCode, @domainCode, @proposedDocId, @linkedDocId,
                        @title, @author, @owner, @status, @sealed
                      );
                      SELECT last_insert_rowid();",
                    connection);

                AddParameters(command, card);
                return Convert.ToInt64(command.ExecuteScalar());
            }
            catch
            {
                return -1;
            }
        });
    }

    public static Task<BasisCard?> GetAsync(long id)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                EnsureSchema(connection);

                using var command = new SqliteCommand(
                    @"SELECT id, created_at, doc_type_code, domain_code, proposed_doc_id,
                             linked_doc_id, title, author, owner, status, sealed
                      FROM basis_cards WHERE id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = command.ExecuteReader();
                return reader.Read() ? ReadCard(reader) : null;
            }
            catch
            {
                return null;
            }
        });
    }

    public static Task<IReadOnlyList<BasisCard>> ListAsync(string? linkedDocId = null)
    {
        return Task.Run(() =>
        {
            var list = new List<BasisCard>();
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                EnsureSchema(connection);

                var sql = @"SELECT id, created_at, doc_type_code, domain_code, proposed_doc_id,
                                   linked_doc_id, title, author, owner, status, sealed
                            FROM basis_cards";
                using var command = new SqliteCommand(sql, connection);
                if (!string.IsNullOrWhiteSpace(linkedDocId))
                {
                    sql += " WHERE linked_doc_id = @linkedDocId";
                    command.CommandText = sql;
                    command.Parameters.AddWithValue("@linkedDocId", linkedDocId);
                }

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(ReadCard(reader)!);
                }
            }
            catch
            {
                // ignored
            }

            return (IReadOnlyList<BasisCard>)list;
        });
    }

    public static Task UpdateAsync(BasisCard card)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                EnsureSchema(connection);

                using var command = new SqliteCommand(
                    @"UPDATE basis_cards SET
                        doc_type_code = @docTypeCode,
                        domain_code = @domainCode,
                        proposed_doc_id = @proposedDocId,
                        linked_doc_id = @linkedDocId,
                        title = @title,
                        author = @author,
                        owner = @owner,
                        status = @status,
                        sealed = @sealed
                      WHERE id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", card.Id);
                AddParameters(command, card);
                command.ExecuteNonQuery();
            }
            catch
            {
                // ignored per user decision
            }
        });
    }

    public static Task DeleteAsync(long id)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = SqliteConnectionFactory.Open();
                using var command = new SqliteCommand("DELETE FROM basis_cards WHERE id = @id", connection);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
            catch
            {
                // ignored
            }
        });
    }

    public static Task<long> AutoCreateForTemplateAsync(
        string docTypeCode, string domainCode, string title, string author, string owner)
    {
        return Task.Run(async () =>
        {
            var proposed = DocumentNumberingService.ProposeDocId(docTypeCode, domainCode, 1);
            var card = new BasisCard
            {
                DocTypeCode = docTypeCode,
                DomainCode = domainCode,
                ProposedDocId = proposed,
                LinkedDocId = null,
                Title = title,
                Author = author,
                Owner = owner,
                Status = "черновик",
                Sealed = false,
            };

            return await CreateAsync(card).ConfigureAwait(false);
        });
    }

    private static void AddParameters(SqliteCommand command, BasisCard card)
    {
        command.Parameters.AddWithValue("@docTypeCode", card.DocTypeCode);
        command.Parameters.AddWithValue("@domainCode", card.DomainCode);
        command.Parameters.AddWithValue("@proposedDocId", card.ProposedDocId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@linkedDocId", card.LinkedDocId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@title", card.Title);
        command.Parameters.AddWithValue("@author", card.Author);
        command.Parameters.AddWithValue("@owner", card.Owner);
        command.Parameters.AddWithValue("@status", card.Status);
        command.Parameters.AddWithValue("@sealed", card.Sealed ? 1 : 0);
    }

    private static BasisCard ReadCard(SqliteDataReader reader)
    {
        return new BasisCard
        {
            Id = reader.GetInt64(0),
            CreatedAt = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
            DocTypeCode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            DomainCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            ProposedDocId = reader.IsDBNull(4) ? null : reader.GetString(4),
            LinkedDocId = reader.IsDBNull(5) ? null : reader.GetString(5),
            Title = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Author = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Owner = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            Status = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Sealed = !reader.IsDBNull(10) && reader.GetBoolean(10),
        };
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = new SqliteCommand("PRAGMA table_info(basis_cards)", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                existing.Add(reader.GetString(1));
            }
        }

        foreach (var column in RequiredColumns)
        {
            if (existing.Contains(column))
            {
                continue;
            }

            var type = column == "sealed" ? "INTEGER NOT NULL DEFAULT 0" : "TEXT";
            using var command = new SqliteCommand($"ALTER TABLE basis_cards ADD COLUMN {column} {type}", connection);
            command.ExecuteNonQuery();
        }
    }
}
