// ============================================================
// Backward-compat static-фасад к Services/MetadataCatalog.
//
// Production-код должен инжектировать IMetadataCatalog через конструктор.
// Этот фасад существует для исторических callsites, которые
// вызывают Defaults.Metadata.All / ApplyTemplateDefaults напрямую.
// ============================================================

using System.Collections.Generic;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services.Defaults;

/// <summary>
/// Legacy static-фасад к <see cref="Services.MetadataCatalog"/>.
/// Сохраняет оригинальные имена методов для обратной совместимости
/// с существующим кодом (MarkdownService, MainWindowViewModel, тесты).
/// </summary>
public static class Metadata
{
    /// <summary>
    /// Все 22 поля метаданных с дескрипторами.
    /// </summary>
    public static IReadOnlyList<MetadataFieldDescriptor> All =>
        Services.MetadataCatalog.StaticAll;

    /// <summary>
    /// Применяет <see cref="MetadataFieldDescriptor.TemplateDefault"/> к пустым
    /// полям метаданных согласно <see cref="FieldPermission"/> каждого дескриптора.
    /// </summary>
    public static void ApplyTemplateDefaults(DocumentMetadata metadata) =>
        Services.MetadataCatalog.StaticApplyTemplateDefaults(metadata);

    /// <summary>
    /// Возвращает копию метаданных, в которой пустые строковые поля заменены
    /// на <see cref="MetadataFieldDescriptor.Placeholder"/> или TemplateDefault.
    /// </summary>
    public static DocumentMetadata ToEffectiveMetadata(DocumentMetadata metadata) =>
        Services.MetadataCatalog.StaticToEffectiveMetadata(metadata);
}