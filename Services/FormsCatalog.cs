using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using AvaloniaApp.Services.Defaults;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IFormsCatalog"/>.
/// Создаёт <see cref="FormTemplate"/> с дескрипторами полей, не зависящими
/// от конкретных Avalonia-контролов.
/// </summary>
/// <remarks>
/// <para>
/// Класс содержит как instance-метод <see cref="GetTemplate"/> (для DI),
/// так и static-фасад <see cref="GetTemplate(string?)"/> (для обратной
/// совместимости с DocumentChangeEngine, DocumentOpenCoordinator и тестами).
/// </para>
/// </remarks>
public sealed class FormsCatalog : IFormsCatalog
{
    private static readonly Lazy<FormsCatalog> InstanceLazy = new(() => new FormsCatalog());

    /// <summary>
    /// Создаёт новый экземпляр каталога (для DI).
    /// </summary>
    public FormsCatalog()
    {
    }

    /// <inheritdoc />
    public FormTemplate GetTemplate(string? docTypeCode)
    {
        return new FormTemplate
        {
            Name = $"DocumentForm[{docTypeCode ?? "Default"}]",
            HeaderFields = BuildHeaderFields(),
            CenterFields = new List<FormFieldDescriptor>
            {
                new TreeFieldDescriptor
                {
                    Key = "Sections",
                    Label = "Основное содержание (произвольные секции)",
                    IsRequired = true,
                    Placeholder = "Опишите содержание раздела",
                    TemplateDefault = "Раздел без заголовка — заполните описание.",
                    HelpText = "Каждая секция — заголовок + тело. Нумерация присваивается автоматически.",
                    Permission = FieldPermission.Mutable,
                },
            },
            TrailingFields = BuildTrailingFields(),
            StarterSections = GetStarterSections(docTypeCode),
        };
    }

    private static List<FormFieldDescriptor> BuildHeaderFields() => new()
    {
        new SingleFieldDescriptor
        {
            Key = "Purpose",
            Label = "1. Цель",
            IsRequired = true,
            IsMultiline = true,
            Placeholder = "Опишите цель документа",
            Permission = FieldPermission.Mutable,
            TemplateDefault = "Опишите цель документа",
            ValueGetter = b => b.Purpose,
            ValueSetter = (b, v, e) =>
            {
                var safe = v ?? string.Empty;
                if (e != null)
                {
                    e.SetPurpose(safe);
                }
                else
                {
                    b.Purpose = safe;
                }
            },
        },
        new SingleFieldDescriptor
        {
            Key = "Scope",
            Label = "2. Область применения",
            IsRequired = true,
            IsMultiline = true,
            Placeholder = "Где и к кому применяется",
            Permission = FieldPermission.Mutable,
            TemplateDefault = "Где и к кому применяется",
            ValueGetter = b => b.Scope,
            ValueSetter = (b, v, e) =>
            {
                var safe = v ?? string.Empty;
                if (e != null)
                {
                    e.SetScope(safe);
                }
                else
                {
                    b.Scope = safe;
                }
            },
        },
        new CollectionFieldDescriptor<TermPair>
        {
            Key = "Terms",
            Label = "3. Термины и определения",
            Placeholder = "Термин — кратко; определение — полная формулировка",
            TemplateDefault = "Термин — кратко; определение — полная формулировка",
            HelpText = "Список терминов с определениями. При пустом списке в MD выводится placeholder.",
            Permission = FieldPermission.Mutable,
            ItemsSourceGetter = b => b.Terms,
            AddAction = (b, e) =>
            {
                if (e != null)
                {
                    e.AddTerm();
                }
                else
                {
                    b.Terms.Add(DocumentBodyFactory.CreateEmptyTermPair());
                }
            },
            RemoveAction = (item, b, e) =>
            {
                if (e != null)
                {
                    e.RemoveTerm(item);
                }
                else
                {
                    b.Terms.Remove(item);
                }
            },
        },
    };

