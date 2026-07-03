using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AvaloniaApp.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AvaloniaApp.Services;

/// <summary>
/// Загрузчик фабричных шаблонов (только чтение, никаких записей в _factory).
/// </summary>
public static class TemplateLoader
{
    private static readonly Regex FrontmatterRegex = new(
        @"^---\s*\r?\n(.*?)\s*\r?\n---\s*\r?\n",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CursorRegex = new(
        @"<%\s*tp\.file\.cursor\(\d+\)\s*%>",
        RegexOptions.Compiled);

    private static readonly Regex IncludeRegex = new(
        @"<%\s*tp\.file\.include\(""([^""]+)""\)\s*%>",
        RegexOptions.Compiled);

    private static readonly Regex HeaderRegex = new(
        @"^##\s+(.*)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex NumberedHeaderRegex = new(
        @"^##\s+(\d+)\s+(.*)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex AbstractRegex = new(
        @"^[ \t]*>\s*\[!abstract\].*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ChecklistItemRegex = new(
        @"^- \[[ xX]\]",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Загружает фабричный шаблон (один файл — T_Стандарт.md).
    /// Если файл не найден — возвращает пустой список (без падения).
    /// </summary>
    public static IReadOnlyList<TemplateDefinition> LoadFromDirectory(string factoryPath)
    {
        const string fileName = "T_Стандарт.md";
        var path = Path.Combine(factoryPath, fileName);
        if (!File.Exists(path))
        {
            return Array.Empty<TemplateDefinition>();
        }

        return new[] { LoadSingle(path) };
    }

    /// <summary>
    /// Загружает один фабричный шаблон.
    /// </summary>
    public static TemplateDefinition LoadSingle(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var factoryDir = Path.GetDirectoryName(filePath)!;

        // --- YAML frontmatter ---
        string yamlText = string.Empty;
        string body = text;
        var fmMatch = FrontmatterRegex.Match(text);
        if (fmMatch.Success)
        {
            yamlText = fmMatch.Groups[1].Value;
            body = text.Substring(fmMatch.Index + fmMatch.Length);
        }

        var frontmatter = DeserializeFrontmatter(yamlText);
        var definition = new TemplateDefinition
        {
            DocTypeCode = frontmatter.DocType ?? string.Empty,
            DefaultApproverRole = frontmatter.ApproverRole ?? string.Empty,
            HmsLevel = MapHmsLevel(frontmatter.DocType),
        };

        // --- tp.file.cursor(N) ---
        body = CursorRegex.Replace(body, string.Empty);

        // --- tp.file.include([[Shield_XX]]) ---
        body = ResolveIncludes(body, factoryDir, definition);

        // --- Флаги по содержимому ---
        definition.HasMermaidBlock = body.Contains("```mermaid", StringComparison.OrdinalIgnoreCase);
        definition.HasRaciTable = body.Contains("RACI", StringComparison.OrdinalIgnoreCase);
        definition.HasJsaTable = body.Contains("JSA", StringComparison.OrdinalIgnoreCase);
        definition.HasChecklist = ChecklistItemRegex.IsMatch(body);
        definition.HasAppendixChecklist = body.Contains("## Приложение А.", StringComparison.OrdinalIgnoreCase);

        // --- Abstract и разделы ---
        definition.AbstractTemplate = ExtractAbstract(body);
        ParseSections(body, definition);
        definition.CopyrightTemplate = ExtractCopyright(body);

        return definition;
    }

    private static TemplateFrontmatter DeserializeFrontmatter(string yamlText)
    {
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            return new TemplateFrontmatter();
        }

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            return deserializer.Deserialize<TemplateFrontmatter>(yamlText) ?? new TemplateFrontmatter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Template frontmatter parse error: {ex.Message}");
            return new TemplateFrontmatter();
        }
    }

    private static int MapHmsLevel(string? docType) => docType switch
    {
        "Code" => 0,
        "Policy" => 1,
        "BP" => 2,
        "ST" => 3,
        "PM" => 4,
        "WM" => 5,
        "Basis" => -1,
        _ => 0,
    };

    private static string ResolveIncludes(string body, string factoryDir, TemplateDefinition definition)
    {
        var h1Index = body.IndexOf("# ", StringComparison.Ordinal);
        string? shieldDocType = null;
        string shieldContent = string.Empty;
        var placement = ShieldPlacement.AfterAbstract;

        var result = IncludeRegex.Replace(body, match =>
        {
            var argument = match.Groups[1].Value;
            var shieldMatch = Regex.Match(argument, @"\[\[Shield_([A-Za-z0-9_]+)\]\]");
            if (shieldMatch.Success)
            {
                shieldDocType = shieldMatch.Groups[1].Value;
                var shieldPath = Path.Combine(factoryDir, "_shields", $"Shield_{shieldDocType}.md");
                if (File.Exists(shieldPath))
                {
                    shieldContent = File.ReadAllText(shieldPath);
                }

                placement = (match.Index < h1Index) ? ShieldPlacement.BeforeH1 : ShieldPlacement.AfterAbstract;
            }

            return string.Empty;
        });

        definition.HasShield = !string.IsNullOrEmpty(shieldContent);
        definition.ShieldDocType = shieldDocType;
        definition.ShieldTemplate = shieldContent;
        definition.ShieldPlacement = placement;

        return result;
    }

