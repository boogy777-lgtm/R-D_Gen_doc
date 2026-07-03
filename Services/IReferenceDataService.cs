using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис справочников: типы документов, подразделения, дисциплины, роли.
/// Реализация — <see cref="ReferenceDataService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Интерфейс выделен из static-класса <c>ReferenceDataService</c> для соблюдения DIP:
/// MainWindowViewModel и другие координаторы зависят от абстракции, не от
/// конкретного класса с прямым доступом к диску/SQLite.
/// </para>
/// <para>
/// Default-коллекции (<see cref="DefaultDocTypesFull"/>, <see cref="DefaultDepartmentsFull"/>,
/// <see cref="DefaultDisciplinesFull"/>, <see cref="Statuses"/>, <see cref="ApproverRoles"/>,
/// <see cref="Disciplines"/>) — канонические списки из
/// <see cref="Defaults.ReferenceData"/>, используются как fallback при пустой папке
/// <c>docs/_справочники/</c> (cold start, удалённая папка).
/// </para>
/// </remarks>
public interface IReferenceDataService
{
    /// <summary>
    /// Сообщение статус-бара, выставляемое после последнего <see cref="SeedIfEmptyAsync"/>.
    /// </summary>
    string SeedStatusMessage { get; }

    /// <summary>
    /// Возвращает все типы документов из <c>docs/_справочники/типы_документов/*.md</c>.
    /// </summary>
    Task<IReadOnlyList<DocType>> GetDocTypesAsync();

    /// <summary>
    /// Возвращает все подразделения из <c>docs/_справочники/подразделения/*.md</c>.
    /// </summary>
    Task<IReadOnlyList<Department>> GetDepartmentsAsync();

    /// <summary>
    /// Возвращает роль по коду или null, если не найдена.
    /// </summary>
    Task<DocType?> GetDocTypeAsync(string code);

    /// <summary>
    /// Возвращает подразделение по коду или null, если не найдено.
    /// </summary>
    Task<Department?> GetDepartmentAsync(string code);

    /// <summary>
    /// Возвращает список ролей утверждающих из <c>docs/_справочники/роли/*.md</c>.
    /// </summary>
    Task<IReadOnlyList<string>> GetApproverRolesAsync();

    /// <summary>
    /// Возвращает канонические коды дисциплин (из папки + встроенный fallback).
    /// </summary>
    Task<IReadOnlyList<string>> GetDisciplineCodesAsync();

    /// <summary>
    /// Сидит SQLite <c>template.db</c> и генерирует <c>docs/_справочники/</c>, если они пусты.
    /// </summary>
    Task SeedIfEmptyAsync();

    /// <summary>
    /// Полный канонический список типов документов (34 типа по ИСМ BSD-RUS-BUR-QHSE-401).
    /// Источник: <see cref="Defaults.ReferenceData.DocTypes"/>.
    /// </summary>
    IReadOnlyList<DocType> DefaultDocTypesFull();

    /// <summary>
    /// Полная каноническая оргструктура (42 подразделения по WM-RUS-BUR-QHSE-401A).
    /// Источник: <see cref="Defaults.ReferenceData.Departments"/>.
    /// </summary>
    IReadOnlyList<Department> DefaultDepartmentsFull();

    /// <summary>
    /// Полный канонический список дисциплин.
    /// Источник: <see cref="Defaults.ReferenceData.Disciplines"/>.
    /// </summary>
    IReadOnlyList<string> DefaultDisciplinesFull();

    /// <summary>
    /// Канонический список статусов документов.
    /// Источник: <see cref="Defaults.ReferenceData.Statuses"/>.
    /// </summary>
    IReadOnlyList<string> Statuses { get; }

    /// <summary>
    /// Канонический список ролей утверждающих.
    /// Источник: <see cref="Defaults.ReferenceData.ApproverRoles"/>.
    /// </summary>
    IReadOnlyList<string> ApproverRoles { get; }

    /// <summary>
    /// Канонический список дисциплин (группы документов по тематике).
    /// Источник: <see cref="Defaults.ReferenceData.Disciplines"/>.
    /// </summary>
    IReadOnlyList<string> Disciplines { get; }

    /// <summary>
    /// Путь к корню <c>docs/_справочники/</c>.
    /// </summary>
    string GetRefStoreDir();
}