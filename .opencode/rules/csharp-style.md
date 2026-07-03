---
paths: ["**/*.cs"]
topic: csharp
---

# C# Style & DI (AvaloniaApp)

## Language Version

Версия языка/рантайма/NRT: @AGENTS.md — проектный манифест.

## Primary Constructors

```yaml
rule: ∀ class ⇒ primary constructor (C# 12+)
  service: "public class MyService(IDep dep) { }"
  viewmodel: "public partial class MyVm(IService svc, IEngine eng) : ViewModelBase { }"
anti: "public class X { private readonly IDep _dep; public X(IDep dep) { _dep = dep; } }"
fix: убрать бойлерплейт, использовать primary constructor
```

## Collection Expressions

```yaml
rule: ∀ collection_init ⇒ [] (C# 12+)
  array: "int[] arr = [1, 2, 3];"
  list: "List<string> items = [\"a\", \"b\"];"
  span: "Span<int> s = [1, 2, 3];"
anti: "new List<T> { ... }", "new T[] { ... }"
fix: использовать []
```

## Dependency Injection

```yaml
rule: ∀ dependency ⇒ constructor injection (primary constructor)
  - Services: inject интерфейсы, ¬ конкретные классы
  - ViewModels: inject через DI, ¬ ServiceLocator, ¬ static
  - Views: inject ViewModel через DI (composition root)
interface: все сервисы — I-префикс интерфейса
registration: "Composition/ServiceCollectionExtensions.AddAvaloniaAppServices()"
anti:
  - ServiceLocator / static ServiceProvider
  - new() в обход DI
  - ViewModel без интерфейсных зависимостей
```

## async / await

```yaml
rule: ∀ I/O ⇒ async/await + CancellationToken
  - File, DB, HTTP, Clipboard: async only
  - Методы возвращают Task/ValueTask
  - CancellationToken ct как последний параметр
anti:
  - async void (кроме UI event handlers)
  - синхронный I/O (File.ReadAllText и т.д.)
  - Task без CancellationToken
  - .Result / .Wait() — deadlock risk
```

## const vs static readonly

```yaml
const: для литералов времени компиляции (MarkdownSyntaxDictionary строки)
static readonly: для значений, вычисляемых в runtime (Lazy<Regex>, HashSet<string>)
```

## Nullable

```yaml
rule: NRT enabled — ∀ reference тип подразумевает non-null
  - ? для nullable значений
  - ! только при доказанной non-null после проверки
```

## Naming

```yaml
classes: PascalCase
methods: PascalCase
properties: PascalCase
fields: _camelCase (private), camelCase для [ObservableProperty]
locals: camelCase
interfaces: IPascalCase
async_methods: VerbAsync (GetAsync, SaveAsync)
```

## Code Organization

```yaml
one_class_per_file: true
namespace: file-scoped (C# 10+)
usings: global usings в GlobalUsings.cs / implicit usings
```

## Cross-references

- @.opencode/rules/architecture.md — Clean Architecture, DI Composition
- @.opencode/rules/build-gate.md — pre-completion gate
- @.opencode/rules/common-mistakes.md — список ошибок
