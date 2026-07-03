// ============================================================
// ИЗМЕНЯЙТЕ ЗДЕСЬ: все предустановленные справочники и шаблоны
// При запуске программы из этих данных формируется template.db
// ============================================================
//
// ReferenceData — единая точка истины для всех предустановленных строк:
//   • статусы, роли утверждающих
//   • дисциплины (группы документов по тематике, исключая коды подразделений)
//   • дефолтные значения полей метаданных
//   • дефолтные DocTypes (34 типа по ИСМ BSD-RUS-BUR-QHSE-401)
//   • дефолтные Departments (42 подразделения по WM-RUS-BUR-QHSE-401A)
//   • стартовые секции документов по DocType
//
// Эти данные попадают в:
//   1. SQLite (template.db) — через ReferenceDataService.SeedIfEmptyAsync()
//   2. Obsidian-папку (docs/_справочники/) — через DocumentsBaseGenerator
//   3. UI-комбобоксы (FormTemplate, SettingsTab) — через Forms и
//      Metadata (Defaults)
//
// Если нужно добавить новое значение — правьте только этот файл.
// ============================================================

using System.Collections.Generic;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services.Defaults;

/// <summary>
/// Централизованное хранилище seed-данных для template.db и UI-справочников.
/// Все предустановленные строки объединены здесь; сервисы только читают.
/// </summary>
public static class ReferenceData
{
    // === Простые справочники (enum-подобные) ==============================

    /// <summary>
    /// Статусы документов, отображаются в ComboBox «Статус».
    /// </summary>
    public static IReadOnlyList<string> Statuses { get; } = new[]
    {
        "проект",
        "утверждён",
        "архив",
        "в пересмотре",
        "отменён",
    };

    /// <summary>
    /// Роли утверждающего. Источник — справочник ИСМ.
    /// </summary>
    public static IReadOnlyList<string> ApproverRoles { get; } = new[]
    {
        "Совет директоров",
        "Директор по качеству, ОТ, ПБ и ООС",
        "Заместитель директора по качеству, ОТ, ПБ и ООС",
        "Руководитель подразделения",
        "Руководитель отдела по работе с персоналом",
        "Региональный директор",
        "Назначенный сотрудник подразделения-собственника",
        "Непосредственный руководитель",
    };

    /// <summary>
    /// Канонический список дисциплин (группы документов по тематике).
    /// НЕ должен содержать кодов подразделений (FIN/HR/IT/LEG и т. п.) —
    /// иначе ComboBox «Дисциплина» сломается (баг #3).
    /// </summary>
    public static IReadOnlyList<string> Disciplines { get; } = new[]
    {
        "ADMIN",  // Общее делопроизводство
        "CMP",    // Комплектация / закупки
        "CON",    // Строительство / монтаж
        "DGT",    // Цифровизация
        "DRL",    // Бурение (drilling)
        "ENV",    // Охрана окружающей среды
        "HSE",    // Охрана труда, ПБ, ООС
        "INS",    // Инспекция / контроль
        "LOG",    // Логистика
        "MNT",    // Техническое обслуживание
        "OPS",    // Эксплуатация
        "PRO",    // Проектирование
        "QA",     // Качество
        "SFT",    // Безопасность (safety)
        "TRN",    // Обучение
        "WST",    // Обращение с отходами
    };

