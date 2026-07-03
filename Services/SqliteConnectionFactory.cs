using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;

namespace AvaloniaApp.Services;

/// <summary>
/// Фабрика открытых подключений к SQLite-базе черновиков.
/// </summary>
public static class SqliteConnectionFactory
{
    public static SqliteConnection Open()
    {
        try
        {
            var connectionString = AppSettings.DraftsDb;
            EnsureDatabaseDirectoryExists(connectionString);

            var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Гарантируем схему при каждом открытии (CREATE TABLE IF NOT EXISTS — идемпотентно).
            DocumentSchema.EnsureCreated(connection);

            return connection;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SqliteConnectionFactory.Open] {ex}");
            throw new InvalidOperationException($"Не удалось открыть SQLite-базу: {ex.Message}", ex);
        }
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        const string prefix = "Data Source=";
        if (string.IsNullOrWhiteSpace(connectionString) ||
            !connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var path = connectionString.Substring(prefix.Length).Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
