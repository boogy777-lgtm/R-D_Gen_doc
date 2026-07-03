// ============================================================
// MetadataFieldDescriptor — generic helpers для DRY-генерации 22 полей метаданных.
//
// Раньше: 22 ручных new MetadataFieldDescriptor { Key = ..., YamlKey = ...,
//   ValueGetter = m => m.FieldName, ValueSetter = (m, v) => m.FieldName = v } —
//   копипаст ~100 строк с риском рассинхрона.
//
// Теперь: декларативные вызовы CreateStringField / CreateBoolField —
//   один источник истины (имя поля), компилятор проверяет,
//   что getter/setter используют существующие свойства.
//
// YamlKey удалён: он не читался ни одним callsite (dead code).
// ============================================================

using System;
using System.Collections.Generic;
using AvaloniaApp.Models;
using AvaloniaApp.Services.Defaults;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IMetadataCatalog"/>.
/// </summary>
/// <remarks>
/// <para>
/// Содержит как instance-методы (для DI через <see cref="IMetadataCatalog"/>),
/// так и static-фасады (для обратной совместимости с MarkdownService,
/// MainWindowViewModel и тестами).
/// </para>
/// </remarks>
public sealed class MetadataCatalog : IMetadataCatalog
{
    private static readonly Lazy<MetadataCatalog> InstanceLazy = new(() => new MetadataCatalog());

    private readonly IReadOnlyList<MetadataFieldDescriptor> allDescriptors = BuildAll();

    /// <inheritdoc />
    public IReadOnlyList<MetadataFieldDescriptor> All => allDescriptors;

    /// <summary>
    /// Создаёт новый экземпляр каталога (для DI).
    /// </summary>
    public MetadataCatalog()
    {
    }

    /// <inheritdoc />
    public void ApplyTemplateDefaults(DocumentMetadata metadata)
    {
        if (metadata is null)
        {
            return;
        }

        foreach (var descriptor in allDescriptors)
        {
            if (descriptor.Permission != FieldPermission.Static
                && descriptor.Permission != FieldPermission.Mutable)
            {
                continue;
            }

            // Заполняем только если пользователь очистил поле.
            var currentValue = descriptor.ValueGetter(metadata);
            if (string.IsNullOrWhiteSpace(currentValue) && descriptor.TemplateDefault is not null)
            {
                descriptor.ValueSetter(metadata, descriptor.TemplateDefault);
            }
        }
    }

    /// <inheritdoc />
    public DocumentMetadata ToEffectiveMetadata(DocumentMetadata metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var effective = CloneMetadata(metadata);

        foreach (var descriptor in allDescriptors)
        {
            if (descriptor.IsBool)
            {
                continue;
            }

            var currentValue = descriptor.ValueGetter(effective);
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                continue;
            }

            var fallback = descriptor.Placeholder ?? descriptor.TemplateDefault;
            if (fallback is not null)
            {
                descriptor.ValueSetter(effective, fallback);
            }
        }

