# 🌌 Avalonia App: Developer Guidelines & Architecture Manifesto

Welcome to the project! This document serves as the foundational guide for understanding our architectural philosophy and engineering practices. Our goal is to build a highly scalable, performant, and maintainable C# Avalonia desktop application.

---

## 1. The Architectural Eidos: Strict Separation

Our architecture is strictly divided into the **Platonic Genotype** (Business Logic & State) and the **Phenotypic Shadow** (User Interface). 

### 1.1 ViewModel (The Pure Idea)
*   **Zero UI Knowledge:** A ViewModel must **never** reference Avalonia controls, colors, or UI namespaces. 
*   **State & Logic Only:** Every distinct UI state must be represented by a ViewModel property. All user interactions are routed through `ICommand` (via `CommunityToolkit.Mvvm`).

### 1.2 View (The Shadow)
*   **Zero Code-Behind:** The `.xaml.cs` file must only contain `InitializeComponent()`. **No event handlers or business logic are allowed in Code-Behind.**
*   **ViewModel-First Approach:** We never instantiate Views directly with their ViewModels (e.g., `new View { DataContext = new VM() }` is **strictly forbidden**).
*   **DataTemplates:** Views are projected onto the screen dynamically using `DataTemplate` mapping in AXAML. The application routes pure ViewModels, and Avalonia resolves the correct View automatically.

---

## 2. UI & AXAML Fundamentals

*   **Declarative Bindings:** No direct control manipulation. Everything is updated via `Binding` (OneWay, TwoWay) reacting to `INotifyPropertyChanged`.
*   **FluentAvalonia:** We utilize modern Fluent Design principles. Use `FAAppWindow` for the main window and `FAContentDialog` for dialogs/modals.
*   **Virtualization:** Any collection expected to exceed **50 items** must use `VirtualizingStackPanel` (or `ListBox`/`DataGrid` with virtualization enabled) to prevent layout performance degradation.

---

## 3. Scale Dynamics & Performance (Big-O Constraints)

We enforce strict algorithmic limits to ensure the application remains responsive at scale:

*   **O(1) Collection Mutations:** Adding or removing elements (e.g., `rule_n_plus_1`) must happen in constant time. Avoid full list re-enumerations or massive array copies when adding to the end of a collection. Use `ObservableCollection<T>`.
*   **O(N) UI State Updates:** Updating the UI state (`rule_n_o`) is capped at O(N).
*   **Debouncing:** Any frequent operations triggered by UI (e.g., Markdown preview rendering, live search) **must** be debounced by at least **250ms**.
*   **Tree Traversal:** Algorithms traversing DOM/AST trees must strictly operate within the parent scope. O(A^2) complexity in tree traversal is forbidden.

---

## 4. C# Coding Standards & Stack

*   **C# 14 Features:** Embrace modern C# features. 
    *   Use **Nullable Reference Types** (`#nullable enable` context).
    *   Use **async/await** for all non-blocking operations.
    *   Mandatory **try-catch blocks** for all I/O operations (file system, network) to prevent unexpected crashes.
*   **Core Libraries:**
    *   `CommunityToolkit.Mvvm` (Source Generators for `[ObservableProperty]`, `[RelayCommand]`).
    *   `Markdig` (Markdown processing).
    *   `OpenXml` (Document generation/parsing).
    *   `YamlDotNet` (Configuration and serialization).
    *   `FluentAvalonia.UI` (Modern UI controls).

---

## 5. Dependency Injection & Extensibility (OCP)

*   **DI Container:** All services and viewmodels must be registered in and resolved from `Microsoft.Extensions.DependencyInjection`. **Static dictionaries, singletons, and service locators are strictly forbidden.**
*   **Open-Closed Principle (OCP):** Use **Registries and Factories** for generating fields or resolving handlers. Avoid giant `switch-case` blocks when mapping types. Adding a new feature/type should only require registering a new class in the DI container.

---

## 6. Core Systems & Context

Get familiar with these central facades and systems:

*   **DocumentChangeEngine:** A unified Facade handling document modifications. Under the hood, it orchestrates subsystems like `ChangeLogManager` and `NumberingEngine`. Do not bypass the Facade to talk to the subsystems directly.
*   **MarkdownSyntaxDictionary:** The single source of truth for Markdown syntactical rules and mappings across the application.
*   **Isomorphic Updates:** State mutations should transparently reflect from the underlying AST (Abstract Syntax Tree) up to the UI via bound ViewModels.

---
*Follow these principles, and your code will remain robust, testable, and deeply aligned with the project's foundational eidos.*