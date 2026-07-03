---
paths: ["AvaloniaApp/**/*.cs", "AvaloniaApp/**/*.{axaml,xaml}"]
topic: avalonia-mvvm
---

# Avalonia MVVM (AvaloniaApp)

## ViewModel-First

```yaml
rule: ViewModel-First. View — тень генотипа (VM).
  - ContentControl с DataType → auto-resolve View
  - ViewLocator.cs: VM-имя → View-имя
  - ¬ View_First: View не создаёт VM (new View { DataContext = new VM() } — запрещено)
```

## ViewModels

```yaml
base: ViewModelBase : ObservableObject (CommunityToolkit.Mvvm)
class: public partial class
constructor: DI через primary constructor
properties: "[ObservableProperty] на private partial полях"
commands: "[RelayCommand] / [AsyncRelayCommand] на private методах"
async: команды возвращают Task; async void — только UI-обработчики
naming: VerbNounCommand
dispose: IDisposable при подписках/CTS/CancellationTokenSource
```

✅ Positive:
```csharp
public partial class MainWindowViewModel(
    IDocumentChangeEngine engine,
    IDialogService dialog,
    IFieldViewModelRegistry fieldRegistry) : ViewModelBase
{
    [ObservableProperty] private string _statusMessage = "Готово";
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task GenerateAsync(CancellationToken ct)
    {
        try { IsBusy = true; await engine.GenerateAsync(ct); }
        finally { IsBusy = false; }
    }
}
```

❌ Anti-patterns:
- VM создаёт Window / StorageProvider / File напрямую
- switch/case по типам полей (→ IFieldViewModelRegistry)
- Бизнес-логика в VM (→ Use Case / Domain Service)

## FieldViewModelRegistry (OCP)

```yaml
interfaces: IFieldViewModelFactory, IFieldViewModelRegistry
location: ViewModels/FieldViewModelRegistry.cs
factories: SingleFieldVM, CollectionFieldVM, TreeFieldVM — каждая 1 тип дескриптора
rule: новый тип поля = новая фабрика, ¬ изменение MainWindowViewModel
```

## Views

```yaml
datatype: x:DataType="vm:ViewModelName" на корневом элементе
bindings: compiled bindings {Binding Property}
codebehind: только InitializeComponent + view-level обработчики (drag-drop)
window: FAAppWindow (FluentAvalonia), ¬ Window
dialog: FAContentDialog (FluentAvalonia), ¬ MessageBox / отдельные окна
$parent: x:CompileBindings=False в DataTemplate с $parent-привязкой
```

✅ Positive:
```xml
<faui:FAAppWindow xmlns:vm="using:AvaloniaApp.ViewModels"
                  x:DataType="vm:MainWindowViewModel"
                  x:Class="AvaloniaApp.Views.MainWindow">
  <Design.DataContext><vm:MainWindowViewModel /></Design.DataContext>
  <TextBlock Text="{Binding StatusMessage}" />
</faui:FAAppWindow>
```

## Services

```yaml
location: Services/
interface: I-префикс
dependency: ViewModel работает через интерфейсы
views: не зависят от AvaloniaApp.Views
io: async/await + CancellationToken
```

## Models / Domain

```yaml
purpose: чистые данные + доменная логика
rule:
  domain_pojo: record/POCO (расчётная логика, value objects) — НЕ ObservableObject
  data_model: ObservableObject + [ObservableProperty] (TermPair, BodyAppendix, ChangeLogRow) — для change notification
collections: DocumentBody содержит ObservableCollection<T> — прямые ссылки, не копии
rule_constraint: CommunityToolkit.Mvvm.ObservableObject ¬ зависит от Avalonia → допустим в Domain
```

## Hard Prohibitions

```yaml
- ViewModel ¬ ссылается на Avalonia.Controls, Window, StorageProvider, FileSystem
- Services ¬ ссылаются на Views
- Domain POCO ¬ ссылается на ViewModels/Views/Services
- Code-behind ¬ содержит бизнес-логики
- ¬ async void (кроме UI event handlers)
- ¬ магические строки (→ MarkdownSyntaxDictionary, константы)
- ¬ прямая мутация коллекций DocumentBody в обход Engine
- ¬ x:CompileBindings=True в DataTemplate с $parent
```

## Language

- UI-строки — русский
- Идентификаторы (классы, свойства, методы, переменные) — английский

## Cross-references

- @.opencode/rules/architecture.md — DocumentChangeEngine, DocxGeneration
- @.opencode/rules/csharp-style.md — C# 14, DI, async
- @.opencode/rules/build-gate.md — pre-completion gate
- @.opencode/rules/ux-checklist.md — UX
- @.opencode/rules/common-mistakes.md — ошибки
- @.opencode/rules/avalonia-fluent-ui.md — FAAppWindow / FAContentDialog
