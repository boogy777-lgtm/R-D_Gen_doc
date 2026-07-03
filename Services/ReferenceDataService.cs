using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Defaults;
using Microsoft.Data.Sqlite;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис справочников: типы документов и подразделения.
/// Реализует <see cref="IReferenceDataService"/> для DI.
/// Читает .md файлы из _справочники/типы_документов/ и _справочники/подразделения/.
/// Без кэша: каждый вызов перечитывает файлы с диска (hot-reload из Obsidian).
/// </summary>
/// <remarks>
/// <para>
/// Класс содержит как instance-методы (для DI через <see cref="IReferenceDataService"/>),
/// так и static-фасады (для обратной совместимости с тестами и Tools/SmokeTest.cs,
/// которые исторически вызывают <c>ReferenceDataService.GetDocTypesAsync()</c>).
/// </para>
/// <para>
/// Для production-кода (MainWindowViewModel, координаторы) — инжектируйте
/// <see cref="IReferenceDataService"/> через конструктор.
/// </para>
/// </remarks>
public sealed class ReferenceDataService : IReferenceDataService
{
    /// <summary>
    /// Singleton-инстанс для static-фасада. Создаётся лениво при первом обращении.
    /// Позволяет сохранить обратную совместимость со static-вызовами из тестов.
    /// </summary>
    private static readonly Lazy<ReferenceDataService> InstanceLazy =
        new(() => new ReferenceDataService());

    private readonly IDeserializer yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private string seedStatusMessage = string.Empty;

    /// <summary>
    /// Создаёт новый экземпляр сервиса (для DI).
    /// </summary>
    public ReferenceDataService()
    {
    }

    /// <inheritdoc />
    public string SeedStatusMessage => seedStatusMessage;

