using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Обрабатывает Markdig <see cref="ContainerInline"/>-ы и добавляет их в
/// <see cref="Paragraph"/>. Рекурсивно обходит <c>EmphasisInline</c>,
/// <c>LinkInline</c> и <c>CodeInline</c> с наследованием свойств.
/// </summary>
/// <remarks>
/// Вынесен из <c>DocxGenerator</c> и <c>DocxRunHelpers</c> для устранения
/// legacy-coupling между <see cref="DocxConverters"/> и фасадом
/// <c>DocxGenerator</c>.
/// </remarks>
internal static class DocxInlinesProcessor
{
    /// <summary>
    /// Обходит <paramref name="container"/> и добавляет все inline-ы в <paramref name="paragraph"/>.
    /// </summary>
    /// <param name="paragraph">Целевой параграф OpenXML.</param>
    /// <param name="container">Markdig-контейнер inline-ов (null = no-op).</param>
    /// <param name="profile">Профиль документа (шрифт, размер).</param>
    /// <param name="inheritedProperties">
    /// Свойства, унаследованные от родительского inline-а (например, цвет ссылки
    /// внутри жирного текста). Клонируются для каждого дочернего run-а.
    /// </param>
    public static void Process(Paragraph paragraph, ContainerInline? container, DocxProfile profile, RunProperties? inheritedProperties = null)
    {
        if (container is null)
        {
            return;
        }

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                {
                    var literalRun = paragraph.AppendChild(new Run(new Text(literal.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve }));
                    var literalProps = inheritedProperties is not null
                        ? (RunProperties)inheritedProperties.CloneNode(true)
                        : DocxRunHelpers.CreateDefaultRunProperties(profile, profile.FontSizePt, false);
                    literalRun.PrependChild(literalProps);
                    break;
                }

                case LineBreakInline:
                    paragraph.AppendChild(new Run(new Break()));
                    break;

                case EmphasisInline emphasis:
                {
                    var emphasisProps = new RunProperties();
                    if (emphasis.DelimiterCount == 2)
                    {
                        emphasisProps.Append(new Bold());
                    }
                    else
                    {
                        emphasisProps.Append(new Italic());
                    }

                    if (inheritedProperties is not null)
                    {
                        emphasisProps.PrependChild(inheritedProperties.CloneNode(true));
                    }
                    else
                    {
                        DocxRunHelpers.EnsureFontInRunProperties(emphasisProps, profile, profile.FontSizePt, false);
                    }

                    Process(paragraph, emphasis, profile, emphasisProps);
                    break;
                }

                case LinkInline link:
                {
                    var linkProps = new RunProperties(
                        new Color { Val = "0563C1" },
                        new Underline { Val = UnderlineValues.Single });
                    if (inheritedProperties is not null)
                    {
                        linkProps.PrependChild(inheritedProperties.CloneNode(true));
                    }
                    else
                    {
                        DocxRunHelpers.EnsureFontInRunProperties(linkProps, profile, profile.FontSizePt, false);
                    }

                    Process(paragraph, link, profile, linkProps);
                    break;
                }

                case CodeInline code:
                {
                    var codeRun = paragraph.AppendChild(new Run(new Text(code.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve }));
                    var codeProps = new RunProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = "F0F0F0" });
                    DocxRunHelpers.EnsureFontInRunProperties(codeProps, profile, profile.FontSizePt, true);
                    codeRun.PrependChild(codeProps);
                    break;
                }
            }
        }
    }
}
