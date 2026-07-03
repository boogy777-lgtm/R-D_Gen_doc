# Changelog

All notable changes to **Burservis DocAsCode** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.8.0] - 2026-07-03 — Architectural Purity & Fluent UI

### Added
- **Архитектурный рефакторинг:** Полный переход на Clean Architecture + SOLID/DRY.
- **FluentAvaloniaUI integration:** Перевод UI на `FAAppWindow` и `FAContentDialog` для in-app overlay диалогов.
- **DI-контейнер:** Внедрен `Microsoft.Extensions.DependencyInjection` для всех сервисов и ViewModel.
- **MarkdownSyntaxDictionary:** Единый источник правды для всей MD-разметки.
- **Registry Pattern:** Реализован `FieldViewModelRegistry` для OCP-генерации полей форм.
- **MASTER_AI_RULES.md:** Сводный документ правил для агентов в `.opencode/rules/`.

### Changed
- **`DocumentChangeEngine` расщеплен:** Теперь это легкий Facade, делегирующий логику `ChangeLogManager`, `NumberingEngine`, `CollectionObserver`.
- **`MainWindowViewModel`:** Освобожден от монолитной логики, теперь только тонкий Binding-адаптер.
- **Парсеры/Генераторы MD:** Переписаны с использованием `MarkdownSyntaxDictionary`, удалено ~840 строк дублирования и магических строк.
- **DIP для справочников:** `ReferenceDataService`, `Forms`, `Metadata` переведены на инжектируемые интерфейсы.

### Fixed
- **UX-баги:** Исправлено "невидимое" выделение текста (`SelectionBrush`), устранено мигание при Hot-reload (умный Diff), исправлена рассинхронизация DocType, добавлен авто-сброс предупреждений ЛРИ.
- **C# 14 / .NET 10:** Внедрены фишки языка (Primary Constructors, Collection Expressions).

---

## [1.7.0] - 2026-06-30 — ChangeLog popup unified

### Added

- **`Services/IChangeLogPromptService.cs` + `ChangeLogPromptService.cs`** — use case «показать popup добавления строки ЛРИ и добавить её через Engine». Инкапсулирует логику, общую для авто-триггера (изменение ревизии/даты в метаданных) и кнопки «+» в редакторе ЛРИ. Семафор защищает от гонок при многократных параллельных изменениях.
- **`CollectionFieldDescriptor<TItem>.AddCommandFactory`** — опциональная фабрика команды `Add`. Если задана, переопределяет стандартное поведение кнопки «+» (раньше — прямой вызов `AddAction`). Применяется для коллекций, требующих подтверждения перед добавлением.
- **4 новых теста** `ChangeLogAddButtonTests`: popup показывается при нажатии «+»; строка добавляется только после подтверждения; при отмене пользователем строка НЕ добавляется; popup показывается даже если ревизия/дата не менялись.
- **Публичный конструктор `MainWindowViewModel(IDialogService)`** — упрощает инжекцию stub-диалога в тестах.

### Changed

- **Кнопка «+» в Листе регистрации изменений больше не добавляет строку напрямую.** Теперь она вызывает тот же popup-диалог подтверждения, что и авто-триггер при изменении ревизии/даты. Только при согласии пользователя строка добавляется в ЛРИ. Это гарантирует, что каждая запись ЛРИ фиксируется осознанно (требование бизнес-процесса).
- **`MainWindowViewModel`**: popup-логика вынесена в `ChangeLogPromptService` (Dependency Rule — Domain/Use Case слой). VM остаётся чистым адаптером: только подписывается на `Metadata.PropertyChanged` и делегирует сервису.
- **`CollectionFieldViewModel<TItem>.AddCommand`** теперь имеет тип `System.Windows.Input.ICommand` (а не `IRelayCommand`), чтобы фабрика могла вернуть любую команду (например, `AsyncRelayCommand` для popup-сценария).
- **Удалены поля** `lastKnownRevision` / `lastKnownEffectiveDate` из `MainWindowViewModel` — теперь они инкапсулированы в `ChangeLogPromptService`.