    /// <inheritdoc />
    public Task<IReadOnlyList<DocType>> GetDocTypesAsync()
    {
        return Task.Run<IReadOnlyList<DocType>>(() =>
        {
            var dir = Path.Combine(GetRefStoreDir(), "типы_документов");
            if (!Directory.Exists(dir))
            {
                return Array.Empty<DocType>();
            }

            var list = new List<DocType>();
            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
            {
                var props = ParseYamlFrontmatter(file);
                if (props == null)
                {
                    continue;
                }

                var code = GetString(props, "doc_type")
                           ?? Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                list.Add(new DocType
                {
                    Code = code,
                    LabelRu = GetString(props, "label") ?? code,
                    Description = GetString(props, "description") ?? string.Empty,
                    ApprovalReview = GetString(props, "approval_review") ?? string.Empty,
                    ApprovalApprove = GetString(props, "approval_approve") ?? string.Empty,
                });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            return list;
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Department>> GetDepartmentsAsync()
    {
        return Task.Run<IReadOnlyList<Department>>(() =>
        {
            var dir = Path.Combine(GetRefStoreDir(), "подразделения");
            if (!Directory.Exists(dir))
            {
                return Array.Empty<Department>();
            }

            var list = new List<Department>();
            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
            {
                var props = ParseYamlFrontmatter(file);
                if (props == null)
                {
                    continue;
                }

                var code = GetString(props, "department")
                           ?? Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var parent = GetString(props, "parent");
                list.Add(new Department
                {
                    Code = code,
                    NameRu = GetString(props, "label") ?? code,
                    ParentCode = string.IsNullOrEmpty(parent) ? null : parent,
                    IsRoot = string.IsNullOrEmpty(parent),
                });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
            return list;
        });
    }

    /// <inheritdoc />
    public async Task<DocType?> GetDocTypeAsync(string code)
    {
        var all = await GetDocTypesAsync().ConfigureAwait(false);
        return all.FirstOrDefault(d =>
            string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<Department?> GetDepartmentAsync(string code)
    {
        var all = await GetDepartmentsAsync().ConfigureAwait(false);
        return all.FirstOrDefault(d =>
            string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetApproverRolesAsync()
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var dir = Path.Combine(GetRefStoreDir(), "роли");
            if (!Directory.Exists(dir))
            {
                return Array.Empty<string>();
            }

            var roles = Directory.EnumerateFiles(dir, "*.md")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            return roles;
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetDisciplineCodesAsync()
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var dir = Path.Combine(GetRefStoreDir(), "дисциплины");
            IReadOnlyList<string> fromDisk;
            if (!Directory.Exists(dir))
            {
                fromDisk = Array.Empty<string>();
            }
            else
            {
                fromDisk = Directory.EnumerateFiles(dir, "*.md")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();
            }

            // Merge with built-in fallback, чтобы ComboBox «Дисциплина» всегда
            // содержал полный набор дисциплин, даже если папка на диске пуста
            // или содержит устаревшие значения.
            var merged = fromDisk
                .Concat(DefaultDisciplinesFull().Where(d => !fromDisk.Contains(d)))
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            return merged;
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<DocType> DefaultDocTypesFull() => ReferenceData.DocTypes;

    /// <inheritdoc />
    public IReadOnlyList<Department> DefaultDepartmentsFull() => ReferenceData.Departments;

    /// <inheritdoc />
    public IReadOnlyList<string> DefaultDisciplinesFull() => ReferenceData.Disciplines;

    /// <inheritdoc />
    public IReadOnlyList<string> Statuses => ReferenceData.Statuses;

    /// <inheritdoc />
    public IReadOnlyList<string> ApproverRoles => ReferenceData.ApproverRoles;

    /// <inheritdoc />
    public IReadOnlyList<string> Disciplines => ReferenceData.Disciplines;

    /// <inheritdoc />
    public string GetRefStoreDir()
    {
        var exeDir = Environment.ProcessPath != null
            ? Path.GetDirectoryName(Environment.ProcessPath)!
            : AppContext.BaseDirectory;
        return Path.Combine(exeDir, "docs", "_справочники");
    }

    /// <inheritdoc />
    public Task SeedIfEmptyAsync()
    {
        return Task.Run(() =>
        {
            using var connection = SqliteConnectionFactory.Open();
            using var transaction = connection.BeginTransaction();

            var seededDocTypes = 0;
            var seededDepartments = 0;

            try
            {
                // cleanup
                var sqlCleanDoc = "DELETE FROM doc_type_registry WHERE TRIM(code, '-') = '' OR TRIM(label_ru, '-') = ''";
                using (var c = new SqliteCommand(sqlCleanDoc, connection, transaction))
                {
                    c.ExecuteNonQuery();
                }

                var sqlCleanOrg = "DELETE FROM org_structure WHERE TRIM(domain, '-') = '' OR TRIM(position, '-') = ''";
                using (var c = new SqliteCommand(sqlCleanOrg, connection, transaction))
                {
                    c.ExecuteNonQuery();
                }

                if (CountRows(connection, transaction, "doc_type_registry") == 0)
                {
                    foreach (var dt in DefaultDocTypesFull())
                    {
                        var sql = "INSERT OR IGNORE INTO doc_type_registry (code, label_ru, description) VALUES (@code, @label, @description)";
                        using var cmd = new SqliteCommand(sql, connection, transaction);
                        cmd.Parameters.AddWithValue("@code", dt.Code);
                        cmd.Parameters.AddWithValue("@label", dt.LabelRu);
                        cmd.Parameters.AddWithValue("@description", dt.Description);
                        cmd.ExecuteNonQuery();
                        seededDocTypes++;
                    }
                }

                if (CountRows(connection, transaction, "org_structure") == 0)
                {
                    foreach (var dep in DefaultDepartmentsFull())
                    {
                        var sql = "INSERT INTO org_structure (domain, is_root, position, comps) VALUES (@domain, @isRoot, @position, @comps)";
                        using var cmd = new SqliteCommand(sql, connection, transaction);
                        cmd.Parameters.AddWithValue("@domain", dep.Code);
                        cmd.Parameters.AddWithValue("@isRoot", dep.IsRoot ? 1 : 0);
                        cmd.Parameters.AddWithValue("@position", dep.NameRu);
                        cmd.Parameters.AddWithValue("@comps", dep.ParentCode ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                        seededDepartments++;
                    }
                }

                transaction.Commit();
                seedStatusMessage = seededDocTypes > 0 || seededDepartments > 0
                    ? $"Справочники загружены (типов: {seededDocTypes}, подразделений: {seededDepartments})."
                    : "Справочники уже заполнены.";

                // Всегда генерируем docs/_справочники/ (могли удалить папку вручную).
                try
                {
                    var exeDir = Environment.ProcessPath != null
                        ? Path.GetDirectoryName(Environment.ProcessPath)!
                        : AppContext.BaseDirectory;
                    DocumentsBaseGenerator.Generate(Path.Combine(exeDir, "docs"));
                }
                catch (Exception genEx)
                {
                    Debug.WriteLine($"[SeedIfEmptyAsync] .base generation failed: {genEx.Message}");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    /* no-op */
                }

                seedStatusMessage = $"Ошибка загрузки справочников: {ex.Message}";
                throw;
            }
        });
    }

    private int CountRows(SqliteConnection c, SqliteTransaction t, string table)
    {
        using var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {table}", c, t);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private Dictionary<string, object>? ParseYamlFrontmatter(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            var start = text.IndexOf("---", StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }

            var end = text.IndexOf("---", start + 3, StringComparison.Ordinal);
            if (end < 0)
            {
                return null;
            }

            var yaml = text.Substring(start + 3, end - start - 3).Trim();
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }

            return yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        }
        catch
        {
            return null;
        }
    }

    private string? GetString(Dictionary<string, object> props, string key)
    {
        return props.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    // === Static facade для обратной совместимости (тесты, SmokeTest) =====
    //
    // Сохраняем оригинальные имена static-методов как thin-facade над singleton-инстансом.
    // Production-код (MainWindowViewModel, координаторы) ДОЛЖЕН инжектировать
    // IReferenceDataService через конструктор — static-фасад только для backward compat.

    /// <summary>
    /// Gets singleton-инстанс сервиса. Используется static-фасадом ниже
    /// и для backward-compat с Tools/SmokeTest.cs, DocumentsBaseGenerator.
    /// </summary>
    public static ReferenceDataService Instance => InstanceLazy.Value;

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.SeedStatusMessage</c>.
    /// </summary>
    public static string StaticSeedStatusMessage => Instance.SeedStatusMessage;

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetDocTypesAsync()</c>.
    /// </summary>
    public static Task<IReadOnlyList<DocType>> StaticGetDocTypesAsync() => Instance.GetDocTypesAsync();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetDepartmentsAsync()</c>.
    /// </summary>
    public static Task<IReadOnlyList<Department>> StaticGetDepartmentsAsync() => Instance.GetDepartmentsAsync();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetDocTypeAsync(code)</c>.
    /// </summary>
    public static Task<DocType?> StaticGetDocTypeAsync(string code) => Instance.GetDocTypeAsync(code);

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetDepartmentAsync(code)</c>.
    /// </summary>
    public static Task<Department?> StaticGetDepartmentAsync(string code) => Instance.GetDepartmentAsync(code);

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetApproverRolesAsync()</c>.
    /// </summary>
    public static Task<IReadOnlyList<string>> StaticGetApproverRolesAsync() => Instance.GetApproverRolesAsync();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetDisciplineCodesAsync()</c>.
    /// </summary>
    public static Task<IReadOnlyList<string>> StaticGetDisciplineCodesAsync() => Instance.GetDisciplineCodesAsync();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.SeedIfEmptyAsync()</c>.
    /// </summary>
    public static Task StaticSeedIfEmptyAsync() => Instance.SeedIfEmptyAsync();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.GetRefStoreDir()</c>.
    /// </summary>
    public static string StaticGetRefStoreDir() => Instance.GetRefStoreDir();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.DefaultDocTypesFull()</c>.
    /// </summary>
    public static IReadOnlyList<DocType> StaticDefaultDocTypesFull() => Instance.DefaultDocTypesFull();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.DefaultDepartmentsFull()</c>.
    /// </summary>
    public static IReadOnlyList<Department> StaticDefaultDepartmentsFull() => Instance.DefaultDepartmentsFull();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceDataService.DefaultDisciplinesFull()</c>.
    /// </summary>
    public static IReadOnlyList<string> StaticDefaultDisciplinesFull() => Instance.DefaultDisciplinesFull();

    /// <summary>
    /// Legacy static accessor: <c>ReferenceData.Statuses</c>.
    /// </summary>
    public static IReadOnlyList<string> StaticStatuses => Instance.Statuses;

    /// <summary>
    /// Legacy static accessor: <c>ReferenceData.ApproverRoles</c>.
    /// </summary>
    public static IReadOnlyList<string> StaticApproverRoles => Instance.ApproverRoles;

    /// <summary>
    /// Legacy static accessor: <c>ReferenceData.Disciplines</c>.
    /// </summary>
    public static IReadOnlyList<string> StaticDisciplines => Instance.Disciplines;
}