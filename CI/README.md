# CI

## GitHub Actions (автоматический релиз)

Релиз запускается автоматически при пуше тега `v*` или вручную через Actions → Release → Run workflow.

```yaml
Триггеры:
  push tags v*  → авто-билд + GitHub Release с .zip
  workflow_dispatch → ручной запуск с выбором конфигурации

Что делает:
  checkout → setup .NET 10 → restore → build → test → publish standalone → upload .zip → GitHub Release
```

Конфигурация: `.github/workflows/release.yml`.

## Локальные скрипты

| Скрипт | Назначение |
|---|---|
| `build-release.ps1` | Полный пайплайн: restore → build → test → publish standalone → zip |
| `run-tests.ps1` | Только тесты с trx-логом |

## Запуск

```powershell
# Полный релизный билд
.\CI\build-release.ps1

# Только тесты
.\CI\run-tests.ps1

# Билд без тестов
.\CI\build-release.ps1 -SkipTests
```

## Артефакты

```
CI/
├── Output/          ← AvaloniaApp-Release-win-x64-*.zip (standalone exe)
├── Logs/
│   ├── build-*.log            ← полный лог пайплайна
│   └── test-results-*.trx     ← результаты xUnit (формат Visual Studio)
├── build-release.ps1
└── run-tests.ps1
```

## Параметры публикации (из .csproj)

```yaml
PublishSingleFile: true        # один .exe без россыпи dll
SelfContained: true            # встроенный .NET runtime
PublishReadyToRun: true        # AOT-предкомпиляция (быстрый старт)
RuntimeIdentifier: win-x64
```
