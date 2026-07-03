using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Координатор синхронизации метаданных: SelectedDocType ↔ DocType,
/// DepartmentCode → DepartmentName и список дочерних подразделений,
/// авто-генерация DocId из метаданных.
/// </summary>
/// <remarks>
/// <para>Зависимости (всё через конструктор, без DI-контейнера):</para>
/// <list type="bullet">
///   <item>Делегаты-геттеры состояния (<see cref="DocumentMetadata"/> + список подразделений +
///       коллекции SelectedDocType / ChildDepartments / DocTypes), чтобы координатор
///       оставался stateless и не держал ссылку на VM.</item>
///   <item>Делегаты-мутаторы, через которые координатор пишет в VM
///       (<c>setSelectedDocType</c>, <c>setSuppressDocTypeSync</c>,
///       <c>setChildDepartments</c>, <c>markDocIdManuallyEdited</c>).</item>
/// </list>
/// <para>Engine и формы не нужны координатору: вся работа — на уровне
/// <see cref="DocumentMetadata"/> и UI-коллекций.</para>
/// </remarks>
public sealed class MetadataSyncCoordinator
{
    private readonly Func<DocumentMetadata?> getMetadata;
    private readonly Func<IReadOnlyList<Department>> getAllDepartments;
    private readonly Func<DocType?> getSelectedDocType;
    private readonly Action<DocType?> setSelectedDocType;
    private readonly Action<bool> setSuppressDocTypeSync;
    private readonly Action<ObservableCollection<string>> setChildDepartments;
    private readonly Func<string> getDepartmentCode;
    private readonly Action markDocIdManuallyEdited;
    private readonly Action<string> tryProposeDocId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSyncCoordinator"/> class.
    /// </summary>
    /// <param name="getMetadata">Геттер текущих метаданных.</param>
    /// <param name="getAllDepartments">Геттер полного списка подразделений (для lookup).</param>
    /// <param name="getSelectedDocType">Геттер текущего выбранного <see cref="DocType"/>.</param>
    /// <param name="setSelectedDocType">Setter <see cref="DocType"/> в ComboBox «Тип документа».</param>
    /// <param name="setSuppressDocTypeSync">Setter guard-флага, блокирующего рекурсию setter-ов.</param>
    /// <param name="setChildDepartments">Сеттер списка child_id для ComboBox «Отдел».</param>
    /// <param name="getDepartmentCode">Геттер кода подразделения.</param>
    /// <param name="markDocIdManuallyEdited">Помечает DocId как отредактированный вручную.</param>
    /// <param name="tryProposeDocId">Запускает попытку авто-генерации DocId.</param>
    public MetadataSyncCoordinator(
        Func<DocumentMetadata?> getMetadata,
        Func<IReadOnlyList<Department>> getAllDepartments,
        Func<DocType?> getSelectedDocType,
        Action<DocType?> setSelectedDocType,
        Action<bool> setSuppressDocTypeSync,
        Action<ObservableCollection<string>> setChildDepartments,
        Func<string> getDepartmentCode,
        Action markDocIdManuallyEdited,
        Action<string> tryProposeDocId)
    {
        this.getMetadata = getMetadata ?? throw new ArgumentNullException(nameof(getMetadata));
        this.getAllDepartments = getAllDepartments ?? throw new ArgumentNullException(nameof(getAllDepartments));
        this.getSelectedDocType = getSelectedDocType ?? throw new ArgumentNullException(nameof(getSelectedDocType));
        this.setSelectedDocType = setSelectedDocType ?? throw new ArgumentNullException(nameof(setSelectedDocType));
        this.setSuppressDocTypeSync = setSuppressDocTypeSync ?? throw new ArgumentNullException(nameof(setSuppressDocTypeSync));
        this.setChildDepartments = setChildDepartments ?? throw new ArgumentNullException(nameof(setChildDepartments));
        this.getDepartmentCode = getDepartmentCode ?? throw new ArgumentNullException(nameof(getDepartmentCode));
        this.markDocIdManuallyEdited = markDocIdManuallyEdited ?? throw new ArgumentNullException(nameof(markDocIdManuallyEdited));
        this.tryProposeDocId = tryProposeDocId ?? throw new ArgumentNullException(nameof(tryProposeDocId));
    }

    /// <summary>
    /// Проверяет, влияет ли свойство с именем <paramref name="propertyName"/>
    /// на авто-предложение DocId.
    /// </summary>
    public static bool AffectsDocId(string? propertyName) =>
        propertyName is nameof(DocumentMetadata.DocType)
            or nameof(DocumentMetadata.DepartmentName)
            or nameof(DocumentMetadata.DivisionEnabled)
            or nameof(DocumentMetadata.DivisionCode)
            or nameof(DocumentMetadata.DisciplineEnabled)
            or nameof(DocumentMetadata.DisciplineCode)
            or nameof(DocumentMetadata.IndexEnabled)
            or nameof(DocumentMetadata.IndexCode)
            or nameof(DocumentMetadata.LanguageEnabled)
            or nameof(DocumentMetadata.LanguageCode);

    /// <summary>
    /// Реакция на смену <c>SelectedDocType</c> в ComboBox «Тип документа»:
    /// прокидывает <c>value.Code</c> в <see cref="DocumentMetadata.DocType"/>.
    /// Guard-флаг подавляет обратную запись <see cref="DocumentMetadata.DocType"/> →
    /// <c>SelectedDocType</c> на время программного обновления.
    /// </summary>
    public void OnSelectedDocTypeChanged(DocType? value)
    {
        var metadata = getMetadata();

        setSuppressDocTypeSync(true);
        try
        {
            if (metadata == null || value == null)
            {
                return;
            }

            if (string.Equals(metadata.DocType, value.Code, StringComparison.Ordinal))
            {
                return;
            }

            metadata.DocType = value.Code;
        }
        finally
        {
            setSuppressDocTypeSync(false);
        }
    }

