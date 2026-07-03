using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Defaults;

namespace AvaloniaApp.Services;

/// <summary>
/// Генерирует Obsidian-совместимую структуру папок и .base views:
///   documents.base              — view над документами пользователя
///   _справочники/типы_документов.base — view над 34 .md типами
///   _справочники/подразделения.base   — view над 42 .md подразделениями
///   _справочники/{статусы,роли}.md + папки — простые enum
/// n+1: добавить запись = создать один .md в нужной папке.
/// </summary>
/// <remarks>
/// <para>
/// Зависит от <see cref="IReferenceDataService"/> (DIP). Для обратной
/// совместимости с историческими callsites (ReferenceDataService.SeedIfEmptyAsync,
/// Tools/SmokeTest.cs) сохранён static-фасад, делегирующий singleton-инстансу.
/// </para>
/// </remarks>
public static class DocumentsBaseGenerator
{
    /// <summary>
    /// Singleton-инстанс сервиса справочников. Используется для DI-вызовов.
    /// </summary>
    private static IReferenceDataService ReferenceData => ReferenceDataService.Instance;

    /// <summary>
    /// Генерирует Obsidian-совместимую структуру папок и .base views
    /// в указанной корневой папке <paramref name="docsDir"/>.
    /// </summary>
    public static void Generate(string docsDir)
    {
        Directory.CreateDirectory(docsDir);
        var refDir = Path.Combine(docsDir, "_справочники");
        Directory.CreateDirectory(refDir);

        // 1. Типы документов: 34 .md + 1 .base view
        EnsureDocTypeTable(refDir, ReferenceData.DefaultDocTypesFull());

        // 2. Подразделения: 42 .md + 1 .base view
        EnsureDepartmentTable(refDir, ReferenceData.DefaultDepartmentsFull());

        // 3. Простые справочники: статусы, роли
        // Источник: ReferenceData (через IReferenceDataService) — единая точка истины.
        EnsureSimpleEnumFiles(
            refDir,
            "статусы",
            "status",
            ReferenceData.Statuses.ToArray());

        EnsureSimpleEnumFiles(
            refDir,
            "роли",
            "approver_role",
            ReferenceData.ApproverRoles.ToArray());

        // 4. Дисциплины — группы документов по тематике
        // (НЕ путать с кодами подразделений: PRO/MNT/INS/HSE/ENV и т. п.)
        // Источник: ReferenceData.Disciplines — единая точка истины.
        EnsureSimpleEnumFiles(
            refDir,
            "дисциплины",
            "discipline",
            ReferenceData.Disciplines.ToArray());

        // 5. Шаблоны НЕ генерируются в Obsidian-папку (см. EnsureTemplates).
        // EnsureTemplates(docsDir);

        // 6. documents.base — главный view над пользовательскими .md
        GenerateDocumentsBase(docsDir);
    }

    // ──── Типы документов ────────────────────────────────────────────────
    private static void EnsureDocTypeTable(string refDir, IReadOnlyList<DocType> docTypes)
    {
        var folder = Path.Combine(refDir, "типы_документов");
        Directory.CreateDirectory(folder);

        // .md файлы (строки таблицы). Не перезаписываем существующие.
        foreach (var dt in docTypes)
        {
            var itemPath = Path.Combine(folder, SanitizeFileName(dt.Code) + ".md");
            if (File.Exists(itemPath))
            {
                continue;
            }

            var item = new StringBuilder();
            item.AppendLine("---");
            item.AppendLine($"doc_type: \"{dt.Code}\"");
            item.AppendLine($"label: \"{EscapeYaml(dt.LabelRu)}\"");
            item.AppendLine($"description: \"{EscapeYaml(dt.Description)}\"");
            item.AppendLine($"approval_review: \"{EscapeYaml(dt.ApprovalReview)}\"");
            item.AppendLine($"approval_approve: \"{EscapeYaml(dt.ApprovalApprove)}\"");
            item.AppendLine("---");
            item.AppendLine();
            item.AppendLine($"# {dt.LabelRu}");
            item.AppendLine();
            item.AppendLine(dt.Description);
            File.WriteAllText(itemPath, item.ToString().TrimEnd() + "\n", Encoding.UTF8);
        }

        // .base view (Obsidian-таблица). Всегда перезаписываем.
        var basePath = Path.Combine(refDir, "типы_документов.base");
        var yaml = new StringBuilder();
        yaml.AppendLine("# Типы документов ИСМ (BSD-RUS-BUR-QHSE-401)");
        yaml.AppendLine();
        yaml.AppendLine("filters:");
        yaml.AppendLine("  and:");
        yaml.AppendLine("    - 'file.ext == \"md\"'");
        yaml.AppendLine("    - file.folder.contains(\"_справочники/типы_документов\")");
        yaml.AppendLine();
        yaml.AppendLine("properties:");
        WriteProp(yaml, "doc_type", "Код");
        WriteProp(yaml, "label", "Тип документа");
        WriteProp(yaml, "description", "Описание");
        WriteProp(yaml, "approval_review", "Уровень согласования");
        WriteProp(yaml, "approval_approve", "Уровень утверждения");
        WriteProp(yaml, "file.mtime", "Изменён");
        yaml.AppendLine();
        yaml.AppendLine("views:");
        yaml.AppendLine("  - type: table");
        yaml.AppendLine("    name: \"Типы документов ИСМ\"");
        yaml.AppendLine("    order:");
        yaml.AppendLine("      - doc_type");
        yaml.AppendLine("      - label");
        yaml.AppendLine("      - description");
        yaml.AppendLine("      - approval_review");
        yaml.AppendLine("      - approval_approve");
        yaml.AppendLine();
        File.WriteAllText(basePath, yaml.ToString(), Encoding.UTF8);
    }

