---
paths: ["**/*Engine.cs", "**/*Manager.cs"]
topic: srp
---
# SRP

## Rules
```yaml
R1: "∀ mutation_logic ⇒ ∈ (ChangeLogManager ∨ NumberingEngine ∨ CollectionObserver). Engine = Facade. ¬ GodObject"
```

## Anti-patterns
```yaml
- p: "monolithic DocumentChangeEngine handling all mutation logic"
  fix: "Split into micro-managers (ChangeLogManager, etc.) and use Engine as Facade"
```