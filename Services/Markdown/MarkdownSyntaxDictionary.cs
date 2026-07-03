using System;
using System.Text.RegularExpressions;

namespace AvaloniaApp.Services.Markdown;

/// <summary>
/// Единый источник правды для Markdown-разметки документа.
/// Содержит заголовки секций, заголовки подсекций, заголовки таблицы ЛРИ,
/// маркеры приложений, паттерны парсинга и инварианты нумерации.
///
/// Принципы:
/// <list type="bullet">
///   <item>Генератор (<see cref="DocumentBodyToMarkdown"/>) и парсер
///         (<see cref="DocumentBodyParser"/>) читают только из этого словаря —
///         никаких магических строк в коде.</item>
///   <item>Изменение формата делается в одном месте — здесь.</item>
///   <item>Регулярные выражения компилируются один раз и кешируются в
///         <see cref="Lazy{T}"/>.</item>
/// </list>
/// </summary>
public static class MarkdownSyntaxDictionary
{
    // === Заголовки верхнего уровня (## N Title) ===
    public const string PurposeHeader = "## 1 Цель";
    public const string ScopeHeader = "## 2 Область применения";
    public const string TermsHeader = "## 3 Термины и определения";

    // === Заголовки без номера (## Title) ===
    public const string TrainingHeader = "## Обучение";
    public const string ReferencesHeader = "## Ссылки";
    public const string ChangeLogHeader = "## Лист регистрации изменений";

    // === Заголовки подсекций (### Title) ===
    public const string PrimaryTrainingSubHeader = "### Первичное обучение";
    public const string RecurrentTrainingSubHeader = "### Повторное обучение";
    public const string ParentDocumentSubHeader = "### Родительский документ";
    public const string RelatedDocumentsSubHeader = "### Связанные документы";

    // === Шапка таблицы ЛРИ ===
    public const string ChangeLogTableHeader =
        "| Дата ред. | Номер ред. | Разработал | Проверил | Краткое описание изменений |";
    public const string ChangeLogTableSeparator =
        "| :--- | :--- | :--- | :--- | :--- |";

    /// <summary>
    /// Заголовок колонки «Дата ред.» в шапке таблицы ЛРИ (для распознавания строки-шапки).
    /// </summary>
    public const string ChangeLogDateHeaderToken = "Дата ред.";

    /// <summary>
    /// Альтернативный заголовок «Дата редакции», встречающийся в legacy-документах.
    /// Парсер принимает оба варианта: «Дата ред.» и «Дата редакции».
    /// </summary>
    public const string LegacyChangeLogDateHeader = "Дата редакции";

    /// <summary>
    /// Маркер шапки таблицы (используется парсером для пропуска строки-разделителя).
    /// </summary>
    public const string MarkdownTableSeparatorMarker = ":---";

    // === Приложения (## Приложение N) ===
    public const string AppendixPrefix = "## Приложение ";
    public const string AppendixLegacyPrefix = "## Приложение А.";

    // === Подсекция-таблица внутри приложения (### Таблица № N) ===
    public const string TableLabelFormat = "Таблица {0}";
    public const string TableSubHeaderPrefix = "### ";

    // === Заголовки разделов иерархии ===
    /// <summary>Префикс «## » для заголовков второго уровня.</summary>
    public const string Level2Prefix = "## ";

    /// <summary>Префикс «### » для заголовков третьего уровня.</summary>
    public const string Level3Prefix = "### ";

    /// <summary>Префикс «# » для заголовков первого уровня.</summary>
    public const string Level1Prefix = "# ";

    // === Маркеры списков ===
    public const string BulletPrefix = "- ";
    public const string BoldDelimiter = "**";
    public const string EmDashSeparator = " — ";
    public const string WikilinkOpen = "[[";
    public const string WikilinkClose = "]]";

    // === Разделители ===
    /// <summary>Маркер начала code-fence (парсер останавливается на нём).</summary>
    public const string CodeFenceMarker = "```";

    // === Нумерация (инварианты) ===
    /// <summary>Первый верхний уровень пользовательских секций (после Цель/Область/Термины).</summary>
    public const int FirstUserSectionNumber = 4;

    /// <summary>Минимальная длина маркера «## » (3 символа).</summary>
    public const int Level2PrefixLength = 3;

    /// <summary>Минимальная длина маркера «### » (4 символа).</summary>
    public const int Level3PrefixLength = 4;