    /// <summary>
    /// Пересинхронизирует <c>SelectedDocType</c> с <see cref="DocumentMetadata.DocType"/>;
    /// вызывается после in-place перезаписи справочника типов документов (Clear+Add)
    /// или при горячей перезагрузке из Obsidian.
    /// </summary>
    public void SyncSelectedDocTypeFromMetadata(IReadOnlyList<DocType> docTypes)
    {
        ArgumentNullException.ThrowIfNull(docTypes);

        var metadata = getMetadata();

        setSuppressDocTypeSync(true);
        try
        {
            if (metadata == null)
            {
                setSelectedDocType(null);
                return;
            }

            var match = docTypes.FirstOrDefault(d =>
                string.Equals(d.Code, metadata.DocType, StringComparison.Ordinal));

            if (ReferenceEquals(match, getSelectedDocType()))
            {
                return;
            }

            setSelectedDocType(match);
        }
        finally
        {
            setSuppressDocTypeSync(false);
        }
    }

    /// <summary>
    /// Реакция на смену <c>DepartmentCode</c> (ComboBox «Отдел»):
    /// синхронизирует <see cref="DocumentMetadata.DepartmentName"/> с <c>Department.NameRu</c>
    /// и обновляет список дочерних подразделений.
    /// </summary>
    public void OnDepartmentCodeChanged(string value)
    {
        var metadata = getMetadata();
        if (metadata == null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var allDepartments = getAllDepartments();
        var dept = allDepartments.FirstOrDefault(d =>
            string.Equals(d.Code, value, StringComparison.OrdinalIgnoreCase));
        if (dept != null && !string.IsNullOrWhiteSpace(dept.NameRu))
        {
            // Не перезаписываем если пользователь ввёл своё (отличается от NameRu).
if (string.IsNullOrWhiteSpace(metadata.DepartmentName)
                || metadata.DepartmentName == "НИОКР"
                || metadata.DepartmentName == "Подразделение НИОКР"
                || allDepartments.Any(d => d.NameRu == metadata.DepartmentName))
            {
                metadata.DepartmentName = dept.NameRu;
            }
        }

        RefreshChildDepartments(value);
    }

    /// <summary>
    /// Преобразует название подразделения (<see cref="DocumentMetadata.DepartmentName"/>)
    /// в КОД КОРНЕВОГО подразделения (<c>parent_id</c>) для использования в шифре документа.
    /// </summary>
    public string ResolveDepartmentCode(string departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return "QHSE";
        }

        var allDepartments = getAllDepartments();
        var dept = allDepartments.FirstOrDefault(d =>
            string.Equals(d.NameRu, departmentName, StringComparison.OrdinalIgnoreCase));
        if (dept != null && !string.IsNullOrWhiteSpace(dept.Code))
        {
            // Если выбрано дочернее подразделение — возвращаем код родителя,
            // потому что шифр содержит parent в основной части, а child_id — в DivisionCode.
            if (!dept.IsRoot && !string.IsNullOrWhiteSpace(dept.ParentCode))
            {
                return dept.ParentCode;
            }

            return dept.Code;
        }

        // Для обратной совместимости: если в поле сохранён старый код домена — используем его.
        if (departmentName.Length <= 10 && departmentName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
        {
            var dashIdx = departmentName.IndexOf('-');
            if (dashIdx > 0)
            {
                return departmentName.Substring(0, dashIdx).ToUpperInvariant();
            }

            return departmentName.ToUpperInvariant();
        }

        return "QHSE";
    }

    /// <summary>
    /// Обновляет список дочерних подразделений (функциональных направлений)
    /// для ComboBox «Отдел / функциональное направление». In-place мутация
    /// (Clear+Add) — не пересоздаёт <see cref="ObservableCollection{T}"/>, чтобы
    /// сохранить выбранный элемент и подписчиков CollectionChanged.
    /// </summary>
    public void RefreshChildDepartments(string parentCode)
    {
        var children = ObservableCollectionExtensions.ChildIdsOf(getAllDepartments(), parentCode);
        setChildDepartments(new ObservableCollection<string>(children));
    }

    /// <summary>
    /// Собирает полный шифр документа из метаданных через
    /// <see cref="DocumentNumberingService.BuildDocId"/>. Возвращает пустую
    /// строку, если метаданные или DocType пустые.
    /// </summary>
    public string BuildDocId()
    {
        var metadata = getMetadata();
        if (metadata is null || string.IsNullOrWhiteSpace(metadata.DocType))
        {
            return string.Empty;
        }

        var domainCode = ResolveDepartmentCode(metadata.DepartmentName);
        return DocumentNumberingService.BuildDocId(metadata, domainCode);
    }

    /// <summary>
    /// Реакция на <see cref="DocumentMetadata.PropertyChanged"/>: если изменился
    /// один из полей, влияющих на DocId, и пользователь НЕ редактировал его вручную —
    /// пересчитывает предложение.
    /// </summary>
    public void OnMetadataPropertyChangedForDocId(object? sender, PropertyChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (AffectsDocId(e.PropertyName))
        {
            tryProposeDocId(string.Empty);
        }
    }

    /// <summary>
    /// Возвращает ТОЛЬКО child_id (без префикса родителя) для использования
    /// в ComboBox «Отдел / функциональное направление». Например, для
    /// Department(Code="SD-NNB", ParentCode="SD") → "NNB".
    /// </summary>
}
