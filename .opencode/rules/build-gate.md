---
paths: ["AvaloniaApp.slnx", "AvaloniaApp.Tests/**/*.cs"]
topic: build-gate
---

# Pre-Completion Gate

## Rule

> ∀ завершение задачи ⇒ пройти build → test → smoke. Задача не выполнена, пока все 3 шага не проходят.

## Steps

```yaml
1_build:
  command: dotnet build AvaloniaApp.slnx
  must: pass (0 errors)
  sdk: см. AGENTS.md (SDK: system)

2_test:
  command: dotnet test AvaloniaApp.Tests
  tests: NetArchTest + domain + smoke
  must: all pass
  veto: ¬ отключать тесты, ¬ комментировать ассерты

3_smoke:
  command: dotnet run --project AvaloniaApp.csproj -- --smoke
  covers: справочники, шаблоны, DOCX-генерация, E2E
  must: pass (exit code 0)

4_release:
  command: .\CI\build-release.ps1
  artifact: CI\Output\AvaloniaApp-Release-win-x64-*.zip (standalone single-file)
  optional: true (для релизных билдов)
```

## Failure Handling

```yaml
on_failure: исправить нарушения, ¬ обойти gate
retry: после исправления — полный прогон (1 → 2 → 3)
```

## Restart opencode After

- ∆ AGENTS.md
- ∆ .opencode/rules/*.md
- ∆ opencode.json
