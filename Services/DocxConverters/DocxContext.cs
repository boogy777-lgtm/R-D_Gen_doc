using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;

namespace AvaloniaApp.Services.DocxConverters;

/// <summary>
/// Контекст DOCX-генерации, разделяемый между всеми <see cref="IBlockConverter"/>.
/// Содержит общие зависимости (профиль) и допускает будущее расширение
/// (например, кеш стилей, счётчик изображений).
/// </summary>
internal sealed record DocxContext(DocxProfile Profile)
{
    // Зарезервировано для будущих расширений:
    // - стили (кеш)
    // - счётчик изображений
    // - шрифты (кеш)
}