    /// <summary>
    /// Русские подписи для дисциплин (отображаются в ComboBox «Дисциплина»).
    /// </summary>
    public static IReadOnlyDictionary<string, string> DisciplineLabels { get; } =
        new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            ["ADMIN"] = "Общее делопроизводство",
            ["CMP"] = "Комплектация / закупки",
            ["CON"] = "Строительство / монтаж",
            ["DGT"] = "Цифровизация",
            ["DRL"] = "Бурение",
            ["ENV"] = "Охрана окружающей среды",
            ["HSE"] = "Охрана труда, ПБ, ООС",
            ["INS"] = "Инспекция / контроль",
            ["LOG"] = "Логистика",
            ["MNT"] = "Техническое обслуживание",
            ["OPS"] = "Эксплуатация",
            ["PRO"] = "Проектирование",
            ["QA"] = "Качество",
            ["SFT"] = "Безопасность (safety)",
            ["TRN"] = "Обучение",
            ["WST"] = "Обращение с отходами",
        };

    // === Дефолтные DocTypes (34 типа) ====================================

    /// <summary>
    /// Полный список типов документов ИСМ (BSD-RUS-BUR-QHSE-401).
    /// Используется при инициализации template.db и Obsidian-папки
    /// docs/_справочники/типы_документов/.
    /// </summary>
    public static IReadOnlyList<DocType> DocTypes { get; } = new DocType[]
    {
        new() { Code = "Policy", LabelRu = "Политика", Description = "Документ отражает общие намерения, приоритеты и ценностные ориентиры в деятельности БурСервис, создает основу для установления целей и правил.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "Code", LabelRu = "Кодекс", Description = "Документ содержит основополагающие принципы поведения и/или деятельности и определяет взаимные отношения внутри БурСервис.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "STR", LabelRu = "Стратегия", Description = "Документ формулирует главные цели и основные пути их достижения, чтобы БурСервис получал единое направление действий.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "TRG", LabelRu = "Цели", Description = "Документ устанавливает цели в определённой области. Цели относятся ко всем подразделениям и уровням управления.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "BP", LabelRu = "Бизнес-практика", Description = "Документ содержит описание правил или совокупность практических приемов, признанных БурСервис как необходимые.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "MAN", LabelRu = "Руководство СМ", Description = "Документ описывает систему менеджмента, содержит или ссылается на документированные процедуры системы менеджмента.", ApprovalReview = "Заместитель директора по качеству, ОТ, ПБ и ООС", ApprovalApprove = "Директор по качеству, ОТ, ПБ и ООС (*)" },
        new() { Code = "CMP", LabelRu = "План антикризисного управления", Description = "Документ обеспечивает системный подход к защите сотрудников, имущества и окружающей среды от воздействия серьезных инцидентов.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "ST", LabelRu = "Стандарт ИСМ", Description = "Документ высокого уровня, устанавливающий требования к процессу ИСМ; что должно быть зафиксировано (входы, выходы, записи).", ApprovalReview = "Заместитель директора по качеству, ОТ, ПБ и ООС; Руководители подразделений", ApprovalApprove = "Директор по качеству, ОТ, ПБ и ООС (*)" },
        new() { Code = "ST-SUB", LabelRu = "Стандарт подразделения", Description = "Документ высокого уровня, устанавливающий требования к процессу, характерному для подразделения.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "LP", LabelRu = "Взаимосвязь процессов", Description = "Документ высокого уровня, показывающий, как различные процессы ИСМ связаны друг с другом, включая зависимости.", ApprovalReview = "Назначенный сотрудник или руководитель подразделения-собственника", ApprovalApprove = "Директор по качеству, ОТ, ПБ и ООС (*)" },
        new() { Code = "PM", LabelRu = "Карта процесса", Description = "Визуальное представление последовательности задач и обязанностей вовлеченных сторон, с информацией о порядке выполнения действий.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "MAN-ISM", LabelRu = "Руководство (в рамках ИСМ)", Description = "Документ содержит описание процесса и/или подробные инструкции по применению процесса в рамках ИСM.", ApprovalReview = "Заместитель директора по качеству, ОТ, ПБ и ООС; Руководители подразделений", ApprovalApprove = "Директор по качеству, ОТ, ПБ и ООС (*)" },
        new() { Code = "MAN-SUB", LabelRu = "Руководство подразделения", Description = "Документ включает руководство по техническому обслуживанию, инспекции, эксплуатации или ремонту (в рамках деятельности подразделения).", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "WM", LabelRu = "Рабочая методика", Description = "Последовательное описание деятельности (работы) с уровнем детализации, при котором снижаются неотъемлемые риски.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "GD", LabelRu = "Руководство", Description = "Рекомендуемая практика, допускающая свободу действий при интерпретации. Детализирует или предоставляет пояснения к процессу.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "DFR", LabelRu = "Положение о подразделении", Description = "Описывает функции, задачи, порядок образования, права и обязанности подразделения.", ApprovalReview = "Руководитель подразделения-собственника", ApprovalApprove = "Совет директоров" },
        new() { Code = "DFR-DEPT", LabelRu = "Положение об отделе", Description = "Описывает функции, задачи, порядок образования, права и обязанности отдела в составе подразделения.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "RGS", LabelRu = "Организационно-правовой документ", Description = "Устанавливает общие правила (порядок) по конкретному направлению сферы деятельности БурСервис.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "BSD", LabelRu = "Вспомогательный документ", Description = "Содержит информацию, дополняющую процесс подразделения, или контент, не требующий контроля стандартами СМ.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "I", LabelRu = "Инструкция", Description = "Документ содержит правила, указания или руководства, устанавливающие порядок и способ выполнения или осуществления.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "CL", LabelRu = "Проверочный лист", Description = "Список (перечень) необходимых пунктов, которые нужно выполнить или проверить.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "ERP", LabelRu = "План реагирования на ЧС", Description = "Описывает аварийные ситуации и действия, которые необходимо предпринять.", ApprovalReview = "Заместитель директора по качеству, ОТ, ПБ и ООС; Региональный директор", ApprovalApprove = "Директор по качеству, ОТ, ПБ и ООС" },
        new() { Code = "EX", LabelRu = "Образец", Description = "Образец формы или документа с типичными данными, заполненными для демонстрации пользователям.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "FO", LabelRu = "Форма", Description = "Пустая (незаполненная) версия документа, используемая для ввода данных.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "RA", LabelRu = "Оценка риска", Description = "Содержит результаты идентификации, оценки и анализа базовых рисков, а также перечень мер по их исключению, снижению или контролю.", ApprovalReview = "Назначенный сотрудник подразделения-собственника; сотрудник QHSE", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "JSA", LabelRu = "Анализ безопасности работ", Description = "Содержит результаты идентификации динамических рисков для конкретного вида деятельности, не учтенных в оценке рисков (RA).", ApprovalReview = "Назначенный сотрудник подразделения-собственника; сотрудник QHSE", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "OC", LabelRu = "Орг. структура компании", Description = "Графическое представление структуры БурСервис.", ApprovalReview = "В соответствии с ST-RUS-BUR-LEG-005", ApprovalApprove = "Совет директоров" },
        new() { Code = "OC-DEPT", LabelRu = "Орг. структура подразделения", Description = "Графическое представление структуры подразделения БурСервис.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "JD", LabelRu = "Должностная инструкция", Description = "Внутренний организационно-распорядительный документ с перечнем должностных обязанностей, прав, мер ответственности и квалификационных требований.", ApprovalReview = "Непосредственный руководитель; руководитель QHSE", ApprovalApprove = "Руководитель отдела по работе с персоналом" },
        new() { Code = "PRO", LabelRu = "Программа", Description = "План действий, программа инструктажей, стажировок, наставничества, график действий, процедуры.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "QP", LabelRu = "План качества", Description = "Описывает этапы выполнения работ по договору и описывает, как система менеджмента применяется к конкретному договору.", ApprovalReview = "Назначенный сотрудник подразделения-собственника; сотрудник QHSE", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "REG", LabelRu = "Реестр", Description = "Тип списка – перечень, реестр. Чаще всего применяется как список контролируемых документов ИСМ.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника (*)" },
        new() { Code = "TEM", LabelRu = "Шаблон", Description = "Пустой (незаполненный) файл для обеспечения базового макета документов. Заполненный файл должен быть переименован.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
        new() { Code = "VWI", LabelRu = "Визуальная рабочая инструкция", Description = "Представление рабочей методики или руководства в визуальном (графическом, видео и т. д.) формате.", ApprovalReview = "Назначенный сотрудник подразделения-собственника", ApprovalApprove = "Руководитель подразделения-собственника" },
    };

    // === Дефолтные Departments (42 подразделения) ========================

    /// <summary>
    /// Полная орг. структура по WM-RUS-BUR-QHSE-401A.
    /// Используется при инициализации template.db и Obsidian-папки
    /// docs/_справочники/подразделения/.
    /// </summary>
    public static IReadOnlyList<Department> Departments { get; } = new Department[]
    {
        new() { Code = "DIR", NameRu = "Дирекция", IsRoot = true },
        new() { Code = "QHSE", NameRu = "Отдел качества, охраны труда, промышленной безопасности и охраны окружающей среды", IsRoot = true },
        new() { Code = "SD", NameRu = "Подразделение бурения телеметрического сопровождения скважин", IsRoot = true },
        new() { Code = "SD-NNB", NameRu = "Технологии бурения и геологическое сопровождение скважин", IsRoot = false, ParentCode = "SD" },
        new() { Code = "SD-REM", NameRu = "Ремонт и техническое обслуживание оборудования", IsRoot = false, ParentCode = "SD" },
        new() { Code = "SD-TECH", NameRu = "Технологические решения", IsRoot = false, ParentCode = "SD" },
        new() { Code = "DBS", NameRu = "Подразделение буровых долот и керноотборного и скважинного инструмента", IsRoot = true },
        new() { Code = "DBS-BIT", NameRu = "Инженерное сопровождение буровых долот", IsRoot = false, ParentCode = "DBS" },
        new() { Code = "DBS-COR", NameRu = "Керноотборное оборудование и внутрискважинный инструмент", IsRoot = false, ParentCode = "DBS" },
        new() { Code = "BAR", NameRu = "Подразделение буровых растворов", IsRoot = true },
        new() { Code = "BAR-DF", NameRu = "Сервис и технологические решения в области буровых растворов", IsRoot = false, ParentCode = "BAR" },
        new() { Code = "BAR-SS", NameRu = "Сервис и технологические решения в области систем очистки", IsRoot = false, ParentCode = "BAR" },
        new() { Code = "BAR-LAB", NameRu = "Лабораторно-технологические решения", IsRoot = false, ParentCode = "BAR" },
        new() { Code = "Cons", NameRu = "Отдел геологии и разработки нефтегазовых месторождений", IsRoot = true },
        new() { Code = "HCT", NameRu = "Подразделение заканчивания скважин", IsRoot = true },
        new() { Code = "HCT-CWT", NameRu = "Подразделение очистки скважин", IsRoot = false, ParentCode = "HCT" },
        new() { Code = "HCT-MLT", NameRu = "Подразделение многоствольного заканчивания скважин", IsRoot = false, ParentCode = "HCT" },
        new() { Code = "WPS", NameRu = "Подразделение геофизических исследований", IsRoot = true },
        new() { Code = "PPS", NameRu = "Подразделение обслуживания трубопроводов", IsRoot = true },
        new() { Code = "TSS", NameRu = "Подразделение бурения с регулируемым давлением", IsRoot = true },
        new() { Code = "CMT", NameRu = "Подразделение цементирования скважин", IsRoot = true },
        new() { Code = "CMT-CBP", NameRu = "Приготовление сухих цементных смесей", IsRoot = false, ParentCode = "CMT" },
        new() { Code = "CMT-LAB", NameRu = "Лабораторно-технологические решения", IsRoot = false, ParentCode = "CMT" },
        new() { Code = "IEM", NameRu = "Отдел технического обслуживания и ремонта оборудования", IsRoot = true },
        new() { Code = "IEM-PREV", NameRu = "Деятельность по техническому обслуживанию оборудования, принадлежащего другим отделам", IsRoot = false, ParentCode = "IEM" },
        new() { Code = "FADM", NameRu = "Административный отдел", IsRoot = true },
        new() { Code = "IT", NameRu = "Отдел информационных технологий", IsRoot = true },
        new() { Code = "SC", NameRu = "Департамент материально-технического обеспечения", IsRoot = true },
        new() { Code = "SC-PR", NameRu = "Управление закупками", IsRoot = false, ParentCode = "SC" },
        new() { Code = "SC-LG", NameRu = "Управление логистикой", IsRoot = false, ParentCode = "SC" },
        new() { Code = "SC-MM", NameRu = "Управление складами", IsRoot = false, ParentCode = "SC" },
        new() { Code = "HR", NameRu = "Отдел по работе с персоналом", IsRoot = true },
        new() { Code = "HR-HRD", NameRu = "Обучение и развитие персонала", IsRoot = false, ParentCode = "HR" },
        new() { Code = "RES", NameRu = "Отдел управления имущественным комплексом", IsRoot = true },
        new() { Code = "BD", NameRu = "Отдел развития бизнеса", IsRoot = true },
        new() { Code = "PM", NameRu = "Подразделение управления проектами", IsRoot = true },
        new() { Code = "LSS", NameRu = "Подразделение цифровых решений", IsRoot = true },
        new() { Code = "FIN", NameRu = "Финансовый департамент", IsRoot = true },
        new() { Code = "LEG", NameRu = "Юридический отдел", IsRoot = true },
        new() { Code = "ICS", NameRu = "Департамент экономической безопасности и внутреннего контроля", IsRoot = true },
        new() { Code = "CSD", NameRu = "Отдел развития и поддержки корпоративных систем", IsRoot = true },
        new() { Code = "RD", NameRu = "НИОКР", IsRoot = true },
        new() { Code = "RD-HW", NameRu = "Аппаратные средства (HW)", IsRoot = false, ParentCode = "RD" },
        new() { Code = "RD-SW", NameRu = "Программные средства (SW)", IsRoot = false, ParentCode = "RD" },
        new() { Code = "RD-DCS", NameRu = "Системы управления и диспетчеризации (DCS)", IsRoot = false, ParentCode = "RD" },
    };

    // === Стартовые секции шаблонов (по DocType) ===========================

    /// <summary>
    /// Возвращает стартовый набор центральных секций для документа
    /// указанного типа. Используется Forms.GetStarterSections().
    /// </summary>
    public static IReadOnlyList<BodySection> GetStarterSections(string? docTypeCode) => docTypeCode switch
    {
        "Code" => new List<BodySection>
        {
            new() { Position = 0, Title = "Видение", Body = string.Empty },
            new() { Position = 1, Title = "Обязательства", Body = string.Empty },
            new() { Position = 2, Title = "Принципы", Body = string.Empty },
            new() { Position = 3, Title = "Ответственность", Body = string.Empty },
        },
        "Policy" => new List<BodySection>
        {
            new() { Position = 0, Title = "Видение", Body = string.Empty },
            new() { Position = 1, Title = "Обязательства", Body = string.Empty },
            new() { Position = 2, Title = "Принципы", Body = string.Empty },
            new() { Position = 3, Title = "Ответственность", Body = string.Empty },
        },
        "BP" => new List<BodySection>
        {
            new() { Position = 0, Title = "Интерфейсы взаимодействия", Body = string.Empty },
            new() { Position = 1, Title = "Ключевые правила", Body = string.Empty },
            new() { Position = 2, Title = "Управление отклонениями", Body = string.Empty },
        },
        "ST" => new List<BodySection>
        {
            new() { Position = 0, Title = "Нормативные ссылки", Body = string.Empty },
            new() { Position = 1, Title = "Технические требования", Body = string.Empty },
            new() { Position = 2, Title = "Требования к материалам", Body = string.Empty },
            new() { Position = 3, Title = "Требования надежности и безопасности", Body = string.Empty },
            new() { Position = 4, Title = "Отклонения (Concessions)", Body = string.Empty },
        },
        "PM" => new List<BodySection>
        {
            new() { Position = 0, Title = "Входы и выходы", Body = "**Владелец процесса:** \n\n**Входные условия (Triggers/Inputs):** \n\n**Ожидаемый результат (Outputs):** " },
            new() { Position = 1, Title = "Визуальный алгоритм", Body = "```mermaid\ngraph TD\n    Start([Старт]) --> Step1[Шаг 1]\n    Step1 --> End([Финиш])\n```" },
            new() { Position = 2, Title = "Матрица ответственности (RACI)", Body = "| № Шага | Описание шага | Роль А | Роль Б | Записи |\n| :--- | :--- | :---: | :---: | :--- |\n| 1 | | R | A | |" },
            new() { Position = 3, Title = "KPI процесса", Body = "| Метрика | Целевое значение | Периодичность |\n| :--- | :--- | :--- |\n| | | |" },
        },
        "WM" => new List<BodySection>
        {
            new() { Position = 0, Title = "Квалификация исполнителя", Body = string.Empty },
            new() { Position = 1, Title = "Матрица рисков (JSA)", Body = "| Шаг операции | Возможная опасность (Risk) | Меры защиты / СИЗ (Mitigation) |\n| :--- | :--- | :--- |\n| | | |" },
            new() { Position = 2, Title = "Требуемые ресурсы (Tools & Equipment)", Body = "- [ ] \n- [ ] " },
            new() { Position = 3, Title = "Пошаговая процедура", Body = "### Шаг 1. \n**Действие:** \n\n**Критерий приемки:** \n\n### Шаг 2. \n**Действие:** \n\n**Критерий приемки:** " },
            new() { Position = 4, Title = "Действия при нештатных ситуациях", Body = string.Empty },
        },
        "Basis" => new List<BodySection>
        {
            new() { Position = 0, Title = "Триггер создания", Body = string.Empty },
            new() { Position = 1, Title = "Влияние на безопасность (HSE)", Body = string.Empty },
            new() { Position = 2, Title = "Владелец и код домена", Body = string.Empty },
            new() { Position = 3, Title = "Чувствительная информация", Body = string.Empty },
            new() { Position = 4, Title = "Архитектурный уровень и тип", Body = string.Empty },
            new() { Position = 5, Title = "Целевая аудитория", Body = string.Empty },
            new() { Position = 6, Title = "Зависимости (Standards)", Body = string.Empty },
            new() { Position = 7, Title = "Обоснование наименования и границ", Body = string.Empty },
            new() { Position = 8, Title = "Обоснование обучения", Body = string.Empty },
        },
        _ => new List<BodySection>
        {
            new() { Position = 0, Title = "Раздел 1", Body = string.Empty },
        },
    };

    // === Дефолтные значения полей метаданных =============================

    /// <summary>
    /// Дефолтные значения для TemplateDefault в Metadata (Defaults).
    /// Используются при создании нового документа и при пустых полях.
    /// </summary>
    public static class MetadataDefaults
    {
        public const string DocType = "ST";
        public const string Department = "НИОКР";
        public const string Revision = "1";
        public const string Status = "проект";
        public const string Owner = "НИОКР";
        public const string ApproverRole = "Директор по качеству, ОТ, ПБ и ООС";
        public const string FooterText = "Печатные версии этого документа считаются НЕКОНТРОЛИРУЕМЫМИ";
    }

    // Шаблоны документов (.md) для _шаблоны/ больше не сидируются —
    // фабричный шаблон один (T_Стандарт.md) и грузится TemplateLoader'ом напрямую.
}