    private static string ExtractAbstract(string body)
    {
        var lines = body.Split(new[] { "\n", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        bool inAbstract = false;

        foreach (var raw in lines)
        {
            if (AbstractRegex.IsMatch(raw))
            {
                inAbstract = true;
                sb.AppendLine(raw);
                continue;
            }

            if (!inAbstract)
            {
                continue;
            }

            if (raw.StartsWith(">"))
            {
                sb.AppendLine(raw);
            }
            else if (string.IsNullOrWhiteSpace(raw))
            {
                sb.AppendLine(raw);
            }
            else
            {
                break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void ParseSections(string body, TemplateDefinition definition)
    {
        var matches = HeaderRegex.Matches(body);
        string? trainingContent = null;
        string? linksContent = null;
        string? changelogContent = null;

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var headerLine = match.Groups[1].Value.Trim();
            var start = match.Index + match.Length;
            var end = (i < matches.Count - 1) ? matches[i + 1].Index : body.Length;
            var content = body.Substring(start, end - start).Trim();

            if (headerLine.StartsWith("Обучение", StringComparison.OrdinalIgnoreCase))
            {
                trainingContent = content;
                continue;
            }

            if (headerLine.StartsWith("Ссылки", StringComparison.OrdinalIgnoreCase))
            {
                linksContent = content;
                continue;
            }

            if (headerLine.StartsWith("Лист регистрации изменений", StringComparison.OrdinalIgnoreCase))
            {
                changelogContent = CutTrailingBlocks(content);
                continue;
            }

            // Обычные разделы добавляем только до блока "Обучение".
            if (trainingContent != null)
            {
                continue;
            }

            var numberedMatch = NumberedHeaderRegex.Match($"## {headerLine}");
            int number = 0;
            var title = headerLine;
            if (numberedMatch.Success)
            {
                number = int.Parse(numberedMatch.Groups[1].Value);
                title = numberedMatch.Groups[2].Value.Trim();
            }

            definition.Sections.Add(new SectionDefinition
            {
                Number = number,
                Title = title,
                MarkdownTemplate = content,
                Kind = InferKind(content),
            });
        }

        definition.TrainingTemplate = trainingContent ?? string.Empty;
        definition.LinksTemplate = linksContent ?? string.Empty;
        definition.ChangeLogTemplate = changelogContent ?? string.Empty;
    }

    private static string CutTrailingBlocks(string content)
    {
        var cuts = new List<int> { content.Length };

        var appendixIdx = content.IndexOf("## Приложение", StringComparison.Ordinal);
        if (appendixIdx >= 0)
        {
            cuts.Add(appendixIdx);
        }

        var dashSepIdx = content.IndexOf("\n---\n", StringComparison.Ordinal);
        if (dashSepIdx >= 0)
        {
            cuts.Add(dashSepIdx);
        }

        var lineSepIdx = content.IndexOf("\n_______\n", StringComparison.Ordinal);
        if (lineSepIdx >= 0)
        {
            cuts.Add(lineSepIdx);
        }

        var len = cuts.Min();
        return content.Substring(0, len).Trim();
    }

    private static SectionKind InferKind(string content)
    {
        if (content.Contains("```mermaid", StringComparison.OrdinalIgnoreCase))
        {
            return SectionKind.Mermaid;
        }

        if (content.Contains("RACI", StringComparison.OrdinalIgnoreCase))
        {
            return SectionKind.Raci;
        }

        if (content.Contains("JSA", StringComparison.OrdinalIgnoreCase))
        {
            return SectionKind.Jsa;
        }

        if (ChecklistItemRegex.IsMatch(content))
        {
            return SectionKind.Checklist;
        }

        if (content.Contains('|'))
        {
            return SectionKind.Table;
        }

        return SectionKind.FreeText;
    }

    private static string ExtractCopyright(string body)
    {
        var idx = body.LastIndexOf("_______", StringComparison.Ordinal);
        if (idx < 0)
        {
            return "®© ООО «БурСервис», 2026 г.";
        }

        var after = body.Substring(idx + "_______".Length).Trim();
        var lines = after.Split(new[] { "\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        return lines.FirstOrDefault() ?? "®© ООО «БурСервис», 2026 г.";
    }

    private class TemplateFrontmatter
    {
        public string? DocType { get; set; }

        public string? ApproverRole { get; set; }
    }
}
