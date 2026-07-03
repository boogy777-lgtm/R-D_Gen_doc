// ============================================================
// Backward-compat static-фасад к Services/FormsCatalog.
//
// Production-код должен инжектировать IFormsCatalog через конструктор.
// Этот фасад существует для исторических callsites (NumberingEngine,
// DocumentBodyToMarkdown, тесты), которые вызывают Forms.GetTemplate(...)
// напрямую.
// ============================================================

using AvaloniaApp.Models;

namespace AvaloniaApp.Services.Defaults;

/// <summary>
/// Legacy static-фасад к <see cref="Services.FormsCatalog"/>.
/// Сохраняет оригинальные имена методов для обратной совместимости
/// с существующим кодом.
/// </summary>
public static class Forms
{
    /// <summary>
    /// Возвращает шаблон формы для указанного DocType.
    /// </summary>
    public static FormTemplate GetTemplate(string? docTypeCode) =>
        FormsCatalog.StaticGetTemplate(docTypeCode);
}