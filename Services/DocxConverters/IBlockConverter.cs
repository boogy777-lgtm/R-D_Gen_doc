using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Strategy-интерфейс для конвертации Markdig-блоков в OpenXML (OCP).
/// Каждый конкретный блок (Heading, Paragraph, List, Table и т.д.) имеет
/// свой <see cref="IBlockConverter"/>, регистрируемый в реестре <c>DocxGenerator</c>.
/// </summary>
internal interface IBlockConverter
{
    /// <summary>
    /// Конвертирует блок и добавляет результат в <paramref name="body"/>.
    /// </summary>
    /// <param name="block">Исходный Markdig-блок (гарантированно нужного типа).</param>
    /// <param name="body">Тело OpenXML-документа, в которое добавляются элементы.</param>
    /// <param name="ctx">Контекст с профилем и общими зависимостями.</param>
    void Convert(Block block, Body body, DocxContext ctx);
}