### Fixed

- **Регрессия**: ранее кнопка «+» обходила popup и сразу вызывала `Engine.AddChangeLogRow(autoIncrementRevision: true)`, что нарушало бизнес-процесс (ЛРИ фиксирует только осознанные изменения). Теперь поведение унифицировано.

---

## [1.6.0] - 2026-06-25 — Service Layer & Overlay refactor

### Added

- **`Services/IMarkdownFileService.cs` + `MarkdownFileService.cs`** — сервис OpenAsync/SaveAsync для `.md` файлов.
- **`Services/IDocumentExportService.cs` + `DocumentExportService.cs`** — сервис экспорта в DOCX (`ExportDocxAsync`).
- **`Services/IDocumentSaveService.cs` + `DocumentSaveService.cs`** — маршрутизация `DocumentRepository` (Save/Load/List/Delete).
- **`Services/IMarkdownPreviewBuilder.cs` + `MarkdownPreviewBuilder.cs`** — компоновка frontmatter + body для MD Preview.
- **`Services/IClipboardService.cs` + `AvaloniaClipboardService.cs`** — абстракция буфера обмена.
- **`Services/IFileSystemService.cs` + `SystemFileSystemService.cs`** — файловая система + запуск `explorer.exe`.
- **Новая кнопка тулбара «Экспорт .md»** (`ExportMarkdownCommand`) — явный экспорт Markdown на диск.
- **`Views/SuccessOverlay.axaml` + `ViewModels/SuccessOverlayViewModel.cs`** — inline overlay-диалог успеха.
- **49 новых unit-тестов**: `MarkdownFileServiceTests` (4), `MarkdownPreviewBuilderTests` (3), `DocumentExportServiceTests` (2), `DocumentChangeEngineTests` (12), `MetadataSyncTests` (13), `UiSyncTests` (8), `AddedRowPreviewTests` (6). Итого: **92/92** тестов проходят.

### Changed

- **`MainWindowViewModel` стал чистым адаптером**: больше не делает прямых `File.ReadAllText`, `File.WriteAllText`, `DocumentRepository.*`, `DocxGenerator.*`, `MarkdownService.*` — только вызывает сервисы через DI.
- **DI через конструктор**: `IDialogService`, `IMarkdownFileService`, `IDocumentExportService`, `IDocumentSaveService`, `IMarkdownPreviewBuilder`, `IClipboardService`, `IFileSystemService`.
- **Кнопка «Сохранить .md» переименована в «Сохранить в БД»** (`SaveToDatabaseCommand`).
- **`Views/MainWindow.axaml`**: `SelectedValueBinding="Value"` → `{Binding Value}` — убрана конструкция, блокировавшая precompiled XAML.

### Removed

- **`Views/SuccessDialog.axaml` + `.axaml.cs`** — удалены; функциональность заменена на `SuccessOverlay`.

### Fixed

- **Архитектурный тест `ViewModels_ShouldInheritFromViewModelBase`** обновлён: исключены вложенные заглушки `NullClipboardService` и `NullFileSystemService`.

---

## [1.5.0] - 2026-06-25 — Phase 5: UX Hardening

### Added

- **`DocumentBodyFactory.EnsureRequiredFields()`** — единая процедура для всех 3 путей входа
  (`NewDocument`, `OpenFileAsync`, `LoadFromDatabaseAsync`). Гарантирует, что `Terms` и `ChangeLog`
  всегда содержат хотя бы одну стартовую строку, без перезаписи существующих данных.
