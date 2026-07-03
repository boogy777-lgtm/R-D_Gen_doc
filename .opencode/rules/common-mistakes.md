---
paths: ["**/*.cs", "**/*.axaml"]
topic: common-mistakes
---

# Common Mistakes

## Architecture Violations

| ❌ Error | ✅ Fix |
|---|---|
| Бизнес-логика в ViewModel | Перенести в Use Case / Domain Service |
| ViewModel зависит от Window/Control/StorageProvider | Внедрить IDialogService / IFileService / IUriLauncherService |
| Domain ссылается на Avalonia/SQLite/OpenXML | Разорвать зависимость; Models — только CommunityToolkit.Mvvm |
| Use Case вызывает `File.ReadAllText` / `new SqliteConnection` | Через интерфейс: IFileSystemService / IDocumentRepository |
| Статические справочники `Defaults.Forms` / `ReferenceDataService` | DI: IReferenceDataService, IFormsCatalog, IMetadataCatalog |

## C# / MVVM Violations

| ❌ Error | ✅ Fix |
|---|---|
| `async void` (кроме UI event handlers) | `async Task` |
| `async void` в RelayCommand | `[AsyncRelayCommand]` → returns Task |
| `async Task` без `CancellationToken` | Добавить CancellationToken ct |
| Синхронный I/O (`File.ReadAllText`, `File.WriteAllText`) | `await File.ReadAllTextAsync()`, IFileSystemService |
| Магические строки в парсерах/генераторах Markdown | MarkdownSyntaxDictionary |
| switch/case по типам полей во ViewModel | IFieldViewModelRegistry + фабрики (OCP) |
| Модель: `ObservableObject` без `partial` + `[ObservableProperty]` | `public partial class X : ObservableObject { [ObservableProperty] private T _field; }` |
| `x:CompileBindings=True` в DataTemplate с `$parent` | `x:CompileBindings=False` (runtime-resolve) |

## DocumentChangeEngine Violations

| ❌ Error | ✅ Fix |
|---|---|
| Прямая мутация `body.Terms.Add()` в обход Engine | Удалять/добавлять ТОЛЬКО через Engine |
| Новая коллекция в DocumentBody, но забыт RegisterCollection | Добавить RegisterCollection в HookCollectionChanges |
| ServiceProperties не обновлён при новом служебном поле | Добавить имя поля в HashSet ServiceProperties |
| Отключить тест из-за падения вместо исправления архитектуры | Исправить нарушение, тест должен проходить |

## UI / UX Violations

| ❌ Error | ✅ Fix |
|---|---|
| Code-behind с бизнес-логикой | Вся логика → VM + Use Case; code-behind = только InitializeComponent |
| Окно без подтверждения при закрытии с несохранёнными данными | Проверять _isDirty, показывать FAContentDialog |
| IsBusy не сбрасывается при ошибке | try/finally { IsBusy = false; } |
| Невидимый выделитель текста | SelectionBrush с достаточным контрастом |
| MessageBox / отдельные OS-окна | FAContentDialog (FluentAvalonia) |
| Стандартный Window | FAAppWindow (FluentAvalonia) |
