using System;
using AvaloniaApp.Services.DocxGeneration;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Конвертер <see cref="CodeBlock"/> в параграф с моноширинным шрифтом.
/// Поддерживает спец-случай: <c>```mermaid</c> при <c>profile.RenderMermaidAsMonospace</c>.
/// </summary>
internal sealed class CodeBlockConverter : IBlockConverter
{
    public void Convert(Block block, Body body, DocxContext ctx)
    {
        var code = (CodeBlock)block;
        var profile = ctx.Profile;

        bool monospace = profile.RenderMermaidAsMonospace
            && code is FencedCodeBlock fcb
            && string.Equals(fcb.Info, "mermaid", StringComparison.OrdinalIgnoreCase);

        body.Append(DocxRunHelpers.ConvertCodeBlock(code, profile, monospace));
    }
}
