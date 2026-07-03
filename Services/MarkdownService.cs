using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Defaults;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис для чтения MD-файла и разбора YAML frontmatter.
/// </summary>
public static class MarkdownService
{
    private static readonly Regex FrontmatterRegex = new(
        @"^---\s*\r?\n(.*?)\s*\r?\n---\s*\r?\n",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Разбирает текст MD на метаданные и тело.
    /// </summary>
    public static (DocumentMetadata Metadata, string Body) Parse(string markdownText)
    {
        var metadata = new DocumentMetadata();
        var body = markdownText ?? string.Empty;

        var match = FrontmatterRegex.Match(body);
        if (match.Success)
        {
            var yamlText = match.Groups[1].Value;
            body = body.Substring(match.Index + match.Length);

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var yaml = deserializer.Deserialize<YamlFrontmatterRead>(yamlText) ?? new YamlFrontmatterRead();

                // Obsidian-совместимость: сохраняем ключи, не входящие в схему (tags, aliases, cssclasses…).
                // Десериализуем YAML как raw-словарь, вычитаем известные ключи — остаток в ExtraYaml.
                try
                {
                    var rawDeserializer = new DeserializerBuilder().Build();
                    var raw = rawDeserializer.Deserialize<Dictionary<object, object>>(yamlText);
                    if (raw != null)
                    {
                        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            "doc_id", "doc_type", "title", "department", "revision", "status",
                            "effective_date", "author", "owner", "approver", "approver_role",
                            "protocol", "footer_text",
                            "division_enabled", "division_code", "discipline_enabled", "discipline_code",
                            "index_enabled", "index_code", "language_enabled", "language_code",
                            "document_name", "document_code", "approved_by", "date_approved", "domain",
                            "basis_card", "version",
                        };

                        foreach (var kv in raw)
                        {
                            var key = kv.Key?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(key) && !knownKeys.Contains(key))
                            {
                                metadata.ExtraYaml[key] = kv.Value;
                            }
                        }
                    }
                }
                catch
                {
                    // Не смогли разобрать raw — не критично, просто не будет extra-ключей.
                }

                // Backward compatibility: старый ключ domain мигрируется в department.
                var department = Coalesce(yaml.Department, MapLegacyDomain(yaml.Domain));

                metadata.DocId = Coalesce(yaml.DocId, yaml.DocumentCode, metadata.DocId);
                metadata.DocType = Coalesce(yaml.DocType, metadata.DocType);
                metadata.Title = Coalesce(yaml.Title, yaml.DocumentName, metadata.Title);
                metadata.DepartmentName = Coalesce(department, metadata.DepartmentName);
                metadata.Revision = Coalesce(yaml.Revision, metadata.Revision);
                metadata.Status = Coalesce(yaml.Status, metadata.Status);
                metadata.EffectiveDate = Coalesce(yaml.EffectiveDate, yaml.DateApproved, metadata.EffectiveDate);
                metadata.Author = Coalesce(yaml.Author, metadata.Author);
                metadata.Owner = Coalesce(yaml.Owner, metadata.Owner);
                metadata.Approver = Coalesce(yaml.Approver, yaml.ApprovedBy, metadata.Approver);
                metadata.ApproverRole = Coalesce(yaml.ApproverRole, metadata.ApproverRole);
                metadata.Protocol = Coalesce(yaml.Protocol, metadata.Protocol);
                metadata.FooterText = Coalesce(yaml.FooterText, metadata.FooterText);

                // Опциональные сегменты шифра документа
                metadata.DivisionEnabled = yaml.DivisionEnabled ?? metadata.DivisionEnabled;
                metadata.DivisionCode = Coalesce(yaml.DivisionCode, metadata.DivisionCode);
                metadata.DisciplineEnabled = yaml.DisciplineEnabled ?? metadata.DisciplineEnabled;
                metadata.DisciplineCode = Coalesce(yaml.DisciplineCode, metadata.DisciplineCode);
                metadata.IndexEnabled = yaml.IndexEnabled ?? metadata.IndexEnabled;
                metadata.IndexCode = Coalesce(yaml.IndexCode, metadata.IndexCode);
                metadata.LanguageEnabled = yaml.LanguageEnabled ?? metadata.LanguageEnabled;
                metadata.LanguageCode = Coalesce(yaml.LanguageCode, metadata.LanguageCode);

                // basis_card больше не хранится в метаданных — игнорируем.
            }
            catch (Exception ex)
            {
                // Если YAML сломан — игнорируем и отдаём пустые метаданные.
                System.Diagnostics.Debug.WriteLine($"YAML parse error: {ex.Message}");
            }
        }

        return (metadata, body.TrimStart());
    }

    /// <summary>
    /// Убирает YAML-frontmatter из markdown-текста и возвращает только тело.
    /// </summary>
    public static string StripFrontmatter(string markdownText)
    {
        var (_, body) = Parse(markdownText);
        return body;
    }

    /// <summary>
    /// Собирает полный MD-файл из метаданных и тела.
    /// Исходные метаданные не мутируются.
    /// Пустые строковые поля всегда заменяются placeholder/TemplateDefault
    /// через <see cref="Metadata.ToEffectiveMetadata"/>.
    /// Markdown — единый источник правды: файл на диске, preview и DOCX
    /// используют одну и ту же строку.
    /// </summary>
    /// <param name="metadata">Метаданные документа.</param>
    /// <param name="body">Markdown-тело.</param>
    public static string Compose(DocumentMetadata metadata, string body)
    {
        var effective = Metadata.ToEffectiveMetadata(metadata);

        // DRY: используем Write-специализацию (без legacy-полей).
        // 20 общих свойств задаются через MetadataToYaml → единая точка истины.
        var yaml = MetadataToYaml(effective);

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var yamlText = serializer.Serialize(yaml).TrimEnd();

        // Obsidian-совместимость: дописываем extra-ключи (tags, aliases, cssclasses…)
        // после сериализованного DTO, чтобы не потерять их при round-trip.
        if (metadata.ExtraYaml.Count > 0)
        {
            var extraSerializer = new SerializerBuilder().Build();
            foreach (var kv in metadata.ExtraYaml)
            {
                var extraLine = extraSerializer.Serialize(new Dictionary<string, object?> { [kv.Key] = kv.Value }).TrimEnd();
                yamlText += "\n" + extraLine;
            }
        }

        return $"---\n{yamlText}\n---\n\n{body.TrimStart()}";
    }

    /// <summary>
    /// Маппит DocumentMetadata → DTO для YAML-сериализации.
    /// Раньше был inline-копипаст из 22 свойств, теперь — единая точка истины.
    /// </summary>
    private static YamlFrontmatterWrite MetadataToYaml(DocumentMetadata m) => new()
    {
        DocId = m.DocId,
        DocType = m.DocType,
        Title = m.Title,
        Department = m.DepartmentName,
        Revision = m.Revision,
        Status = m.Status,
        EffectiveDate = m.EffectiveDate,
        Author = m.Author,
        Owner = m.Owner,
        Approver = m.Approver,
        ApproverRole = m.ApproverRole,
        Protocol = m.Protocol,
        FooterText = m.FooterText,
        DivisionEnabled = m.DivisionEnabled,
        DivisionCode = m.DivisionCode,
        DisciplineEnabled = m.DisciplineEnabled,
        DisciplineCode = m.DisciplineCode,
        IndexEnabled = m.IndexEnabled,
        IndexCode = m.IndexCode,
        LanguageEnabled = m.LanguageEnabled,
        LanguageCode = m.LanguageCode,
    };

    private static string Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Миграция legacy-ключа <c>domain</c> в <c>department</c>.
    /// </summary>
    private static string? MapLegacyDomain(string? domain)
    {
        return string.IsNullOrWhiteSpace(domain)
            ? null
            : $"Бывший домен: {domain}";
    }

    /// <summary>
    /// Единый DTO для чтения/записи YAML frontmatter (DRY).
    /// Legacy-поля помечены <see cref="YamlIgnoreAttribute"/> — сериализатор
    /// их не выводит (Write-режим), но десериализатор игнорирует их отсутствие
    /// благодаря <c>IgnoreUnmatchedProperties</c> (Read-режим).
    /// </summary>
    private class YamlFrontmatterRead
    {
        public string? DocId { get; set; }

        public string? DocType { get; set; }

        public string? Title { get; set; }

        public string? Department { get; set; }

        public string? Revision { get; set; }

        public string? Status { get; set; }

        public string? EffectiveDate { get; set; }

        public string? Author { get; set; }

        public string? Owner { get; set; }

        public string? Approver { get; set; }

        public string? ApproverRole { get; set; }

        public string? Protocol { get; set; }

        public string? FooterText { get; set; }

        // Опциональные сегменты шифра
        public bool? DivisionEnabled { get; set; }

        public string? DivisionCode { get; set; }

        public bool? DisciplineEnabled { get; set; }

        public string? DisciplineCode { get; set; }

        public bool? IndexEnabled { get; set; }

        public string? IndexCode { get; set; }

        public bool? LanguageEnabled { get; set; }

        public string? LanguageCode { get; set; }

        // Legacy-поля для обратной совместимости при чтении старых .md.
        public string? DocumentName { get; set; }

        public string? DocumentCode { get; set; }

        public string? ApprovedBy { get; set; }

        public string? DateApproved { get; set; }

        public string? Domain { get; set; }
    }

    /// <summary>
    /// DTO для записи YAML. Не содержит legacy-полей.
    /// </summary>
    /// <remarks>
    /// <para>
    /// DRY: 20 общих свойств определены один раз в <see cref="MetadataToYaml"/>
    /// — единая точка истины для маппинга <see cref="DocumentMetadata"/> → DTO.
    /// </para>
    /// <para>
    /// Почему НЕ наследование от <see cref="YamlFrontmatterRead"/>: YamlDotNet
    /// сериализует ВСЕ свойства в иерархии, включая унаследованные — legacy-поля
    /// попали бы в выходной YAML, нарушая контракт round-trip.
    /// </para>
    /// </remarks>
    private sealed class YamlFrontmatterWrite
    {
        public string? DocId { get; set; }

        public string? DocType { get; set; }

        public string? Title { get; set; }

        public string? Department { get; set; }

        public string? Revision { get; set; }

        public string? Status { get; set; }

        public string? EffectiveDate { get; set; }

        public string? Author { get; set; }

        public string? Owner { get; set; }

        public string? Approver { get; set; }

        public string? ApproverRole { get; set; }

        public string? Protocol { get; set; }

        public string? FooterText { get; set; }

        public bool? DivisionEnabled { get; set; }

        public string? DivisionCode { get; set; }

        public bool? DisciplineEnabled { get; set; }

        public string? DisciplineCode { get; set; }

        public bool? IndexEnabled { get; set; }

        public string? IndexCode { get; set; }

        public bool? LanguageEnabled { get; set; }

        public string? LanguageCode { get; set; }
    }
}
