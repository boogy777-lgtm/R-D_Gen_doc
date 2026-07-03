using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace AvaloniaApp.Services;

/// <summary>
/// Централизованный доступ к appsettings.json.
/// </summary>
public static class AppSettings
{
    static AppSettings()
    {
        // В single-file режиме AppContext.BaseDirectory = temp dir.
        // Используем путь к реальному .exe, чтобы БД лежала рядом с бинарником.
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath!)
                     ?? AppContext.BaseDirectory;

        var fallbackDraftsDb = $"Data Source={Path.Combine(exeDir, "template.db")}";
        var fallbackFactoryRoot = Path.Combine(exeDir, "docs", "_шаблоны");
        var fallbackSubdivisionsMd = Path.Combine(exeDir, "ref", "subdivisions.md");
        const string fallbackDefaultAuthor = "";

        try
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var baseDir = string.IsNullOrWhiteSpace(assemblyPath)
                ? exeDir
                : Path.GetDirectoryName(assemblyPath)!;
            var settingsPath = Path.Combine(baseDir, "appsettings.json");

            // В single-file режиме assembly.Location пуст — ищем рядом с .exe.
            if (!File.Exists(settingsPath))
            {
                settingsPath = Path.Combine(exeDir, "appsettings.json");
            }

            if (!File.Exists(settingsPath))
            {
                DraftsDb = fallbackDraftsDb;
                FactoryRoot = fallbackFactoryRoot;
                SubdivisionsMd = fallbackSubdivisionsMd;
                DefaultAuthor = fallbackDefaultAuthor;
                return;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            DraftsDb = ReadString(document.RootElement, "ConnectionStrings", "DraftsDb", fallbackDraftsDb);
            FactoryRoot = ReadString(document.RootElement, "FactoryPaths", "Root", fallbackFactoryRoot);
            SubdivisionsMd = ReadString(document.RootElement, "ReferenceData", "SubdivisionsMd", fallbackSubdivisionsMd);
            DefaultAuthor = ReadString(document.RootElement, "UserSettings", "DefaultAuthor", fallbackDefaultAuthor);
        }
        catch
        {
            DraftsDb = fallbackDraftsDb;
            FactoryRoot = fallbackFactoryRoot;
            SubdivisionsMd = fallbackSubdivisionsMd;
            DefaultAuthor = fallbackDefaultAuthor;
        }
    }

    public static string DraftsDb { get; }

    public static string FactoryRoot { get; }

    public static string SubdivisionsMd { get; }

    /// <summary>
    /// Gets имя автора по умолчанию для подстановки в Лист регистрации изменений.
    /// </summary>
    public static string DefaultAuthor { get; }

    private static string ReadString(JsonElement root, string section, string key, string fallback)
    {
        if (root.TryGetProperty(section, out var sectionElement) &&
            sectionElement.TryGetProperty(key, out var property))
        {
            var value = property.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return fallback;
    }
}