    // ──── Подразделения ──────────────────────────────────────────────────
    private static void EnsureDepartmentTable(string refDir, IReadOnlyList<Department> departments)
    {
        var folder = Path.Combine(refDir, "подразделения");
        Directory.CreateDirectory(folder);

        // .md файлы (строки таблицы)
        foreach (var dep in departments)
        {
            var itemPath = Path.Combine(folder, SanitizeFileName(dep.Code) + ".md");
            if (File.Exists(itemPath))
            {
                continue;
            }

            var item = new StringBuilder();
            item.AppendLine("---");
            item.AppendLine($"department: \"{dep.Code}\"");
            item.AppendLine($"label: \"{EscapeYaml(dep.NameRu)}\"");
            if (!string.IsNullOrEmpty(dep.ParentCode))
            {
                item.AppendLine($"parent: \"{dep.ParentCode}\"");
            }

            item.AppendLine($"is_root: {dep.IsRoot.ToString().ToLowerInvariant()}");
            item.AppendLine("---");
            item.AppendLine();
            item.AppendLine($"# {dep.NameRu}");
            File.WriteAllText(itemPath, item.ToString().TrimEnd() + "\n", Encoding.UTF8);
        }

        // .base view
        var basePath = Path.Combine(refDir, "подразделения.base");
        var yaml = new StringBuilder();
        yaml.AppendLine("# Подразделения БурСервис (WM-RUS-BUR-QHSE-401A)");
        yaml.AppendLine();
        yaml.AppendLine("filters:");
        yaml.AppendLine("  and:");
        yaml.AppendLine("    - 'file.ext == \"md\"'");
        yaml.AppendLine("    - file.folder.contains(\"_справочники/подразделения\")");
        yaml.AppendLine();
        yaml.AppendLine("properties:");
        WriteProp(yaml, "department", "Код");
        WriteProp(yaml, "label", "Подразделение");
        WriteProp(yaml, "parent", "Код родителя");
        WriteProp(yaml, "is_root", "Корневое");
        WriteProp(yaml, "file.mtime", "Изменён");
        yaml.AppendLine();
        yaml.AppendLine("views:");
        yaml.AppendLine("  - type: table");
        yaml.AppendLine("    name: \"Подразделения БурСервис\"");
        yaml.AppendLine("    order:");
        yaml.AppendLine("      - department");
        yaml.AppendLine("      - label");
        yaml.AppendLine("      - parent");
        yaml.AppendLine();
        File.WriteAllText(basePath, yaml.ToString(), Encoding.UTF8);
    }

    // ──── Простые enum (статусы, роли) ───────────────────────────────────
    private static void EnsureSimpleEnumFiles(string refDir, string name, string property, string[] values)
    {
        var indexPath = Path.Combine(refDir, name + ".md");
        var index = new StringBuilder();
        index.AppendLine($"# Справочник: {name}");
        index.AppendLine();
        foreach (var v in values)
        {
            index.AppendLine($"- {v}");
        }

        File.WriteAllText(indexPath, index.ToString().TrimEnd() + "\n", Encoding.UTF8);

        var folder = Path.Combine(refDir, name);
        Directory.CreateDirectory(folder);
        foreach (var v in values)
        {
            var itemPath = Path.Combine(folder, SanitizeFileName(v) + ".md");
            if (File.Exists(itemPath))
            {
                continue;
            }

            var item = new StringBuilder();
            item.AppendLine("---");
            item.AppendLine($"{property}: \"{EscapeYaml(v)}\"");
            item.AppendLine("---");
            item.AppendLine();
            item.AppendLine($"# {v}");
            File.WriteAllText(itemPath, item.ToString().TrimEnd() + "\n", Encoding.UTF8);
        }
    }