- **`UserSettings.DefaultAuthor`** в `appsettings.json` — Ф.И.О. автора по умолчанию для Листа регистрации.
- **`appsettings.example.json`** с примером конфигурации.
- **`DocumentBodyFactory`** — Domain-слой фабрика для стартовых коллекций (Clean Architecture compliance).
- **47 регрессионных unit-тестов**:
  - 4 × `ChangeLogTests` (placeholder hint, prefilled row, Revision increment)
  - 4 × `RepeatingFieldSubscriptionTests` (регрессия: подписка CollectionChanged при смене коллекции через setter)
  - 3 × `PreviewUpdateTests` (tab-switch refresh, document-tab no-op, RefreshPreview captures state)
  - 16 × `StarterRowsTests` (7 типов × 2 коллекции + Ensure scenarios)
  - 6 × `MalformedMdRecoveryTests` (legacy MD, empty MD, only-YAML, full valid MD, битый файл)
  - 4 × `InitialPreviewStateTests` (все секции сразу видны, placeholder)

### Changed

- **`DocumentBodyToMarkdown.Convert`** — все структурные секции (`## 1 Цель`, `## 2 Область`,
  `## 3 Термины`, `## 4-N`, `## Обучение`, `## Ссылки`, `## Лист регистрации`) рендерятся **всегда**,
  даже если содержимое пустое. Для пустых — серый placeholder `_...заполните..._`.
  Это даёт пользователю видеть всю структуру документа сразу после открытия.
- **`UpdateSourceTrigger=PropertyChanged`** добавлен во все TextBox-поля формы
  (`TextAreaField`, `SingleLineField`, `RepeatingPairField`, `RepeatingSectionField`,
  `RepeatingSingleLineField`, `ChangeLogField`). Preview обновляется **на каждое нажатие клавиши**,
  не требуя потери фокуса.
- **`OnActiveTabIndexChanged` hook** в `MainWindowViewModel` — при переключении на вкладку
  "MD Preview" preview **явно** пересчитывается из текущего `DocumentBody`.
- **`ExportDocxAsync`** вызывает `RefreshPreview()` в начале — DOCX **гарантированно** содержит
  актуальные данные, даже если пользователь редактировал без переключения вкладок.
- **`RepeatingPairField` / `RepeatingSectionField` / `ChangeLogField`** теперь используют
  `StyledProperty.Changed.AddClassHandler` вместо подписки в конструкторе — исправлен баг,
  когда при смене коллекции через setter старая подписка оставалась на orphaned-коллекции.
- **`ChangeLogField.CreatePrefilledRow`** делегирует в `DocumentBodyFactory.CreatePrefilledChangeLogRow`
  (логика в Domain, не в View).

### Fixed

- **BUG-CL-001**: Подписка `CollectionChanged` в `RepeatingPairField`/`RepeatingSectionField`/
  `ChangeLogField` привязывалась к первой созданной коллекции. При подмене коллекции через setter
  (FormControl создаёт `new ObservableCollection(body.Terms)`) подписка терялась, изменения
  не доходили до `body.Terms`/`body.ChangeLog`.
- **BUG-MD-002**: MD Preview не обновлялся при печати в TextBox (только при клике на кнопку).
  Причина: Avalonia `TextBox.Text` по умолчанию обновляет binding только на `LostFocus`.
  Решение: `UpdateSourceTrigger=PropertyChanged`.
- **BUG-PREV-003**: При открытии приложения preview не показывал структурные секции
  (заголовки `## 1 Цель` и т.д. появлялись только после ввода текста).
  Решение: `DocumentBodyToMarkdown` теперь всегда рендерит заголовки, для пустых — placeholder.
- **BUG-MD-004**: При печати в TextBox в "Метаданные" вкладке поля `Metadata.DocumentName`
  и `Metadata.DocumentCode` использовали старые имена из легаси-YAML, что приводило
  к рассинхронизации с preview. Решение: TabControl с биндингами на актуальные имена.

### Security

- `appsettings.json` (содержит локальные пути и потенциально секреты) добавлен в `.gitignore`.

---

