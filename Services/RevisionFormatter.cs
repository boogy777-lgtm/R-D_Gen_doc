namespace AvaloniaApp.Services;

/// <summary>
/// Форматирование номера редакции документа для отображения в UI и DOCX.
///
/// Single source of truth — все остальные компоненты (ChangeLogRow, DocumentBodyToMarkdown,
/// DocumentChangeEngine, DocxGenerator) вызывают <see cref="Trim"/> вместо дублирования
/// логики обрезки «.0».
/// </summary>
public static class RevisionFormatter
{
    /// <summary>
    /// Обрезает суффикс «.0» у номера редакции:
    /// <list type="bullet">
    ///   <item>«1.0» → «1»</item>
    ///   <item>«2.5» → «2.5»</item>
    ///   <item>«3.10» → «3.10»</item>
    ///   <item>«1.0.0» → «1.0»</item>
    /// </list>
    /// </summary>
    public static string Trim(string revision)
    {
        if (string.IsNullOrWhiteSpace(revision))
        {
            return string.Empty;
        }

        return revision.EndsWith(".0", System.StringComparison.Ordinal)
            ? revision[..^2]
            : revision;
    }

    /// <summary>
    /// Извлекает целую часть номера редакции (до первой точки).
    /// <list type="bullet">
    ///   <item>«1» → 1</item>
    ///   <item>«1.0» → 1</item>
    ///   <item>«2.5» → 2</item>
    ///   <item>«3.10» → 3</item>
    ///   <item>«» / «ДД.ММ.ГГГГ» / null → 0</item>
    /// </list>
    /// </summary>
    public static int ParseInteger(string? revision)
    {
        if (string.IsNullOrWhiteSpace(revision))
        {
            return 0;
        }

        int dotIndex = revision.IndexOf('.');
        string intPart = dotIndex > 0 ? revision[..dotIndex] : revision;
        return int.TryParse(intPart, out int result) ? result : 0;
    }

    /// <summary>
    /// Возвращает следующий номер редакции: целая часть + 1.
    /// <code>«1» → «2», «2.5» → «3», «»/null → «1»</code>
    /// </summary>
    public static string Increment(string? revision)
    {
        int next = ParseInteger(revision) + 1;
        return next.ToString();
    }
}