    private static List<FormFieldDescriptor> BuildTrailingFields() => new()
    {
        new SingleFieldDescriptor
        {
            Key = "PrimaryTraining",
            Label = "Первичное обучение",
            IsMultiline = true,
            Placeholder = "Вид и срок первичного обучения",
            Permission = FieldPermission.Mutable,
            TemplateDefault = "Вводный инструктаж.",
            ValueGetter = b => b.PrimaryTraining,
            ValueSetter = (b, v, e) =>
            {
                var safe = v ?? string.Empty;
                if (e != null)
                {
                    e.SetPrimaryTraining(safe);
                }
                else
                {
                    b.PrimaryTraining = safe;
                }
            },
        },
        new SingleFieldDescriptor
        {
            Key = "RecurrentTraining",
            Label = "Повторное обучение",
            IsMultiline = true,
            Placeholder = "Вид и периодичность повторного обучения",
            Permission = FieldPermission.Mutable,
            TemplateDefault = "Ежегодная проверка знаний.",
            ValueGetter = b => b.RecurrentTraining,
            ValueSetter = (b, v, e) =>
            {
                var safe = v ?? string.Empty;
                if (e != null)
                {
                    e.SetRecurrentTraining(safe);
                }
                else
                {
                    b.RecurrentTraining = safe;
                }
            },
        },
        new SingleFieldDescriptor
        {
            Key = "ParentDocument",
            Label = "Родительский документ",
            IsMultiline = false,
            Placeholder = "BSD-RUS-BUR-QHSE-401",
            Permission = FieldPermission.Mutable,
            TemplateDefault = "BSD-RUS-BUR-QHSE-401",
            ValueGetter = b => b.ParentDocument,
            ValueSetter = (b, v, e) =>
            {
                var safe = v ?? string.Empty;
                if (e != null)
                {
                    e.SetParentDocument(safe);
                }
                else
                {
                    b.ParentDocument = safe;
                }
            },
        },
        new CollectionFieldDescriptor<StringItem>
        {
            Key = "RelatedDocuments",
            Label = "Связанные документы",
            Placeholder = "ST-RUS-BUR-QHSE-0601",
            Permission = FieldPermission.Mutable,
            TemplateDefault = "ST-RUS-BUR-QHSE-0601",
            ItemsSourceGetter = b => b.RelatedDocuments,
            AddAction = (b, e) =>
            {
                if (e != null)
                {
                    e.AddRelatedDocument();
                }
                else
                {
                    b.RelatedDocuments.Add(new StringItem { Value = string.Empty });
                }
            },
            RemoveAction = (item, b, e) =>
            {
                if (e != null)
                {
                    e.RemoveRelatedDocument(item);
                }
                else
                {
                    b.RelatedDocuments.Remove(item);
                }
            },
        },
        new CollectionFieldDescriptor<ChangeLogRow>
        {
            Key = "ChangeLog",
            Label = "Лист регистрации изменений",
            IsEditable = false,
            Placeholder = "Добавьте строку ЛРИ через вкладку «Настройки»",
            TemplateDefault = "История изменений не заполнена. Добавьте строку через вкладку «Настройки».",
            HelpText = "Отображается в режиме просмотра. Для редактирования перейдите на вкладку «Настройки».",
            ItemsSourceGetter = b => b.ChangeLog,
            AddAction = (b, e) =>
            {
                if (e != null)
                {
                    e.AddChangeLogRow();
                }
                else
                {
                    b.ChangeLog.Add(DocumentBodyFactory.CreatePrefilledChangeLogRow(null, b.ChangeLog));
                }
            },
            RemoveAction = (item, b, e) =>
            {
                if (e != null)
                {
                    e.RemoveChangeLogRow(item);
                }
                else
                {
                    b.ChangeLog.Remove(item);
                }
            },
        },
        new CollectionFieldDescriptor<BodyAppendix>
        {
            Key = "Appendices",
            Label = "Приложения",
            Placeholder = "Например: «Форма заявки на материалы»",
            TemplateDefault = "Приложение без названия — добавьте заголовок приложения.",
            HelpText = "Каждое приложение печатается на отдельной странице с заголовком «Приложение N». " +
                        "При создании автоматически добавляется подсекция-таблица (номер — по Position).",
            Permission = FieldPermission.Mutable,
            ItemsSourceGetter = b => b.Appendices,
            AddAction = (b, e) =>
            {
                if (e != null)
                {
                    e.AddAppendix();
                }
                else
                {
                    b.Appendices.Add(DocumentBodyFactory.CreateEmptyAppendix());
                }
            },
            RemoveAction = (item, b, e) =>
            {
                if (e != null)
                {
                    e.RemoveAppendix(item);
                }
                else
                {
                    b.Appendices.Remove(item);
                }
            },
            AddSubItemAction = (appendix, e) =>
            {
                e?.AddSectionToAppendix(appendix);
            },
            RemoveSubItemAction = (appendix, subItem, e) =>
            {
                if (subItem is BodySection section)
                {
                    e?.RemoveSectionFromAppendix(appendix, section);
                }
            },
        },
    };

    private static List<BodySection> GetStarterSections(string? docTypeCode) =>
        ReferenceData.GetStarterSections(docTypeCode).ToList();

    // === Static facade для обратной совместимости ===

    private static FormsCatalog Instance => InstanceLazy.Value;

    /// <summary>
    /// Legacy static accessor: <c>Forms.GetTemplate(docTypeCode)</c>.
    /// </summary>
    public static FormTemplate StaticGetTemplate(string? docTypeCode) => Instance.GetTemplate(docTypeCode);
}