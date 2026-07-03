using System.Linq;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Распознаватели специальных таблиц и цитат в Markdown:
/// «Лист регистрации изменений» (ЛРИ) и callout-цитата <c>[!shield]</c>.
/// Вынесены из <c>DocxGenerator</c> для устранения legacy-coupling
/// (используются <see cref="DocxConverters.TableBlockConverter"/> и
/// <see cref="DocxConverters.QuoteBlockConverter"/>).
/// </summary>
internal static class DocxBlockRecognizers
{
    /// <summary>
    /// Распознаёт таблицу «Лист регистрации изменений» по содержимому
    /// первой строки (должна содержать подстроку <c>«Дата ред.»</c>).
    /// </summary>
    public static bool IsChangeLogTable(Table mdTable)
    {
        if (mdTable.Count == 0)
        {
            return false;
        }

        var firstRow = mdTable[0] as TableRow;
        if (firstRow is null)
        {
            return false;
        }

        var firstCell = firstRow.FirstOrDefault() as TableCell;
        if (firstCell is null)
        {
            return false;
        }

        var literal = firstCell.Descendants<LiteralInline>().FirstOrDefault();
        return literal is not null && literal.Content.ToString().Trim().Contains("Дата ред.");
    }

    /// <summary>
    /// Распознаёт callout-цитату <c>[!shield]</c> (первый LiteralInline
    /// начинается с этого маркера после TrimStart).
    /// </summary>
    public static bool IsShieldQuote(QuoteBlock quote)
    {
        var first = quote.Descendants<LiteralInline>().FirstOrDefault();
        if (first is null)
        {
            return false;
        }

        return first.Content.ToString().TrimStart()
            .StartsWith("[!shield]", System.StringComparison.OrdinalIgnoreCase);
    }
}
