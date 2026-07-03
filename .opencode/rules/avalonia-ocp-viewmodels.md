---
paths: ["**/*ViewModel.cs", "**/*Factory.cs"]
topic: viewmodels
---
# ViewModels

## Rules
```yaml
R1: "∀ field_creation ⇒ ¬ switch_case. ∃ IFieldViewModelRegistry ∧ IFieldViewModelFactory ∈ Strategy"
```

## Anti-patterns
```yaml
- p: "switch-case over field types to instantiate ViewModels"
  fix: "Use Registry and Strategy pattern (IFieldViewModelRegistry, IFieldViewModelFactory)"
```