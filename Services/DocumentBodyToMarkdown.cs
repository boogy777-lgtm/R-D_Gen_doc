using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Defaults;
using AvaloniaApp.Services.Markdown;

namespace AvaloniaApp.Services;

/// <summary>
/// Конвертирует DocumentBody → markdown body (без YAML frontmatter).
/// Все структурные секции (1-3, 4-N, Обучение, Ссылки, Лист регистрации)
/// рендерятся с заголовками — даже если содержимое пустое.
///
/// Markdown — единый источник правды: пустые поля **всегда** заполняются
/// <see cref="FormFieldDescriptor.Placeholder"/> или
/// <see cref="FormFieldDescriptor.TemplateDefault"/> согласно
/// <see cref="FieldPermission"/> из <see cref="FormTemplate"/>.
/// Никаких разветвлений на "с плейсхолдерами"/"без плейсхолдеров" нет —
/// один и тот же путь применяется для preview, экспорта в Word и записи на диск.
///
/// Для всех DocType каталог <see cref="Forms"/> возвращает
/// дескрипторы, поэтому fallback'и работают универсально.
///
/// Все заголовки, паттерны и маркеры берутся из
/// <see cref="MarkdownSyntaxDictionary"/> — единственного источника правды.
/// </summary>
public static class DocumentBodyToMarkdown
{
    /// <summary>
    /// Конвертирует тело документа в markdown.
    /// Шаблон подбирается автоматически по <paramref name="metadata"/>;
    /// если метаданных нет — fallback-и не применяются (используется только
    /// фактическое содержимое тела, например для диагностики).
    /// </summary>
    public static string Convert(DocumentBody body)
        => Convert((DocumentMetadata?)null, body);

    /// <summary>
    /// Конвертирует тело документа в markdown с шаблоном,
    /// выбранным из <see cref="Forms"/> по <paramref name="metadata"/>.
    /// </summary>
    public static string Convert(DocumentMetadata? metadata, DocumentBody body)
    {
        var formTemplate = metadata is null ? null : Forms.GetTemplate(metadata.DocType);
        return Convert(metadata, body, formTemplate);
    }