    // ──── documents.base ─────────────────────────────────────────────────
    private static void GenerateDocumentsBase(string docsDir)
    {
        var basePath = Path.Combine(docsDir, "documents.base");
        if (File.Exists(basePath) && !Directory.EnumerateFiles(docsDir, "*.md").Any())
        {
            return;
        }

        var yaml = new StringBuilder();
        yaml.AppendLine("# Burservis Documents Base — Obsidian database view");
        yaml.AppendLine("# Справочники: _справочники/типы_документов.base, _справочники/подразделения.base");
        yaml.AppendLine();
        yaml.AppendLine("filters:");
        yaml.AppendLine("  and:");
        yaml.AppendLine("    - 'file.ext == \"md\"'");
        yaml.AppendLine("    - 'doc_type != \"\"'");
        yaml.AppendLine("    - '!file.folder.contains(\"_справочники\")'");
        yaml.AppendLine("    - '!file.folder.contains(\"_шаблоны\")'");
        yaml.AppendLine();
        yaml.AppendLine("properties:");

        WriteProp(yaml, "file.mtime", "Изменён");
        WriteProp(yaml, "file.folder", "Папка");
        WriteProp(yaml, "file.name", "Имя файла");
        WriteProp(yaml, "file.size", "Размер");
        WriteProp(yaml, "file.tags", "Теги (файл)");
        WriteProp(yaml, "file.backlinks", "Обратные ссылки");
        WriteProp(yaml, "file.links", "Ссылки");

        WriteProp(yaml, "doc_id", "Номер документа");
        WriteProp(yaml, "doc_type", "Тип документа");
        WriteProp(yaml, "title", "Наименование");
        WriteProp(yaml, "department", "Подразделение");
        WriteProp(yaml, "revision", "Ред.");
        WriteProp(yaml, "status", "Статус");
        WriteProp(yaml, "effective_date", "Дата редакции");
        WriteProp(yaml, "author", "Автор");
        WriteProp(yaml, "owner", "Владелец");
        WriteProp(yaml, "approver", "Утверждающий (ФИО)");
        WriteProp(yaml, "approver_role", "Роль утверждающего");
        WriteProp(yaml, "protocol", "№ протокола");
        WriteProp(yaml, "footer_text", "Текст колонтитула");
        WriteProp(yaml, "domain", "Домен");
        WriteProp(yaml, "scope", "Область применения");
        WriteProp(yaml, "hse_critical", "Критичность HSE");
        WriteProp(yaml, "hms_level", "Уровень СМ");
        WriteProp(yaml, "version", "Версия");
        WriteProp(yaml, "tags", "Теги");
        WriteProp(yaml, "aliases", "Псевдонимы");
        yaml.AppendLine();

        yaml.AppendLine("views:");
        yaml.AppendLine("  - type: table");
        yaml.AppendLine("    name: \"Все документы\"");
        yaml.AppendLine("    order:");
        foreach (var col in new[] { "doc_id", "title", "doc_type", "department", "revision", "status", "author", "approver", "approver_role", "effective_date", "file.mtime" })
        {
            yaml.AppendLine($"      - {col}");
        }

        yaml.AppendLine();
        yaml.AppendLine("  - type: table");
        yaml.AppendLine("    name: \"По статусу\"");
        yaml.AppendLine("    groupBy:");
        yaml.AppendLine("      property: status");
        yaml.AppendLine("      direction: ASC");
        yaml.AppendLine("    order:");
        foreach (var col in new[] { "doc_id", "title", "doc_type", "department", "revision", "effective_date", "author" })
        {
            yaml.AppendLine($"      - {col}");
        }

        yaml.AppendLine();
        yaml.AppendLine("  - type: table");
        yaml.AppendLine("    name: \"По типу документа\"");
        yaml.AppendLine("    groupBy:");
        yaml.AppendLine("      property: doc_type");
        yaml.AppendLine("      direction: ASC");
        yaml.AppendLine("    order:");
        foreach (var col in new[] { "doc_id", "title", "department", "revision", "status", "author" })
        {
            yaml.AppendLine($"      - {col}");
        }

        yaml.AppendLine();
        yaml.AppendLine("  - type: table");
        yaml.AppendLine("    name: \"По подразделению\"");
        yaml.AppendLine("    groupBy:");
        yaml.AppendLine("      property: department");
        yaml.AppendLine("      direction: ASC");
        yaml.AppendLine("    order:");
        foreach (var col in new[] { "doc_id", "title", "doc_type", "revision", "status", "author" })
        {
            yaml.AppendLine($"      - {col}");
        }

        yaml.AppendLine();
        File.WriteAllText(basePath, yaml.ToString(), Encoding.UTF8);
    }

    // ──── Helpers ─────────────────────────────────────────────────────────
    private static void WriteProp(StringBuilder yaml, string key, string displayName)
    {
        yaml.AppendLine($"  {key}:");
        yaml.AppendLine($"    displayName: \"{displayName}\"");
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

    private static string EscapeYaml(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}