    // === Заголовки секций первого уровня (Title без номера) для парсера ===
    /// <summary>Title секции 1 — должен содержать «Цель».</summary>
    public const string PurposeTitleMarker = "Цель";

    /// <summary>Title секции 2 — должен содержать «Область».</summary>
    public const string ScopeTitleMarker = "Область";

    /// <summary>Title секции 3 — должен содержать «Термин».</summary>
    public const string TermsTitleMarker = "Термин";

    /// <summary>Title подсекции «### Родительский документ» — должен содержать «Родительский».</summary>
    public const string ParentDocumentTitleMarker = "Родительский";

    /// <summary>Title подсекции «### Связанные документы» — должен содержать «Связанные».</summary>
    public const string RelatedDocumentsTitleMarker = "Связанные";

    /// <summary>Маркер подсекции первичного обучения.</summary>
    public const string PrimaryTrainingMarker = "Первичное";

    /// <summary>Маркер подсекции повторного обучения.</summary>
    public const string RecurrentTrainingMarker = "Повторное";

    /// <summary>Маркер заголовка приложения (для SplitAppendixSections).</summary>
    public const string TableTitleMarker = "Таблица";

    /// <summary>Маркер вторичной разметки (для SplitAppendixSections).</summary>
    public const string UnderscoreMarker = "_";

    // === Регулярные выражения (компилируются один раз) ===

    private static readonly Lazy<Regex> SectionHeaderLazy = new(
        () => new(@"^##\s+(\d+(?:\.\d+)*)\s+(.+?)\s*$", RegexOptions.Multiline | RegexOptions.Compiled));

    private static readonly Lazy<Regex> AppendixHeaderLazy = new(
        () => new(@"^##\s+Приложение\s+(\d+)\s*$", RegexOptions.Multiline | RegexOptions.Compiled));

    private static readonly Lazy<Regex> SubHeaderLazy = new(
        () => new(@"^###\s+(.+?)\s*$", RegexOptions.Multiline | RegexOptions.Compiled));

    private static readonly Lazy<Regex> AppendixTableLazy = new(
        () => new(@"^###\s+Таблица\s+№\s+(\d+)(?::\s*(.+))?\s*$", RegexOptions.Multiline | RegexOptions.Compiled));

    private static readonly Lazy<Regex> BulletTermLazy = new(
        () => new(@"^-\s+\*\*(.+?)\*\*\s+—\s+(.+?)\s*$", RegexOptions.Multiline | RegexOptions.Compiled));

    private static readonly Lazy<Regex> BulletPlainLazy = new(
        () => new(@"^-\s+(.+?)\s*$", RegexOptions.Multiline | RegexOptions.Compiled));

    /// <summary>Заголовок иерархической секции: «## 4», «## 4.1», «## 4.1.1».</summary>
    public static Regex SectionHeader => SectionHeaderLazy.Value;

    /// <summary>Заголовок приложения: «## Приложение N».</summary>
    public static Regex AppendixHeader => AppendixHeaderLazy.Value;

    /// <summary>Произвольный подзаголовок: «### text».</summary>
    public static Regex SubHeader => SubHeaderLazy.Value;

    /// <summary>Заголовок таблицы приложения: «### Таблица № N: title».</summary>
    public static Regex AppendixTable => AppendixTableLazy.Value;

    /// <summary>Bullet-строка термина: «- **Term** — Definition».</summary>
    public static Regex BulletTerm => BulletTermLazy.Value;

    /// <summary>Простая bullet-строка: «- value».</summary>
    public static Regex BulletPlain => BulletPlainLazy.Value;

    // === Хелперы генерации заголовков ===

    /// <summary>Строит заголовок приложения с заданным номером: «## Приложение N».</summary>
    public static string FormatAppendixHeader(int number) => $"{AppendixPrefix}{number}";

    /// <summary>Строит заголовок таблицы внутри приложения: «### Таблица N» или «### Таблица N: title».</summary>
    public static string FormatTableSubHeader(int number, string? title = null)
    {
        var label = string.Format(System.Globalization.CultureInfo.InvariantCulture, TableLabelFormat, number);
        var titlePart = string.IsNullOrWhiteSpace(title) ? string.Empty : $": {title}";
        return $"{TableSubHeaderPrefix}{label}{titlePart}";
    }
}