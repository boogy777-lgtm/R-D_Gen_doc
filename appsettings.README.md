# appsettings.json — Конфигурация / Configuration

> `appsettings.json` ∈ `.gitignore`. Эталон: [`appsettings.example.json`](appsettings.example.json).

## Быстрый старт

```powershell
Copy-Item appsettings.example.json appsettings.json
notepad appsettings.json
```

## appsettings.example.json (эталон)

```json
{
  "ConnectionStrings": {
    "DraftsDb": "Data Source=path/to/local_drafts.db"
  },
  "FactoryPaths": {
    "Root": "path/to/_factory"
  },
  "ReferenceData": {
    "SubdivisionsMd": "path/to/ISM_doc-types-and-subdivisions.md"
  },
  "UserSettings": {
    "DefaultAuthor": ""
  }
}
```

## Поля

### `ConnectionStrings.DraftsDb` — SQLite БД черновиков

ADO.NET-строка. Файл `.db` создаётся авто при первом запуске (`DocumentSchema.EnsureCreated()`).

**Fallback:** `template.db` (рядом с `.exe`).

```json
"Data Source=D:\\data\\local_drafts.db"
```

### `FactoryPaths.Root` — папка шаблонов `_factory/`

Ожидаемая структура:
```
_factory/
├── T_Code.md, T_Policy.md, T_BP.md, T_ST.md, T_PM.md, T_WM.md, T_Basis.md
└── _shields/
    ├── Shield_ST.md, Shield_PM.md, Shield_WM.md
```

**Fallback:** `docs/_шаблоны` (рядом с `.exe`).  
Шаблоны **read-only** — приложение не пишет в `_factory/`.

### `ReferenceData.SubdivisionsMd` ⚠️ DEPRECATED

~~Путь к MD-файлу со справочником подразделений.~~

> Справочники (статусы, роли, дисциплины, DocTypes, Departments, стартовые секции) теперь загружаются из **`Services/Defaults/ReferenceData.cs`** через DI-сервис **`IReferenceDataService`**.  
> Поле `SubdivisionsMd` **скоро будет удалено** из кода. Пока осталось для обратной совместимости.

**Fallback:** `ref/subdivisions.md`.

### `UserSettings.DefaultAuthor` — автор по умолчанию

Ф.И.О. (напр. `"Иванов И.И."`). Подставляется в `DocumentBodyFactory.CreatePrefilledChangeLogRow()` → поле `Author` в ЛРИ.

**Пустая строка** = пользователь заполняет вручную.

## Fallback-пути (если `appsettings.json` отсутствует)

| Параметр | Fallback |
|---|---|
| `DraftsDb` | `template.db` (рядом с `.exe`) |
| `FactoryRoot` | `docs/_шаблоны` (рядом с `.exe`) |
| `SubdivisionsMd` | `ref/subdivisions.md` (рядом с `.exe`) |
| `DefaultAuthor` | `""` |

Логика: `AppSettings` (static ctor) → `appsettings.json` → если не найден/ошибка → fallback.

## `appsettings.Development.json` (рекомендовано для dev)

ASP.NET Core-стиль: положите рядом с `appsettings.json` — переопределяет поля для dev-окружения.

```json
{
  "UserSettings": {
    "DefaultAuthor": "Разработчик"
  }
}
```

Не кладите в него реальные пути — `appsettings.Development.json` может попасть в Git.

## Поведение при неверных путях

| Сценарий | Результат |
|---|---|
| Нет файла `.db` | Авто-создание + `EnsureCreated()` |
| Нет `_factory/` | `TemplateLoader.LoadFromDirectory()` → пустой список, создание документов заблокировано |
| Нет `ref/subdivisions.md` | Fallback в лог, справочники грузятся из `ReferenceData.cs` |

Ошибки → `StatusMessage` (нижняя панель UI).

## Production deployment

```powershell
Copy-Item appsettings.example.json appsettings.json
notepad appsettings.json
# Проверить существование путей:
Get-ChildItem "D:\path\to\_factory"       # должны быть T_*.md
Get-ChildItem "D:\path\to\ISM_doc-...md"  # (deprecated, скоро не нужно)
dotnet run
```
