using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AvaloniaApp.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AvaloniaApp.Services;

/// <summary>
/// Рендеринг документа по метаданным и фабричному шаблону.
/// </summary>
public static class TemplateEngine
{
    private static readonly Regex PlaceholderRegex = new(
        @"\{\{(\w+)\}\}",
        RegexOptions.Compiled);

    private static readonly Regex FrontmatterPlaceholderRegex = new(
        @"<%\s*tp\.frontmatter\.(\w+)\s*%>",
        RegexOptions.Compiled);

    /// <summary>
    /// Формирует Markdown-документ на основе метаданных и шаблона.
    /// </summary>
    public static string Render(DocumentMetadata metadata, TemplateDefinition definition)
    {
        if (!string.Equals(metadata.DocType, definition.DocTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Несоответствие типа документа: метаданные '{metadata.DocType}', шаблон '{definition.DocTypeCode}'.");
        }

        var sb = new StringBuilder();

        // 1. YAML frontmatter
        sb.AppendLine(SerializeFrontmatter(metadata));

        // 2. Shield перед H1
        if (definition.HasShield && definition.ShieldPlacement == ShieldPlacement.BeforeH1)
        {
            sb.AppendLine(definition.ShieldTemplate);
        }

        // 3. Заголовок H1
        sb.AppendLine($"# {metadata.DocId} {metadata.Title}".Trim());

        // 4. Abstract
        sb.AppendLine(definition.AbstractTemplate);

        // 5. Shield после abstract
        if (definition.HasShield && definition.ShieldPlacement == ShieldPlacement.AfterAbstract)
        {
            sb.AppendLine(definition.ShieldTemplate);
        }

        // 6. Разделы в нативном порядке
        foreach (var section in definition.Sections)
        {
            sb.AppendLine($"## {section.Number} {section.Title}");
            sb.AppendLine();
            sb.AppendLine(section.MarkdownTemplate);
        }

        // 7. Обучение
        sb.AppendLine("## Обучение");
        sb.AppendLine();
        sb.AppendLine(definition.TrainingTemplate);

        // 8. Ссылки
        sb.AppendLine("## Ссылки");
        sb.AppendLine();
        sb.AppendLine(definition.LinksTemplate);

        // 9. Лист регистрации изменений
        sb.AppendLine("## Лист регистрации изменений");
        sb.AppendLine();
        sb.AppendLine(definition.ChangeLogTemplate);

        // 10. Копирайт
        sb.AppendLine("_______");
        sb.AppendLine(definition.CopyrightTemplate);

        var result = sb.ToString();
        result = ReplaceFrontmatterPlaceholders(result, metadata);
        result = ReplacePropertyPlaceholders(result, metadata);
        return result;
    }

    private static string SerializeFrontmatter(DocumentMetadata metadata)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var dto = new FrontmatterDto
        {
            DocId = metadata.DocId,
            DocType = metadata.DocType,
            Title = metadata.Title,
            Department = metadata.DepartmentName,
            Revision = metadata.Revision,
            Status = metadata.Status,
            EffectiveDate = metadata.EffectiveDate,
            Author = metadata.Author,
            Owner = metadata.Owner,
            Approver = metadata.Approver,
            ApproverRole = metadata.ApproverRole,
            Protocol = metadata.Protocol,
            FooterText = metadata.FooterText,
        };

        var yamlText = serializer.Serialize(dto).TrimEnd();
        return $"---\n{yamlText}\n---";
    }

    private static string ReplaceFrontmatterPlaceholders(string text, DocumentMetadata metadata)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["doc_id"] = metadata.DocId,
            ["doc_type"] = metadata.DocType,
            ["title"] = metadata.Title,
            ["department"] = metadata.DepartmentName,
            ["revision"] = metadata.Revision,
            ["status"] = metadata.Status,
            ["effective_date"] = metadata.EffectiveDate,
            ["author"] = metadata.Author,
            ["owner"] = metadata.Owner,
            ["approver"] = metadata.Approver,
            ["approver_role"] = metadata.ApproverRole,
            ["protocol"] = metadata.Protocol,
            ["footer_text"] = metadata.FooterText,

            // Backward compatibility для старых шаблонов.
            ["domain"] = metadata.DepartmentName,
        };

        return FrontmatterPlaceholderRegex.Replace(text, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var val) ? val : string.Empty;
        });
    }

    private static string ReplacePropertyPlaceholders(string text, DocumentMetadata metadata)
    {
        var properties = typeof(DocumentMetadata).GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        return PlaceholderRegex.Replace(text, match =>
        {
            var name = match.Groups[1].Value;
            if (!properties.TryGetValue(name, out var property))
            {
                return match.Value;
            }

            var value = property.GetValue(metadata);
            if (value == null)
            {
                return string.Empty;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                return string.Join(", ", enumerable.Cast<object>());
            }

            return value.ToString() ?? string.Empty;
        });
    }

    private class FrontmatterDto
    {
        public string DocId { get; set; } = string.Empty;

        public string DocType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Revision { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string EffectiveDate { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string Owner { get; set; } = string.Empty;

        public string Approver { get; set; } = string.Empty;

        public string ApproverRole { get; set; } = string.Empty;

        public string Protocol { get; set; } = string.Empty;

        public string FooterText { get; set; } = string.Empty;
    }
}