    /// <summary>
    /// Конвертирует <see cref="DocumentBody"/> в markdown с учётом
    /// <see cref="FieldPermission"/> из переданного <paramref name="formTemplate"/>.
    /// </summary>
    /// <param name="metadata">Метаданные документа (используются для автора ЛРИ).</param>
    /// <param name="body">Тело документа.</param>
    /// <param name="formTemplate">
    /// Шаблон формы с дескрипторами полей и <see cref="FieldPermission"/>.
    /// Если <c>null</c> — fallback-и не применяются (только содержимое модели).
    /// </param>
    public static string Convert(DocumentMetadata? metadata, DocumentBody body, FormTemplate? formTemplate)
    {
        var sb = new StringBuilder();

        // Единый источник дескрипторов (null-safe).
        var allDescriptors = formTemplate?.AllDescriptors()
            ?? Array.Empty<FormFieldDescriptor>();

        // Хелперы поиска дескриптора по ключу.
        SingleFieldDescriptor? S(string key) => allDescriptors.OfType<SingleFieldDescriptor>()
            .FirstOrDefault(d => d.Key == key);
        CollectionFieldDescriptor<StringItem>? C(string key) => allDescriptors.OfType<CollectionFieldDescriptor<StringItem>>()
            .FirstOrDefault(d => d.Key == key);
        CollectionFieldDescriptor<TermPair>? CTerms(string key) => allDescriptors.OfType<CollectionFieldDescriptor<TermPair>>()
            .FirstOrDefault(d => d.Key == key);
        CollectionFieldDescriptor<ChangeLogRow>? CChangeLog(string key) => allDescriptors.OfType<CollectionFieldDescriptor<ChangeLogRow>>()
            .FirstOrDefault(d => d.Key == key);
        CollectionFieldDescriptor<BodyAppendix>? CAppendices(string key) => allDescriptors.OfType<CollectionFieldDescriptor<BodyAppendix>>()
            .FirstOrDefault(d => d.Key == key);
        TreeFieldDescriptor? T(string key) => allDescriptors.OfType<TreeFieldDescriptor>()
            .FirstOrDefault(d => d.Key == key);

        // 1 Цель — заголовок всегда, тело — с fallback на Placeholder/TemplateDefault.
        sb.AppendLine(MarkdownSyntaxDictionary.PurposeHeader);
        sb.AppendLine();
        var purposeEffective = EffectiveValue(body.Purpose, S("Purpose"));
        if (!string.IsNullOrWhiteSpace(purposeEffective))
        {
            sb.AppendLine(purposeEffective.Trim());
        }

        sb.AppendLine();

        // 2 Область применения
        sb.AppendLine(MarkdownSyntaxDictionary.ScopeHeader);
        sb.AppendLine();
        var scopeEffective = EffectiveValue(body.Scope, S("Scope"));
        if (!string.IsNullOrWhiteSpace(scopeEffective))
        {
            sb.AppendLine(scopeEffective.Trim());
        }

        sb.AppendLine();

        // 3 Термины и определения — fallback на placeholder при пустой коллекции.
        sb.AppendLine(MarkdownSyntaxDictionary.TermsHeader);
        sb.AppendLine();
        var termsDescriptor = CTerms("Terms");
        var termsToRender = body.Terms.Count > 0
            ? body.Terms.OrderBy(t => t.Position).ToList()
            : new List<TermPair>();
        if (termsToRender.Count == 0)
        {
            var termsFallback = termsDescriptor?.Placeholder ?? termsDescriptor?.TemplateDefault;
            if (!string.IsNullOrWhiteSpace(termsFallback))
            {
                termsToRender.Add(new TermPair
                {
                    Position = 0,
                    Term = termsFallback,
                    Definition = termsFallback,
                });
            }
        }

        foreach (var term in termsToRender)
        {
            sb.Append(MarkdownSyntaxDictionary.BulletPrefix);
            sb.Append(MdFormatHelper.Format(term.Term, MdSyntax.Bold));
            sb.Append(MarkdownSyntaxDictionary.EmDashSeparator);
            sb.AppendLine(MdFormatHelper.Format(term.Definition, MdSyntax.Plain));
        }

        sb.AppendLine();

        // Центральная часть (4, 5, 6, ...) — fallback на placeholder при пустой коллекции.
        void RenderSection(BodySection section, int headingLevel)
        {
            var prefix = new string('#', headingLevel);
            var number = string.IsNullOrEmpty(section.HierarchyPath) ? "?" : section.HierarchyPath;
            sb.Append(prefix);
            sb.Append(' ');
            sb.Append(number);

            var hasTitle = !string.IsNullOrWhiteSpace(section.Title);
            if (hasTitle)
            {
                sb.Append(' ');
                sb.AppendLine(EscapeMarkdown(section.Title));
            }
            else
            {
                sb.AppendLine();
            }

            sb.AppendLine();

            var hasBody = !string.IsNullOrWhiteSpace(section.Body);
            if (hasBody)
            {
                sb.AppendLine(section.Body.Trim());
                sb.AppendLine();
            }

            foreach (var child in section.Children.OrderBy(c => c.Position))
            {
                RenderSection(child, headingLevel + 1);
            }
        }

        if (body.Sections.Count == 0)
        {
            // Пустой список секций — рендерим один placeholder-узел,
            // чтобы секция была представлена в MD/DOCX.
            var sectionsDescriptor = T("Sections");
            var placeholderText = sectionsDescriptor?.Placeholder
                ?? sectionsDescriptor?.TemplateDefault;
            if (!string.IsNullOrWhiteSpace(placeholderText))
            {
                var placeholderSection = new BodySection
                {
                    Position = 0,
                    Level = 1,
                    Title = placeholderText,
                    Body = placeholderText,
                    HierarchyPath = "4",
                };
                RenderSection(placeholderSection, headingLevel: 2);
            }
        }
        else
        {
            foreach (var section in body.Sections.OrderBy(s => s.Position))
            {
                RenderSection(section, headingLevel: 2);
            }
        }

        // Обучение — с fallback на Placeholder/TemplateDefault.
        sb.AppendLine(MarkdownSyntaxDictionary.TrainingHeader);
        sb.AppendLine();
        sb.AppendLine(MarkdownSyntaxDictionary.PrimaryTrainingSubHeader);
        sb.AppendLine();
        var primaryEffective = EffectiveValue(body.PrimaryTraining, S("PrimaryTraining"));
        if (!string.IsNullOrWhiteSpace(primaryEffective))
        {
            sb.AppendLine(primaryEffective.Trim());
        }

        sb.AppendLine();
        sb.AppendLine(MarkdownSyntaxDictionary.RecurrentTrainingSubHeader);
        sb.AppendLine();
        var recurrentEffective = EffectiveValue(body.RecurrentTraining, S("RecurrentTraining"));
        if (!string.IsNullOrWhiteSpace(recurrentEffective))
        {
            sb.AppendLine(recurrentEffective.Trim());
        }

        sb.AppendLine();

        // Ссылки — с fallback на Placeholder/TemplateDefault.
        sb.AppendLine(MarkdownSyntaxDictionary.ReferencesHeader);
        sb.AppendLine();
        sb.AppendLine(MarkdownSyntaxDictionary.ParentDocumentSubHeader);
        sb.AppendLine();

        var parentEffective = EffectiveValue(body.ParentDocument, S("ParentDocument"));
        sb.AppendLine(MdFormatHelper.Format(parentEffective, MdSyntax.Wikilink));

        sb.AppendLine();
        sb.AppendLine(MarkdownSyntaxDictionary.RelatedDocumentsSubHeader);
        sb.AppendLine();

        var relatedEffective = EffectiveValues(body.RelatedDocuments, C("RelatedDocuments"));
        foreach (var value in relatedEffective)
        {
            sb.Append(MarkdownSyntaxDictionary.BulletPrefix);
            sb.AppendLine(MdFormatHelper.Format(value, MdSyntax.Wikilink));
        }

        sb.AppendLine();

        // Лист регистрации изменений — fallback на placeholder-строку при пустой коллекции.
        sb.AppendLine(MarkdownSyntaxDictionary.ChangeLogHeader);
        sb.AppendLine();
        sb.AppendLine(MarkdownSyntaxDictionary.ChangeLogTableHeader);
        sb.AppendLine(MarkdownSyntaxDictionary.ChangeLogTableSeparator);
        var changeLogDescriptor = CChangeLog("ChangeLog");
        var changeLogRows = body.ChangeLog.Count > 0
            ? body.ChangeLog.OrderBy(c => c.Position).ToList()
            : new List<ChangeLogRow>();
        if (changeLogRows.Count == 0)
        {
            var changeLogFallback = changeLogDescriptor?.Placeholder ?? changeLogDescriptor?.TemplateDefault;
            if (!string.IsNullOrWhiteSpace(changeLogFallback))
            {
                changeLogRows.Add(new ChangeLogRow
                {
                    Position = 0,
                    Description = changeLogFallback,
                });
            }
        }

        foreach (var row in changeLogRows)
        {
            sb.Append("| ");
            sb.Append(EscapeMarkdown(row.Date));
            sb.Append(" | ");
            sb.Append(EscapeMarkdown(row.Revision));
            sb.Append(" | ");
            sb.Append(EscapeMarkdown(metadata?.Author ?? string.Empty));
            sb.Append(" | ");
            sb.Append(EscapeMarkdown(row.Reviewer));
            sb.Append(" | ");
            sb.Append(EscapeMarkdown(row.Description));
            sb.AppendLine(" |");
        }

        sb.AppendLine();

        // Приложения — fallback на placeholder-приложение при пустой коллекции.
        var appendicesDescriptor = CAppendices("Appendices");
        var appendicesToRender = body.Appendices.Count > 0
            ? body.Appendices.OrderBy(a => a.Position).ToList()
            : new List<BodyAppendix>();
        if (appendicesToRender.Count == 0)
        {
            var appendixFallback = appendicesDescriptor?.Placeholder ?? appendicesDescriptor?.TemplateDefault;
            if (!string.IsNullOrWhiteSpace(appendixFallback))
            {
                appendicesToRender.Add(new BodyAppendix
                {
                    Position = 1,
                    Title = appendixFallback,
                    Body = appendixFallback,
                });
            }
        }

        // Сквозная нумерация таблиц по документу (а не per-appendix):
        // используем GlobalTableIndex, рассчитываемый Engine-ом при
        // добавлении/удалении приложений или таблиц.
        foreach (var app in appendicesToRender)
        {
            sb.AppendLine(MarkdownSyntaxDictionary.FormatAppendixHeader(app.Position));
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(app.Title))
            {
                sb.AppendLine(EscapeMarkdown(app.Title));
                sb.AppendLine();
            }

            if (app.Sections != null && app.Sections.Count > 0)
            {
                foreach (var subSection in app.Sections.OrderBy(s => s.Position))
                {
                    var tableNumber = subSection.GlobalTableIndex > 0
                        ? subSection.GlobalTableIndex
                        : 1; // fallback для подсекций без Engine (диагностические вызовы)
                    var titlePart = string.IsNullOrWhiteSpace(subSection.Title)
                        ? string.Empty
                        : $": {EscapeMarkdown(subSection.Title)}";

                    sb.AppendLine(MarkdownSyntaxDictionary.FormatTableSubHeader(tableNumber) +
                        (titlePart == string.Empty ? string.Empty : titlePart));
                    sb.AppendLine();

                    if (!string.IsNullOrWhiteSpace(subSection.Body))
                    {
                        sb.AppendLine(subSection.Body.Trim());
                    }

                    sb.AppendLine();
                }
            }
            else if (!string.IsNullOrWhiteSpace(app.Body))
            {
                sb.AppendLine(app.Body.Trim());
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Вычисляет эффективное значение для одиночного поля с учётом
    /// <see cref="FieldPermission"/> дескриптора.
    /// Всегда возвращает подставленное значение (либо пользовательское, либо placeholder/default) —
    /// нет ветки, в которой пустое поле возвращалось бы как есть.
    /// </summary>
    private static string EffectiveValue(string? bodyValue, SingleFieldDescriptor? descriptor)
    {
        if (descriptor == null)
        {
            // Без шаблона — отдаём фактическое значение (может быть пустым).
            // Это допустимо только для диагностических вызовов без метаданных.
            return bodyValue ?? string.Empty;
        }

        return descriptor.Permission switch
        {
            FieldPermission.Static => descriptor.TemplateDefault ?? string.Empty,
            FieldPermission.Mutable => !string.IsNullOrWhiteSpace(bodyValue)
                ? bodyValue
                : descriptor.Placeholder ?? descriptor.TemplateDefault ?? string.Empty,
            FieldPermission.Dynamic => (descriptor.TemplateDefault ?? string.Empty) + (bodyValue ?? string.Empty),
            _ => bodyValue ?? string.Empty,
        };
    }

    /// <summary>
    /// Вычисляет эффективные значения для коллекции <see cref="StringItem"/>
    /// с учётом <see cref="FieldPermission"/> дескриптора.
    /// </summary>
    private static IReadOnlyList<string> EffectiveValues(
        ObservableCollection<StringItem> items,
        CollectionFieldDescriptor<StringItem>? descriptor)
    {
        var nonEmpty = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Value))
            .Select(i => i.Value)
            .ToList();

        if (nonEmpty.Count > 0)
        {
            return nonEmpty;
        }

        if (descriptor?.Permission == FieldPermission.Mutable)
        {
            var fallback = descriptor.Placeholder ?? descriptor.TemplateDefault;
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return new[] { fallback };
            }
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Экранирует символы, ломающие Markdown-структуру.
    /// Делегирует в MdFormatHelper.Format(..., Plain).
    /// </summary>
    private static string EscapeMarkdown(string s) => MdFormatHelper.Format(s, MdSyntax.Plain);
}