# Burservis DocAsCode / БурСервис DocAsCode

> Desktop-редактор нормативных документов ООО «БурСервис» с генерацией DOCX.  
> Desktop editor for Burservis normative documents with DOCX generation.

**Версия:** 1.6.0 (Service Layer refactor) | **Платформа:** .NET 8 + Avalonia 12.0.5 | **Лицензия:** Internal use only

---

## Содержание / Contents

1. [Возможности](#возможности--features)
2. [Быстрый старт](#быстрый-старт--quick-start)
3. [Конфигурация](#конфигурация--configuration)
4. [Архитектура](#архитектура--architecture)
5. [Поддерживаемые типы документов](#поддерживаемые-типы-документов--supported-document-types)
6. [База данных](#база-данных--database)
7. [Фабричные шаблоны](#фабричные-шаблоны--factory-templates)
8. [Жизненный цикл документа](#жизненный-цикл-документа--document-lifecycle)
9. [Тестирование](#тестирование--testing)
10. [Известные проблемы и ограничения](#известные-проблемы-и-ограничения)
11. [Дополнительная документация](#дополнительная-документация)
12. [Лицензия и контакты](#лицензия-и-контакты--license-and-contacts)

---

## Возможности / Features

### Phase 1–3: Базовый редактор
- Редактирование документа по динамической форме, построенной из шаблона
- YAML frontmatter (`doc_id`, `doc_type`, `title`, `aliases`, `tags`, `basis_card`, …) с 15 полями
- 7 фабричных шаблонов (Code, Policy, BP, ST, PM, WM, Basis), загружаемых read-only
- 3 вкладки: **Документ** (форма), **Метаданные**, **MD Preview**
- Экспорт в `.docx` с профилями форматирования для каждого типа (Mermaid monospace, RACI monospace, JSA monospace)
- Справочники: 29 типов документов + 42 подразделения (seed из Markdown-файла)
- Markdown round-trip: YAML → MD → модель → MD → DOCX
- Валидация сгенерированного DOCX через OpenXML SDK (0 errors)

### Phase 4: Структурированный документ
- Структурированная модель `DocumentBody` (Purpose, Scope, Terms, Sections, Training, References, ChangeLog)
- Динамический `FormControl` рендерит поля из `FormTemplate` descriptors
- 6 типов полей: TextArea, SingleLine, RepeatingPair, RepeatingSection, RepeatingSingleLine, ChangeLogTable
- SQLite-хранилище черновиков (`documents` + 5 связанных таблиц)
- 3 точки входа: **Новый**, **Открыть .md**, **Загрузить из БД**
- Стартовые секции для каждого типа (PM = "Входы/Выходы + Визуальный алгоритм + RACI + KPI" и т.д.)

### Phase 5: UX Hardening
- `EnsureRequiredFields()` — единая процедура для всех путей входа: гарантирует starter-строки в Terms и ChangeLog
- `UpdateSourceTrigger=PropertyChanged` на всех TextBox'ах — preview обновляется **на каждое нажатие клавиши**, без потери фокуса
- `OnActiveTabIndexChanged` → `RefreshPreview()` — при переключении на вкладку "MD Preview" preview **гарантированно свежий**
- `RefreshPreview()` в начале `ExportDocxAsync` — DOCX **всегда** содержит актуальные данные
- Серый 4-строчный шаблон-подсказка в `ChangeLogField.Description` — UI-only, в DOCX не попадает
- Все структурные секции рендерятся в preview **всегда** (даже пустые) с placeholder `_...заполните..._` — пользователь видит структуру сразу после запуска
- Регрессионные тесты: 4 RepeatingFieldSubscriptionTests + 3 PreviewUpdateTests + 6 MalformedMdRecoveryTests + 16 StarterRowsTests = **29 регрессионных**

---

## Быстрый старт / Quick start

### Требования / Prerequisites

| Компонент | Версия | Примечание |
|-----------|--------|------------|
| .NET SDK | 8.0.x | Рекомендуется 8.0.422+ |
| OS | Windows 10/11 x64 | Avalonia 12.0.5 + SkiaSharp |
| SQLite | bundled | Microsoft.Data.Sqlite 9.0.0 |
| RAM | ≥ 4 GB | Для рендеринга DOCX |

### Установка / Installation

```bash
# 1. Клонировать репозиторий
cd D:\Avalonia\R&D_Gen_doc

# 2. Скопировать пример конфигурации
copy appsettings.example.json appsettings.json
# Отредактировать appsettings.json — указать пути к _factory и SQLite

# 3. Собрать
dotnet build
```

### Запуск / Run

```bash
# Обычный запуск
dotnet run

# Дымовой тест (E2E для CI/CD)
dotnet run -- --smoke
```

### Сборка release-версии / Production build

```bash
dotnet publish -c Release -o ./publish
./publish/AvaloniaApp.exe
```

---

## Конфигурация / Configuration

Конфигурация хранится в `appsettings.json` (не в Git). Шаблон — `appsettings.example.json`.

### Структура

```json
{
  "ConnectionStrings": {
    "DraftsDb": "Data Source=path/to/local_drafts.db"
  },
  "FactoryPaths": {
    "Root": "path/to/_factory"
  },
  "ReferenceData": {
    "SubdivisionsMd": "path/to/ISM_doc-types-and-subdivisions.md"
  },
  "UserSettings": {
    "DefaultAuthor": ""
  }
}
```

### Поля

| Поле | Описание | По умолчанию |
|------|----------|--------------|
| `ConnectionStrings.DraftsDb` | ADO.NET строка подключения к SQLite | локальный путь |
| `FactoryPaths.Root` | Папка с шаблонами `_factory` | локальный путь |
| `ReferenceData.SubdivisionsMd` | MD-файл со справочником подразделений | локальный путь |
| `UserSettings.DefaultAuthor` | Ф.И.О. автора по умолчанию для Листа регистрации | `""` |

---

## Архитектура / Architecture

### Clean Architecture + MVVM

```
┌─────────────────────────────────────────────────────────────┐
│ Frameworks & Drivers                                        │
│ Views/ (Avalonia AXAML), SQLite, OpenXML, FileSystem        │
└──────────────────────┬──────────────────────────────────────┘
                       │ depends on ↓
┌──────────────────────┴──────────────────────────────────────┐
│ Interface Adapters                                           │
│ ViewModels/ (MainWindowViewModel, NewDocumentDialogVM,       │
│             SuccessOverlayViewModel)                         │
│ Services/   (IDialogService, IMarkdownFileService,            │
│             IDocumentExportService, IDocumentSaveService,    │
│             IMarkdownPreviewBuilder, IClipboardService,      │
│             IFileSystemService)                              │
└──────────────────────┬──────────────────────────────────────┘
                       │ depends on ↓
┌──────────────────────┴──────────────────────────────────────┐
│ Application / Use Cases                                      │
│ FormTemplateCatalog, DocumentBodyFactory,                   │
│ DocumentBodyToMarkdown, MarkdownService, DocxGenerator      │
└──────────────────────┬──────────────────────────────────────┘
                       │ depends on ↓
┌──────────────────────┴──────────────────────────────────────┐
│ Domain / Entities                                           │
│ Models/ (DocumentMetadata, DocumentBody, TermPair, ... )     │
└─────────────────────────────────────────────────────────────┘
```

**Правило зависимостей:** Зависимости только внутрь. Внутренние слои не знают о внешних.

Подробнее: см. [Docs/Architecture.md](Docs/Architecture.md)

### Ключевые паттерны

| Паттерн | Где применяется | Зачем |
|---------|-----------------|-------|
| **MVVM через CommunityToolkit.Mvvm** | Все ViewModel | `[ObservableProperty]` + `[RelayCommand]` |
| **DI через конструкторы** | MainWindowViewModel(... 7 сервисов ...) | Тестируемость |
| **Интерфейсы для сервисов** | `IDialogService`, `IMarkdownFileService`, `IDocumentExportService`, `IDocumentSaveService`, `IMarkdownPreviewBuilder`, `IClipboardService`, `IFileSystemService` | Подмена в тестах |
| **async/await для I/O** | `DocumentRepository.SaveAsync`, `MarkdownFileService.OpenAsync` | Не блокируем UI |
| **TwoWay bindings + UpdateSourceTrigger** | Все TextBox в полях формы | Мгновенное обновление preview |
| **Factory в Domain** | `DocumentBodyFactory.CreateStarterCollections` | Чистая логика, тестируемая |
| **EnsureRequiredFields** | На всех 3 путях входа | Единообразное состояние документа |
| **Overlay pattern** | `SuccessOverlay` в `MainWindow.axaml` | Замена модальному `Window.ShowDialog` |

### Структура каталогов

```
AvaloniaApp/
├── Models/                          # Domain / Entities
│   ├── DocumentMetadata.cs         # YAML schema (15 полей)
│   ├── DocumentBody.cs             # Runtime body (Purpose, Scope, Terms, ...)
│   ├── TermPair.cs, BodySection.cs, ChangeLogRow.cs
│   ├── FormFieldDescriptor.cs      # Enum FormFieldKind + descriptor
│   ├── FormTemplate.cs             # HeaderFields/CenterFields/TrailingFields
│   └── DocumentBodyFactory.cs      # ★ Starter collections + EnsureRequiredFields
│
├── Services/                        # Interface Adapters / Infrastructure
│   ├── DocumentRepository.cs       # SQLite CRUD
│   ├── DocumentSchema.cs           # CREATE TABLE IF NOT EXISTS
│   ├── DocumentBodyToMarkdown.cs   # ★ Все секции всегда видны + placeholder
│   ├── DocumentBodyParser.cs       # MD → DocumentBody round-trip
│   ├── FormTemplateCatalog.cs      # 7 шаблонов (ST, PM, WM, ...)
│   ├── MarkdownService.cs          # YAML Parse/Compose
│   ├── DocxGenerator.cs            # ★ Phase 3 refactored
│   ├── DocxProfile.cs              # Per-type formatting
│   ├── TemplateLoader.cs           # Read-only factory loader
│   ├── ReferenceDataService.cs     # 29 doc types + 42 subdivisions
│   ├── BasisCardService.cs         # CRUD for basis cards
│   ├── AppSettings.cs              # Centralized appsettings.json access
│   ├── SqliteConnectionFactory.cs  # Connection factory
│   ├── DocumentChangeEngine.cs     # ★ Единый движок мутаций DocumentBody
│   ├── IDialogService.cs           # Абстракция диалогов
│   ├── IMarkdownFileService.cs     # Open/Save .md
│   ├── IDocumentExportService.cs   # Export DOCX
│   ├── IDocumentSaveService.cs     # SQLite CRUD-адаптер
│   ├── IMarkdownPreviewBuilder.cs  # Компоновка preview
│   ├── IClipboardService.cs        # Буфер обмена
│   ├── IFileSystemService.cs       # Файловая система + explorer.exe
│   └── *Implementation*.cs         # Реализации вышеуказанных интерфейсов
│
├── ViewModels/                      # Interface Adapters
│   ├── MainWindowViewModel.cs      # ★ Phase 5: tab-switch refresh; 1.6.0: чистый адаптер
│   ├── NewDocumentDialogViewModel.cs
│   ├── SuccessOverlayViewModel.cs  # ★ Overlay-диалог успеха
│   └── ViewModelBase.cs
│
├── Views/                           # Frameworks & Drivers
│   ├── MainWindow.axaml            # ★ TabControl (3 вкладки) + SuccessOverlay Panel
│   ├── FormControl.axaml           # ★ Dynamic FormTemplate renderer
│   ├── NewDocumentDialog.axaml
│   ├── SuccessOverlay.axaml        # ★ Inline overlay (замена SuccessDialog)
│   └── Fields/                     # 6 dynamic field controls
│       ├── TextAreaField.axaml          (Purpose, Scope, Training)
│       ├── SingleLineField.axaml        (ParentDocument)
│       ├── RepeatingPairField.axaml     (Terms)         ★ PropertyChanged handler
│       ├── RepeatingSectionField.axaml  (Sections)      ★ PropertyChanged handler
│       ├── RepeatingSingleLineField.axaml (RelatedDocs)
│       └── ChangeLogField.axaml         (ChangeLogSheet) ★ 5-col + placeholder
│
├── Tools/
│   └── SmokeTest.cs                # E2E test (12 checks)
│
├── Docs/                            # Документация
│   ├── Architecture.md             # Clean Architecture overview
│   ├── Services.md                 # Каталог сервисов и DI
│   ├── ChangeLogSheet_v2_Design.md
│   └── PreviewUpdate_Audit.md
│
├── AvaloniaApp.Tests/               # xUnit + NetArchTest
│   ├── ArchitectureTests.cs        # 6 architectural tests
│   ├── ChangeLogTests.cs           # 4 ChangeLog field tests
│   ├── RepeatingFieldSubscriptionTests.cs # 4 subscription regression tests
│   ├── PreviewUpdateTests.cs       # 3 tab-switch refresh tests
│   ├── StarterRowsTests.cs         # 16 starter rows tests (7 doc types × 2 + 2)
│   ├── MalformedMdRecoveryTests.cs # 6 MD recovery tests
│   ├── InitialPreviewStateTests.cs # 4 initial preview state tests
│   ├── MarkdownFileServiceTests.cs # 4 .md open/save tests
│   ├── MarkdownPreviewBuilderTests.cs # 3 preview composition tests
│   ├── DocumentExportServiceTests.cs # 2 DOCX export tests
│   ├── DocumentChangeEngineTests.cs # 12 mutation engine tests
│   ├── MetadataSyncTests.cs        # 13 metadata → preview sync tests
│   ├── UiSyncTests.cs              # 8 UI collection sync tests
│   └── AddedRowPreviewTests.cs     # 6 add/remove row preview tests
│
├── appsettings.json                 # ★ локальные пути (не в Git)
├── appsettings.example.json
├── README.md                        # ← Этот файл
├── CHANGELOG.md
├── .gitignore
└── AvaloniaApp.slnx                 # Solution file
```

---

## Поддерживаемые типы документов / Supported document types

| Код | Назначение | Уровень HMS | Стартовые секции |
|-----|------------|-------------|------------------|
| `Code` | Кодекс деловой этики / корпоративный кодекс | 1 | Видение, Обязательства, Принципы, Ответственность |
| `Policy` | Политика / стратегия | 2 | Видение, Обязательства, Принципы, Ответственность |
| `BP` | Бизнес-процесс (Business Process) | 3 | Интерфейсы, Ключевые правила, Управление отклонениями |
| `ST` | Стандарт (Standard) | 3 | Нормативные ссылки, Технические требования, Материалы, Надёжность, Concessions |
| `PM` | Карта процесса (Process Map) | 4 | Входы/выходы, Визуальный алгоритм (Mermaid), RACI, KPI |
| `WM` | Рабочая инструкция (Work Method) | 4 | Квалификация, JSA, Инструменты, Пошаговая процедура, Нештатные ситуации |
| `Basis` | Карточка основания / basis card | — | 9 секций архитектурного обоснования |

Все шаблоны имеют **общие header-поля** (1 Цель, 2 Область, 3 Термины) и **trailing-поля** (Обучение, Ссылки, Лист регистрации).

---

## База данных / Database

SQLite-база задаётся в `appsettings.json` через `ConnectionStrings:DraftsDb`.

### Схема

| Таблица | Назначение |
|---------|------------|
| `documents` | Метаданные документа (YAML fields) |
| `document_bodies` | Purpose, Scope, PrimaryTraining, RecurrentTraining, ParentDocument |
| `document_terms` | Термины и определения (Position, Term, Definition) |
| `document_sections` | Разделы тела (Position, Title, Body) |
| `document_related_docs` | Связанные документы |
| `document_change_log` | Лист регистрации (Date, Revision, Author, Reviewer, Description) |
| `doc_type_registry` | Справочник типов документов (29 записей) |
| `org_structure` | Справочник подразделений (42 записи) |

Схема создаётся автоматически при запуске (`DocumentSchema.EnsureCreated`).

### Backup

SQLite — файл. Просто скопируйте `local_drafts.db` в безопасное место. WAL-файлы (`*.db-shm`, `*.db-wal`) — не нужны для бэкапа при остановленном приложении.

---

## Фабричные шаблоны / Factory templates

Шаблоны загружаются **только для чтения** из внешней папки `_factory`, указанной в `appsettings.json` (`FactoryPaths:Root`).

Каждый шаблон — Markdown-файл с YAML frontmatter, определяющий:

- код типа документа и уровень HMS;
- секции по умолчанию;
- роль утверждающего;
- теги и настройки форматирования DOCX.

⚠️ **Не изменяйте шаблоны из приложения** — они являются источником истины для нормативной базы.

---

## Жизненный цикл документа / Document lifecycle

### 3 точки входа в документ

```
┌─────────────────────────┐
│ Запуск приложения       │
│ InitializeAsync()       │
│ → NewDocument("ST")     │ ─── Дефолтный документ (ST)
└─────────────────────────┘
            ↓
[Пользователь работает с документом]
            ↓
   ┌────────┴────────┐
   ↓                 ↓
[Новый из       [Открыть .md]    [Загрузить из БД]
 шаблона]              ↓                ↓
   ↓          IMarkdownFileService   IDocumentSaveService
NewDocument       .OpenAsync()         .LoadByDocIdAsync()
   ↓                 ↓                ↓
   └────────┬────────┴────────┬───────┘
            ↓                 ↓
   DocumentBodyFactory.EnsureRequiredFields(body)
            ↓                 ↓
   vm.DocumentBody = body  [ObservableProperty setter]
            ↓
   FormControl.RebuildFields()
            ↓
   Поля динамически создаются из дескрипторов + данных
            ↓
   [Пользователь редактирует через TwoWay bindings / DocumentChangeEngine]
            ↓
   ┌────────┬─────────┬──────────┐
   ↓        ↓         ↓          ↓
[Сохранить  [Экспорт   [Экспорт  [Preview
 в БД]       .md]       DOCX]     tab]
   ↓           ↓         ↓         ↓
IDocument-  IMarkdown- IDocument- RefreshPreview()
SaveService .SaveAsync() ExportService
.SaveAsync()            .ExportDocxAsync()
```

### Команды тулбара

| Кнопка | Команда VM | Сервис | Результат |
|--------|-----------|--------|-----------|
| **Новый из шаблона** | `NewFromTemplateCommand` | `IDialogService` + `TemplateCatalog` | Создаёт документ из выбранного фабричного шаблона |
| **Открыть .md** | `OpenFileCommand` | `IMarkdownFileService.OpenAsync` | Загружает `.md`, парсит frontmatter/body |
| **Сохранить в БД** | `SaveToDatabaseCommand` | `IDocumentSaveService.SaveAsync` | Insert/update в SQLite |
| **Экспорт .md** | `ExportMarkdownCommand` | `IMarkdownFileService.SaveAsync` | Записывает `.md` на диск |
| **Экспорт в DOCX** | `ExportDocxCommand` | `IDocumentExportService.ExportDocxAsync` | Генерирует `.docx` |

### Overlay-диалоги

Все успешные операции (`Открыть .md`, `Сохранить в БД`, `Экспорт .md`, `Экспорт в DOCX`) показывают `SuccessOverlay` — inline-панель поверх основного окна.

- Реализован в `Views/SuccessOverlay.axaml` + `ViewModels/SuccessOverlayViewModel.cs`.
- Встроен в `MainWindow.axaml` через корневой `<Panel>` + binding `IsSuccessOverlayVisible`.
- Поддерживает кнопки: **Скопировать путь**, **Открыть в проводнике**, **ОК**.
- `SuccessOverlayViewModel` получает `IClipboardService` и `IFileSystemService` через DI, поэтому не зависит от `Avalonia.Controls`.

> Ранее использовался `SuccessDialog.axaml` как отдельное `Window`. Он удалён в 1.6.0.

### Что попадает в preview / DOCX

| Поле | preview | DOCX |
|------|---------|------|
| 1 Цель (Purpose) | всегда с заголовком | если заполнено |
| 2 Область (Scope) | всегда с заголовком | если заполнено |
| 3 Термины (Terms) | всегда с заголовком | если есть непустые |
| 4-N Секции (Sections) | всегда с заголовками | если заполнены |
| Обучение (Training) | всегда с подзаголовками | если заполнены |
| Ссылки (References) | всегда с подзаголовками | если заполнены |
| Лист регистрации (ChangeLog) | всегда с таблицей | всегда с таблицей |

Пустые секции в preview получают серый placeholder `_...заполните..._`. В DOCX placeholder **не попадает** (только если секция непустая).

---

## Тестирование / Testing

### Все тесты

| Категория | Файл | Кол-во |
|-----------|------|--------|
| **Архитектурные** | `ArchitectureTests.cs` | 6 |
| ChangeLog field | `ChangeLogTests.cs` | 4 |
| RepeatingField subscription (regression) | `RepeatingFieldSubscriptionTests.cs` | 4 |
| Tab-switch preview refresh | `PreviewUpdateTests.cs` | 3 |
| Starter rows (7 doc types × 2 + 2) | `StarterRowsTests.cs` | 16 |
| Malformed MD recovery | `MalformedMdRecoveryTests.cs` | 6 |
| Initial preview state | `InitialPreviewStateTests.cs` | 4 |
| Markdown file service | `MarkdownFileServiceTests.cs` | 4 |
| Markdown preview builder | `MarkdownPreviewBuilderTests.cs` | 3 |
| Document export service | `DocumentExportServiceTests.cs` | 2 |
| Document change engine | `DocumentChangeEngineTests.cs` | 12 |
| Metadata sync | `MetadataSyncTests.cs` | 13 |
| UI sync | `UiSyncTests.cs` | 8 |
| Added row preview | `AddedRowPreviewTests.cs` | 6 |
| **ИТОГО** | | **92** |

### Запуск

```bash
# Все тесты
dotnet test AvaloniaApp.Tests/AvaloniaApp.Tests.csproj

# Конкретная категория
dotnet test --filter "FullyQualifiedName~RepeatingField"
```

### E2E smoke test

```bash
dotnet run -- --smoke
```

Ожидаемый результат:
```
SUMMARY: pass=12 fail=0
```

12 проверок включают:
1. Schema created
2. Document saved (id returned)
3. Document loaded by DocId
4. Markdown generated
5. DOCX generated
6. OpenXML validation: 0 errors
7. Update + re-save
8. Re-load (revision incremented, sections count matches)
9. Delete
10. After delete: gone
11. Description hint defined (Phase 5)
12. ChangeLogTable 5-column format preserved

### Архитектурные тесты (NetArchTest)

Проверяют, что зависимости направлены только внутрь (Clean Architecture):

- ViewModels inherit from ViewModelBase
- Views reside in Views namespace
- ViewModels don't reference Views
- Models don't reference ViewModels or Views
- Services don't reference Views
- ViewModels don't reference Avalonia.Controls

Архитектурный тест `ViewModels_ShouldInheritFromViewModelBase` исключает вложенные заглушки `NullDialogService`, `NullClipboardService`, `NullFileSystemService` из проверки наследования, но они остаются внутри `MainWindowViewModel` и используются для конструктора по умолчанию в тестах.

---

## Известные проблемы и ограничения

### Известные ограничения

1. **Только Windows.** Avalonia 12.0.5 + SkiaSharp не тестировались на Linux/macOS.
2. **Single-window.** Не поддерживается работа с несколькими документами одновременно.
3. **Нет undo/redo.** Phase 6 (не входит в текущую версию).
4. **YAML legacy-поля.** Поддерживаются при чтении (`document_name`, `document_code`, `approved_by`, `date_approved`) для совместимости со старыми шаблонами, но при сохранении пишутся только новые поля.

### Известные UX-нюансы

- При печати в TextBox preview обновляется на каждое нажатие клавиши (UpdateSourceTrigger=PropertyChanged).
- При переключении на вкладку "MD Preview" preview пересчитывается явно.
- При экспорте DOCX preview также пересчитывается перед генерацией — гарантирует свежесть данных.

---

## Дополнительная документация

- [Docs/Architecture.md](Docs/Architecture.md) — подробное описание Clean Architecture
- [Docs/Services.md](Docs/Services.md) — каталог сервисов и DI
- [Docs/ChangeLogSheet_v2_Design.md](Docs/ChangeLogSheet_v2_Design.md) — дизайн Листа регистрации (5 колонок + серый шаблон)
- [Docs/PreviewUpdate_Audit.md](Docs/PreviewUpdate_Audit.md) — аудит механизма обновления preview
- [CHANGELOG.md](CHANGELOG.md) — история изменений

---

## Лицензия и контакты / License and contacts

© 2026 ООО «БурСервис». Внутренний проект.  
По вопросам обращайтесь к команде QHSE / ИНТИ.
