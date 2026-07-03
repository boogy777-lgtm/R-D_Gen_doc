---
paths: ["**/*.axaml", "**/*ViewModel.cs", "**/*Window.axaml.cs"]
topic: ux
---

# UX Checklist

## IsBusy / Progress

```yaml
rule: ∀ длительная I/O ⇒ ViewModel.IsBusy = true/false
  - IsBusy = true перед началом операции
  - IsBusy = false в finally-блоке (await не проглатывает reset)
anti: IsBusy не сбрасывается при ошибке → UI навсегда заблокирован
fix: try { IsBusy = true; await ... } finally { IsBusy = false; }
```

## Confirmation Dialogs

```yaml
rule: ∀ деструктивная операция ⇒ FAContentDialog с подтверждением
required:
  - удаление документа/строки/коллекции
  - закрытие окна с несохранёнными изменениями
  - перезапись существующего файла
anti: удаление без подтверждения → потеря данных
```

## Closing Protection (Dirty Flag)

```yaml
rule: ∀ окно с редактированием ⇒ проверка _isDirty перед закрытием
  - _isDirty = true при любом изменении данных
  - при Closing event: если _isDirty → показать диалог «Сохранить изменения?»
pattern: MainWindowViewModel.DocumentModified → блокирует закрытие без сохранения
```

## Selection Brush (Text Visibility)

```yaml
rule: ∀ TextBox/TextBlock ⇒ SelectionBrush контрастирует с фоном
bug: невидимый выделитель текста на тёмном/светлом фоне
fix: установить явный SelectionBrush в ресурсах/теме (AccentColor)
```

## Auto-Reset Banner (ЛРИ)

```yaml
rule: ∀ информационный баннер ⇒ автосброс через N секунд или по действию пользователя
bug: баннер ЛРИ висит бесконечно после операции
fix: таймер автосброса + сброс при новом действии
```

## Hot-Reload Flashing

```yaml
rule: ¬ мерцание UI при hot-reload справочников
fix: batch update + debounce (250ms), ContentChanged ≠ StructureChanged
```

## Dialog Style

```yaml
rule: ∀ диалог ⇒ FAContentDialog (FluentAvalonia, in-app overlay)
anti: отдельные OS-окна (MessageBox, Window)
```
