# AvaloniaApp
> C# 14 / .NET 10. Clean Architecture + MVVM + DI + OCP.
> SDK: system 10.0.301 (`C:\Program Files\dotnet`). Решение: `AvaloniaApp.slnx`.
> NRT: nullable enable. Детали: @.opencode/rules/csharp-style.md.

## Gate
- ∀ завершение задачи ⇒ @.opencode/rules/build-gate.md (build → test → smoke). Не пройден = не сделано.

## Rules Index
| Domain | Rule file |
|---|---|
| Architecture | @.opencode/rules/architecture.md |
| MVVM / ViewModel-First | @.opencode/rules/avalonia-mvvm.md |
| C# style, DI, async | @.opencode/rules/csharp-style.md |
| Build gate | @.opencode/rules/build-gate.md |
| UX checklist | @.opencode/rules/ux-checklist.md |
| Common mistakes | @.opencode/rules/common-mistakes.md |
| FluentAvalonia UI | @.opencode/rules/avalonia-fluent-ui.md |
| Field VM Registry (OCP) | @.opencode/rules/avalonia-ocp-viewmodels.md |
| SRP managers | @.opencode/rules/avalonia-srp-managers.md |
| Markdown syntax | @.opencode/rules/avalonia-markdown.md |
| DI: ref data interfaces | @.opencode/rules/avalonia-clean-arch.md |

## Seed Data — ReferenceData (Project-Specific)
- **Единственная точка** предустановленных строк: `Services/Defaults/ReferenceData.cs` (статусы, роли, дисциплины, метаданные, типы документов, орг. структура, стартовые секции).
- Интерфейсы: `IReferenceDataService`, `IFormsCatalog`, `IMetadataCatalog` → DI-синглтоны.
- Каталоги рядом: `Services/Defaults/Forms.cs`, `Services/Defaults/Metadata.cs`.
- При первом запуске: `ReferenceData` → `template.db` + `docs/_справочники/` + UI ComboBox-ы.

## Architecture (Project-Specific Highlights)
- **DI Composition Root**: `Composition/ServiceCollectionExtensions.AddAvaloniaAppServices()` — единая точка регистрации. Слои: Models → Use Cases → Services/ViewModels → Views/Infrastructure.

### DocumentChangeEngine — Core Mutation Engine
- Facade (SRP) — делегирует: `ChangeLogManager` (ЛРИ), `NumberingEngine` (нумерация), `CollectionObserver` (подписки).
- 5 ролевых интерфейсов (ISP): `IDocumentChangeEngine`, `ISingleFieldMutator`, `ICollectionMutator`, `ITreeMutator`, `IEngineCore`.
- `RegisterCollection<T>(coll, nestedSelector?)` — 1 строка на коллекцию; `HashSet<object>` anti-dup; `HashSet<string> ServiceProperties` фильтрует служебные поля.
- Прямая мутация `body.Terms.Add()` в обход Engine → **запрещена**.

### DocxGeneration/ Modules
`DocxGenerator.cs` расщеплён по SRP в `Services/DocxGeneration/`:
`BlockConverterRegistry` (реестр IBlockConverter), `DocxBlockDispatcher`, `DocxBlockRecognizers`, `DocxBodyBuilder`, `DocxDocumentBuilder` (оркестратор), `DocxHeaderFooterBuilder`, `DocxInlinesProcessor`, `DocxPageLayout`, `DocxRunHelpers`, `DocxStylesFactory`, `BlockConverterConstants`, `DispatchHelpers`.

### MarkdownSyntaxDictionary
- `Services/Markdown/MarkdownSyntaxDictionary.cs` — единый источник правды. Заголовки секций, маркеры приложений, таблица ЛРИ, Lazy\<Regex\>. Все парсеры/генераторы читают ТОЛЬКО отсюда — ¬ магические строки.

### FieldViewModelRegistry (OCP)
- `IFieldViewModelFactory` / `IFieldViewModelRegistry`. 3 фабрики: SingleField, CollectionField, TreeField. Реестр собирает все → маршрутизирует descriptor → VM. Новый тип поля = новая фабрика, ¬ правка MainWindowViewModel.

### Static → DI
- `ReferenceData`, `Forms`, `Metadata` стали DI-сервисами с интерфейсами. ¬ `using Defaults.Forms` напрямую — всегда через DI.

## Build / Test / Smoke
```bash
dotnet build AvaloniaApp.slnx
dotnet test AvaloniaApp.Tests
dotnet run --project AvaloniaApp.csproj -- --smoke
```
Smoke test: справочники, шаблоны, DOCX-генерация, E2E. Gate: @.opencode/rules/build-gate.md.

## CI (Standalone Release)
- Скрипты: `CI/build-release.ps1` (полный пайплайн), `CI/run-tests.ps1` (только тесты).
- Артефакт: `CI/Output/AvaloniaApp-Release-win-x64-*.zip` — self-contained single-file exe.
- Логи: `CI/Logs/build-*.log`, `CI/Logs/test-results-*.trx`.
- Подробнее: @CI/README.md.

## NuGet Packages (Key)
```yaml
Avalonia: 12.0.5
FluentAvaloniaUI: 3.0.0
CommunityToolkit.Mvvm: 8.4.1
Markdig: 0.40.0
DocumentFormat.OpenXml: 3.3.0
Microsoft.Data.Sqlite: 10.0.0
Microsoft.Extensions.DI: 10.0.0
YamlDotNet: 16.3.0
NetArchTest.Rules: tests
xunit: tests
```

## Do Not Touch
- `bin/`, `obj/` (generated)
- Системный .NET SDK (`C:\Program Files\dotnet`)
- Локальные пути/secrets в `appsettings.json` → `appsettings.Development.json` или env

## Manifest
```yaml
entry: AvaloniaApp.csproj
tests: AvaloniaApp.Tests/AvaloniaApp.Tests.csproj
config: opencode.json
composition: Composition/ServiceCollectionExtensions.cs
engine: Services/DocumentChangeEngine.cs
body: Models/DocumentBody.cs
syntax: Services/Markdown/MarkdownSyntaxDictionary.cs
seed: Services/Defaults/ReferenceData.cs
registry: ViewModels/FieldViewModelRegistry.cs
```