## [1.4.0] - 2026-06-25 — Phase 4: Структурированный документ

### Added

- **Phase 4: Structured document editor** with SQLite persistence.
- **`DocumentBody`** model: `Purpose`, `Scope`, `Terms`, `Sections`, `PrimaryTraining`,
  `RecurrentTraining`, `ParentDocument`, `RelatedDocuments`, `ChangeLog`.
- **`FormTemplate`** + **`FormFieldDescriptor`** (6 типов полей).
- **`FormTemplateCatalog`** со стартовыми секциями для каждого из 7 DocType.
- **6 динамических View-полей** в `Views/Fields/`:
  - `TextAreaField`, `SingleLineField`, `RepeatingPairField`, `RepeatingSectionField`,
    `RepeatingSingleLineField`, `ChangeLogField`.
- **`DocumentSchema.EnsureCreated()`** — авто-создание SQLite-схемы.
- **`DocumentRepository`** — полный CRUD для документов.
- **`DocumentBodyToMarkdown`** + **`DocumentBodyParser`** — round-trip MD ↔ DocumentBody.
- **`TabControl`** в `MainWindow.axaml` — 3 вкладки: Документ, Метаданные, MD Preview.
- **`MarkdownService`** — YAML frontmatter Parse/Compose с UnderscoredNamingConvention.
- 7 архитектурных тестов (NetArchTest).
- E2E smoke test: 9/9 pass, OpenXML validation 0 errors.

---

## [1.3.0] - 2026-06-25 — Phase 3: DOCX-форматирование

### Added

- **`DocxGenerator`** — переработан с поддержкой профилей форматирования.
- **`DocxProfile`** — per-DocType настройки: monospace для Mermaid/RACI/JSA, shield-callout для ST/PM/WM.
- **BasisCard** — автоматическая подстановка ссылки на карточку основания.
- Валидация OpenXML для всех 7 DocType (0 errors).

---

## [1.2.0] - 2026-06-25 — Phase 2: Справочники и basis cards

### Added

- **`appsettings.json`** с подключением к SQLite и путями к фабрике.
- **`AppSettings`** — централизованный доступ к конфигурации.
- **`SqliteConnectionFactory`** — фабрика подключений.
- **`ReferenceDataService`** — авто-seed 29 doc types + 42 подразделений из MD-файла.
- **`BasisCardService`** — CRUD для карточек основания + авто-создание.
- **`NewDocumentDialog`** — UI для выбора фабричного шаблона.

---

## [1.1.0] - 2026-06-25 — Phase 1: Базовый редактор

### Added

- **`DocumentMetadata`** с 15 YAML-полями (DocId, DocType, Title, Aliases, Domain,
  Author, Owner, Approver, Version, Revision, Status, EffectiveDate, Tags, BasisCard).
- **`TemplateLoader`** + **`TemplateEngine`** — загрузка и рендеринг шаблонов.
- **`MarkdownService`** — YAML Parse/Compose.
- Поддержка 7 фабричных шаблонов (Code, Policy, BP, ST, PM, WM, Basis), загружаемых read-only.

---

## [1.0.0] - 2026-06-25 — Initial release

- Базовое Avalonia-приложение с MVVM-архитектурой.
- CommunityToolkit.Mvvm для `[ObservableProperty]` / `[RelayCommand]`.
- Avalonia 12.0.5 + .NET 8.0.

[1.6.0]: #160---2026-06-25--service-layer--overlay-refactor
[1.5.0]: #150---2026-06-25--phase-5-ux-hardening
[1.4.0]: #140---2026-06-25--phase-4-структурированный-документ
[1.3.0]: #130---2026-06-25--phase-3-docx-форматирование
[1.2.0]: #120---2026-06-25--phase-2-справочники-и-basis-cards
[1.1.0]: #110---2026-06-25--phase-1-базовый-редактор
[1.0.0]: #100---2026-06-25--initial-release
