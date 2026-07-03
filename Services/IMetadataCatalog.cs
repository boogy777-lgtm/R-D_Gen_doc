using System;
using System.Collections.Generic;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Каталог дескрипторов полей метаданных с Permission + TemplateDefault.
/// Единый источник истины для CreateDefaultMetadata и fallback-ов при пустых полях.
/// </summary>
/// <remarks>
/// <para>
/// Интерфейс выделен из static-класса <c>Defaults.Metadata</c> для соблюдения DIP.
/// Production-код (MainWindowViewModel, MarkdownService, DocumentMetadataCoordinator)
/// зависят от абстракции, а не от конкретного класса.
/// </para>
/// </remarks>
public interface IMetadataCatalog
{
    /// <summary>
    /// Все дескрипторы полей метаданных (22 поля).
    /// </summary>
    IReadOnlyList<MetadataFieldDescriptor> All { get; }

    /// <summary>
    /// Применяет <see cref="MetadataFieldDescriptor.TemplateDefault"/> к пустым полям
    /// метаданных согласно <see cref="FieldPermission"/> каждого дескриптора.
    /// Не перезаписывает непустые значения.
    /// </summary>
    void ApplyTemplateDefaults(DocumentMetadata metadata);

    /// <summary>
    /// Возвращает копию метаданных, в которой пустые строковые поля заменены
    /// на <see cref="MetadataFieldDescriptor.Placeholder"/> (или
    /// <see cref="MetadataFieldDescriptor.TemplateDefault"/>).
    /// Исходный объект не мутируется.
    /// </summary>
    DocumentMetadata ToEffectiveMetadata(DocumentMetadata metadata);
}