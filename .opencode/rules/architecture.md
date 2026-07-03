---
paths: ["**/*.cs"]
topic: architecture
---

# Architecture (AvaloniaApp)

## Clean Architecture Layers

```yaml
layers:
  Domain/Entities:
    - Models (DocumentBody, TermPair, BodyAppendix, BodySection, ChangeLogRow, etc.)
    - interfaces: IDocumentChangeEngine, IEngineCore, ISingleFieldMutator, ICollectionMutator, ITreeMutator
    - dep: ¬ Avalonia, SQLite, OpenXML, FileSystem, ViewModels, Views
    - exception: CommunityToolkit.Mvvm ObservableObject для data-transfer моделей (¬ тянет UI)
  Application/Use Cases:
    - Use Cases (оркестрация)
    - dep: только Domain + абстракции (интерфейсы infra)
  Interface Adapters:
    - Services (Services/), ViewModels (ViewModels/)
    - dep: Domain + Application, ¬ Views, ¬ Avalonia
  Frameworks & Drivers:
    - Views (Avalonia, FluentAvalonia), SQLite, OpenXML, FileSystem
```

## Dependency Rule

- Наружу → внутрь: Views/Infra → Services/ViewModels → Use Cases → Domain
- Внутренние слои ¬ знают о внешних
- Все внешние зависимости — через DI-интерфейсы

## DI Composition Root

```yaml
location: Composition/ServiceCollectionExtensions.cs
method: AddAvaloniaAppServices(IServiceCollection)
rules:
  - Singletons: stateless-сервисы, каталоги (IReferenceDataService, IFormsCatalog, IMetadataCatalog, FieldViewModelRegistry)
  - Transients: ViewModel/View при resolve
  - Все интерфейсы регистрируются через DI, ¬ ServiceLocator, ¬ static
```

## DocumentChangeEngine — Facade (SRP)

```yaml
facade: DocumentChangeEngine.cs (Services/)
interfaces: IDocumentChangeEngine, ISingleFieldMutator, ICollectionMutator, ITreeMutator, IEngineCore (ISP)
delegates_to:
  ChangeLogManager: ЛРИ, ревизии, read-only флаги
  NumberingEngine: Position/Level/HierarchyPath/GlobalTableIndex
  CollectionObserver: подписки на изменения коллекций/элементов
pattern: RegisterCollection<T>(coll, nestedSelector?) — 1 строка на коллекцию, HashSet<object> anti-dup, HashSet<string> ServiceProperties фильтрует служебные поля
```

## DocxGeneration/ Modules (SRP split of DocxGenerator)

| Модуль | Назначение |
|---|---|
| `BlockConverterRegistry.cs` | Реестр IBlockConverter-ов (OCP) |
| `DocxBlockDispatcher.cs` | Диспетчер блоков-стратегий |
| `DocxBlockRecognizers.cs` | Распознавание Markdig-блоков |
| `DocxBodyBuilder.cs` | Сборка тела документа |
| `DocxDocumentBuilder.cs` | Оркестратор сборки всего DOCX |
| `DocxHeaderFooterBuilder.cs` | Колонтитулы |
| `DocxInlinesProcessor.cs` | Inline-элементы |
| `DocxPageLayout.cs` | Параметры страницы |
| `DocxRunHelpers.cs` | Run-хелперы |
| `DocxStylesFactory.cs` | Фабрика стилей |
| `BlockConverterConstants.cs` | Константы конвертеров |
| `DispatchHelpers.cs` | Хелперы диспетчеризации |

Устаревшие классы: `DocxGenerator.cs`, `DocxProfile.cs`, `DocxConverters/` — фасад/адаптер к новому DocxGeneration.

## MarkdownSyntaxDictionary

```yaml
location: Services/Markdown/MarkdownSyntaxDictionary.cs
role: единый источник правды для Markdown-разметки
consumers: DocumentBodyParser, DocumentBodyToMarkdown
content:
  - заголовки секций (PurposeHeader, ScopeHeader, TermsHeader)
  - маркеры приложений (AppendixPrefix)
  - заголовки таблицы ЛРИ (ChangeLogTableHeader)
  - паттерны парсинга (Lazy<Regex>)
  - инварианты нумерации (FirstUserSectionNumber, Level2PrefixLength)
rule: ¬ магические строки в парсерах/генераторах
```

## FieldViewModelRegistry (OCP)

```yaml
interfaces: IFieldViewModelFactory, IFieldViewModelRegistry
location: ViewModels/FieldViewModelRegistry.cs, ViewModels/IFieldViewModelFactory.cs
factories:
  - SingleFieldViewModelFactory
  - CollectionFieldViewModelFactory
  - TreeFieldViewModelFactory
pattern: реестр собирает все IFieldViewModelFactory → маршрутизирует descriptor → VM по типу
rule: ¬ switch/case на типах полей; новый тип = новая фабрика, без изменений в MainWindowViewModel
```

## Seed Data — ReferenceData/Forms/Metadata

```yaml
location: Services/Defaults/ReferenceData.cs (+ Forms.cs, Metadata.cs)
interfaces: IReferenceDataService, IFormsCatalog, IMetadataCatalog (DI-singletons)
rule: единственная точка редактирования предустановленных строк, статусов, ролей, дисциплин
first_run: ReferenceData → template.db + docs/_справочники/ + UI ComboBox-ы
```

## Cross-references

- @.opencode/rules/avalonia-mvvm.md
- @.opencode/rules/avalonia-fluent-ui.md
- @.opencode/rules/avalonia-ocp-viewmodels.md
- @.opencode/rules/avalonia-srp-managers.md
- @.opencode/rules/avalonia-markdown.md
- @.opencode/rules/avalonia-clean-arch.md
