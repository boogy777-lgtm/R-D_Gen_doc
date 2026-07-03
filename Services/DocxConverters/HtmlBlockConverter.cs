using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="Markdig.Syntax.HtmlBlock"/> (raw HTML в Markdown).
/// В DOCX HTML не рендерится — представляется пустым параграфом.
/// Идентично поведению исходного switch (case отсутствует → ничего не добавляется).
/// </summary>
internal sealed class HtmlBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        // HTML в DOCX не поддерживается — сохраняем spacing через пустой параграф,
        // как и для всех неизвестных типов (default branch исходного switch).
        body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "120" })));
    }
}
