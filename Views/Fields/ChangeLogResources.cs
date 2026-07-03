namespace AvaloniaApp.Views.Fields;

/// <summary>
/// UI-ресурсы для Листа регистрации изменений.
/// </summary>
public static class ChangeLogResources
{
    /// <summary>
    /// Серый шаблон-подсказка для столбца "Описание изменений".
    /// UI-only — в БД и DOCX не сохраняется, если Description остался пустым.
    /// </summary>
    public const string DescriptionHint =
        "[Раздел/пункт]: ___\n[Что изменено]: ___\n[Почему изменено]: ___\n[Основание]: ___";
}
