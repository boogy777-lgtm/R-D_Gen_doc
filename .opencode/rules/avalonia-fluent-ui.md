---
paths: ["**/*.axaml", "**/*.axaml.cs"]
topic: ui
---
# UI

## Rules
```yaml
R1: "∀ Window ⇒ ⊆ FAAppWindow. ∀ Dialog ⇒ ⊆ FAContentDialog. ¬ OS_Window_Dialog"
```

## Anti-patterns
```yaml
- p: "Inheriting from standard Window or opening separate OS windows for messages"
  fix: "Inherit from FAAppWindow, use FAContentDialog for in-app overlay dialogs"
```