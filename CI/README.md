# Сборка, тестирование и выпуск R-D Gen Doc

## GitHub Actions

Workflow: `.github/workflows/release.yml` — **CI and Release**.

| Событие | Сборка | xUnit | ZIP + SHA256 | GitHub Release |
|---|---|---|---|---|
| Pull request | Да | Да | Actions artifact | Нет |
| Push в `main` | Да | Да | Actions artifact | `snapshot-<run_number>`, prerelease |
| Push тега `v*` | Да | Да | Actions artifact | Версионированный стабильный релиз |
| Ручной workflow_dispatch | Да | Да | Actions artifact | Нет |

Публикация релиза выполняется **только после успешной** сборки, тестов и проверки SHA256.
У CI минимальные права: запись в Releases выдана только job `release`.
Артефакты можно получить как из вкладки Actions, так и из GitHub Releases
(для push в main или version tag).

## Сборка локально (PowerShell 7 + .NET SDK 10)

```powershell
pwsh -File ./CI/build-release.ps1
pwsh -File ./CI/run-tests.ps1
```

Для сборки с тестовым gate используется `build-release.ps1`.
Переключатель `-SkipTests` предназначен только для отладки сборки
и не применяется в CI.

Готовые файлы:
```text
CI/Output/
  R-D_Gen_doc-Release-win-x64.zip
  R-D_Gen_doc-Release-win-x64.zip.sha256
CI/Logs/
  build.log
  test-results.trx
```

## Состав дистрибутива

- `AvaloniaApp.exe` — Windows x64, self-contained, single-file.
- `appsettings.example.json` и `appsettings.README.md` — справка по локальной конфигурации.
- Только необходимые дополнительные файлы, которые выдал `dotnet publish`
  (включая native DLL, если они необходимы для Avalonia/SQLite).

На целевом ПК не требуется устанавливать .NET Runtime или SDK.
Для задания реальных локальных путей создайте рядом с приложением
`appsettings.json` по примеру; этот файл не включается в GitHub Release.

## Меры против избыточных зависимостей

- `RuntimeIdentifier` не задан глобально: обычные build/test не скачивают
  Windows self-contained runtime packs.
- В Release не восстанавливаются `AvaloniaUI.DiagnosticsSupport`
  и `StyleCop.Analyzers`; они доступны только для Debug.
- Удалены избыточные ссылки на `Microsoft.CodeAnalysis.NetAnalyzers`
  (анализаторы SDK уже встроены), `ReactiveUI.Avalonia` (MVVM использует
  CommunityToolkit.Mvvm) и ненужный тестовый `coverlet.collector`.
- `PublishReadyToRun=false` уменьшает объём дистрибутива; предварительная
  JIT-компиляция при запуске возможна.
- `IncludeAllContentForSelfExtract=false` исключает ненужное
  извлечение всего содержимого single-file.
- `InvariantGlobalization` не включён: сохраняется корректная работа
  культурозависимых форматов для русскоязычной документации.

`PublishTrimmed` и NativeAOT намеренно **не** включаются: Avalonia, DI,
OpenXML и сериализация могут использовать reflection. Включение trim
без отдельной проверки функциональности может повредить генерацию документов.

## Проверка готового архива

```powershell
Get-FileHash CI/Output/R-D_Gen_doc-Release-win-x64.zip -Algorithm SHA256
```

Хеш должен совпасть с файлом `.sha256`.
После сборки запустите `AvaloniaApp.exe` на Windows x64 и проверьте
открытие UI, создание документа, загрузку БД и экспорт DOCX.

**Важно:** `Tools/SmokeTest.cs` использует жёсткий каталог `D:\Avalonia`
и пока не является переносимым автоматическим gate.