        return effective;
    }

    // === DRY-хелперы для генерации 22 дескрипторов ========================
    //
    // Каждый хелпер инкапсулирует пару getter/setter с правильной сигнатурой.
    // При добавлении нового поля — одна строка вызова, без копипаста.

    /// <summary>
    /// Декларативно создаёт string-поле метаданных. Имя свойства проверяется компилятором.
    /// </summary>
    private static MetadataFieldDescriptor CreateStringField(
        string key,
        FieldPermission permission,
        Func<DocumentMetadata, string> getter,
        Action<DocumentMetadata, string> setter,
        string? templateDefault = null,
        string? placeholder = null)
    {
        return new MetadataFieldDescriptor
        {
            Key = key,
            Permission = permission,
            TemplateDefault = templateDefault,
            Placeholder = placeholder,
            ValueGetter = getter,
            ValueSetter = setter,
            IsBool = false,
        };
    }

    /// <summary>
    /// Декларативно создаёт bool-поле метаданных (сериализуется как «true»/«false»).
    /// </summary>
    private static MetadataFieldDescriptor CreateBoolField(
        string key,
        Func<DocumentMetadata, bool> getter,
        Action<DocumentMetadata, bool> setter)
    {
        return new MetadataFieldDescriptor
        {
            Key = key,
            Permission = FieldPermission.Mutable,
            ValueGetter = m => getter(m) ? "true" : "false",
            ValueSetter = (m, v) => setter(m, bool.TryParse(v, out var b) && b),
            IsBool = true,
        };
    }

    private static IReadOnlyList<MetadataFieldDescriptor> BuildAll() => new MetadataFieldDescriptor[]
    {
        // ── Основная фабричная схема ──
        CreateStringField("DocType", FieldPermission.Static, m => m.DocType, (m, v) => m.DocType = v, templateDefault: ReferenceData.MetadataDefaults.DocType),
        CreateStringField("Title", FieldPermission.Mutable, m => m.Title, (m, v) => m.Title = v, placeholder: "Полное название документа"),
        CreateStringField("DocId", FieldPermission.Mutable, m => m.DocId, (m, v) => m.DocId = v, placeholder: "например: ST-RUS-BUR-QHSE-001"),
        CreateStringField("DepartmentName", FieldPermission.Mutable, m => m.DepartmentName, (m, v) => m.DepartmentName = v, templateDefault: ReferenceData.MetadataDefaults.Department),
        CreateStringField("Revision", FieldPermission.Mutable, m => m.Revision, (m, v) => m.Revision = v, templateDefault: ReferenceData.MetadataDefaults.Revision),
        CreateStringField("Status", FieldPermission.Mutable, m => m.Status, (m, v) => m.Status = v, templateDefault: ReferenceData.MetadataDefaults.Status),
        CreateStringField("EffectiveDate", FieldPermission.Mutable, m => m.EffectiveDate, (m, v) => m.EffectiveDate = v, placeholder: "ДД.ММ.ГГГГ"),
        CreateStringField("Author", FieldPermission.Static, m => m.Author, (m, v) => m.Author = v, placeholder: "ФИО автора последней редакции"),
        CreateStringField("Owner", FieldPermission.Mutable, m => m.Owner, (m, v) => m.Owner = v, templateDefault: ReferenceData.MetadataDefaults.Owner),
        CreateStringField("Approver", FieldPermission.Mutable, m => m.Approver, (m, v) => m.Approver = v, placeholder: "ФИО утверждающего"),
        CreateStringField("ApproverRole", FieldPermission.Mutable, m => m.ApproverRole, (m, v) => m.ApproverRole = v, templateDefault: ReferenceData.MetadataDefaults.ApproverRole),
        CreateStringField("Protocol", FieldPermission.Mutable, m => m.Protocol, (m, v) => m.Protocol = v, placeholder: "например: PR-2026-042"),
        CreateStringField("FooterText", FieldPermission.Mutable, m => m.FooterText, (m, v) => m.FooterText = v, templateDefault: ReferenceData.MetadataDefaults.FooterText),

        // ── Опциональные сегменты шифра ──
        CreateBoolField("DivisionEnabled", m => m.DivisionEnabled, (m, v) => m.DivisionEnabled = v),
        CreateStringField("DivisionCode", FieldPermission.Mutable, m => m.DivisionCode, (m, v) => m.DivisionCode = v, placeholder: "напр. SD-NNB"),
        CreateBoolField("DisciplineEnabled", m => m.DisciplineEnabled, (m, v) => m.DisciplineEnabled = v),
        CreateStringField("DisciplineCode", FieldPermission.Mutable, m => m.DisciplineCode, (m, v) => m.DisciplineCode = v, placeholder: "напр. PRO"),
        CreateBoolField("IndexEnabled", m => m.IndexEnabled, (m, v) => m.IndexEnabled = v),
        CreateStringField("IndexCode", FieldPermission.Mutable, m => m.IndexCode, (m, v) => m.IndexCode = v, placeholder: "напр. A или A-1"),
        CreateBoolField("LanguageEnabled", m => m.LanguageEnabled, (m, v) => m.LanguageEnabled = v),
        CreateStringField("LanguageCode", FieldPermission.Mutable, m => m.LanguageCode, (m, v) => m.LanguageCode = v, placeholder: "напр. ENG"),
    };

    private static DocumentMetadata CloneMetadata(DocumentMetadata source)
    {
        return new DocumentMetadata
        {
            DocId = source.DocId,
            DocType = source.DocType,
            Title = source.Title,
            DepartmentName = source.DepartmentName,
            Revision = source.Revision,
            Status = source.Status,
            EffectiveDate = source.EffectiveDate,
            Author = source.Author,
            Owner = source.Owner,
            Approver = source.Approver,
            ApproverRole = source.ApproverRole,
            Protocol = source.Protocol,
            FooterText = source.FooterText,
            DivisionEnabled = source.DivisionEnabled,
            DivisionCode = source.DivisionCode,
            DisciplineEnabled = source.DisciplineEnabled,
            DisciplineCode = source.DisciplineCode,
            IndexEnabled = source.IndexEnabled,
            IndexCode = source.IndexCode,
            LanguageEnabled = source.LanguageEnabled,
            LanguageCode = source.LanguageCode,
            ExtraYaml = new Dictionary<string, object?>(source.ExtraYaml ?? new Dictionary<string, object?>()),
        };
    }

    // === Static facade для обратной совместимости ===

    private static MetadataCatalog Instance => InstanceLazy.Value;

    /// <summary>
    /// Legacy static accessor: <c>Metadata.All</c>.
    /// </summary>
    public static IReadOnlyList<MetadataFieldDescriptor> StaticAll => Instance.All;

    /// <summary>
    /// Legacy static accessor: <c>Metadata.ApplyTemplateDefaults(meta)</c>.
    /// </summary>
    public static void StaticApplyTemplateDefaults(DocumentMetadata metadata) =>
        Instance.ApplyTemplateDefaults(metadata);

    /// <summary>
    /// Legacy static accessor: <c>Metadata.ToEffectiveMetadata(meta)</c>.
    /// </summary>
    public static DocumentMetadata StaticToEffectiveMetadata(DocumentMetadata metadata) =>
        Instance.ToEffectiveMetadata(metadata);
}