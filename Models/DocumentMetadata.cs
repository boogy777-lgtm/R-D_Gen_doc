using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.Models;

/// <summary>
/// Метаданные документа в полной фабричной YAML-схеме (Phase 1).
/// </summary>
public partial class DocumentMetadata : ObservableObject
{
    // --- Основная фабричная схема ---
    [ObservableProperty]
    private string docId = string.Empty;

    [ObservableProperty]
    private string docType = "ST";

    [ObservableProperty]
    private string title = string.Empty;

    /// <summary>
    /// Название подразделения-владельца документа (бывшее поле «Домен» + «Карточка основания»).
    /// YAML-ключ: <c>department</c>.
    /// </summary>
    [ObservableProperty]
    private string departmentName = "НИОКР";

    /// <summary>
    /// Номер редакции документа. Используется в ЛРИ и в шапке DOCX.
    /// По умолчанию "0". Тип — строка (чтобы поддерживать "1", "1.0", "2.5").
    /// </summary>
    [ObservableProperty]
    private string revision = "0";

    [ObservableProperty]
    private string status = "проект";

    [ObservableProperty]
    private string effectiveDate = "ДД.ММ.ГГГГ";

    [ObservableProperty]
    private string author = string.Empty;

    [ObservableProperty]
    private string owner = "НИОКР";

    [ObservableProperty]
    private string approver = string.Empty;

    [ObservableProperty]
    private string approverRole = "Директор по качеству, ОТ, ПБ и ООС";

    [ObservableProperty]
    private string protocol = string.Empty;

    /// <summary>
    /// Текст нижнего колонтитула DOCX. По умолчанию — формулировка из ГОСТ 6.1-6.2.
    /// YAML-ключ: <c>footer_text</c>.
    /// </summary>
    [ObservableProperty]
    private string footerText = "Печатные версии этого документа считаются НЕКОНТРОЛИРУЕМЫМИ";

    // --- Опциональные сегменты шифра документа (checkbox + поле) ---
    // YAML-ключи: division_enabled, division_code, etc.
    [ObservableProperty]
    private bool divisionEnabled;

    [ObservableProperty]
    private string divisionCode = string.Empty;

    [ObservableProperty]
    private bool disciplineEnabled;

    [ObservableProperty]
    private string disciplineCode = string.Empty;

    [ObservableProperty]
    private bool indexEnabled;

    [ObservableProperty]
    private string indexCode = string.Empty;

    [ObservableProperty]
    private bool languageEnabled;

    [ObservableProperty]
    private string languageCode = string.Empty;

    // --- Фасад для существующего UI и DOCX-генератора ---
    // Эти свойства не сериализуются в YAML, но сохраняют работу View/DOCX.
    public string DocumentName
    {
        get => Title;
        set
        {
            if (Title == value)
            {
                return;
            }

            Title = value;
            OnPropertyChanged(nameof(DocumentName));
        }
    }

    public string DocumentCode
    {
        get => DocId;
        set
        {
            if (DocId == value)
            {
                return;
            }

            DocId = value;
            OnPropertyChanged(nameof(DocumentCode));
        }
    }

    public string ApprovedBy
    {
        get => Approver;
        set
        {
            if (Approver == value)
            {
                return;
            }

            Approver = value;
            OnPropertyChanged(nameof(ApprovedBy));
        }
    }

    public string DateApproved
    {
        get => EffectiveDate;
        set
        {
            if (EffectiveDate == value)
            {
                return;
            }

            EffectiveDate = value;
            OnPropertyChanged(nameof(DateApproved));
        }
    }

    /// <summary>
    /// Obsidian-совместимость: ключи YAML, не входящие в основную схему (tags, aliases, cssclasses…).
    /// Сохраняются при Parse → передаются при Compose — не теряются при round-trip.
    /// </summary>
    public Dictionary<string, object?> ExtraYaml { get; set; } = new();
}
