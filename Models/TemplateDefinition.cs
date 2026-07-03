using System.Collections.Generic;

namespace AvaloniaApp.Models;

/// <summary>
/// Описание фабричного шаблона DocAsCode.
/// </summary>
public class TemplateDefinition
{
    public string DocTypeCode { get; set; } = string.Empty;

    public int HmsLevel { get; set; }

    public string DefaultApproverRole { get; set; } = string.Empty;

    public bool HasShield { get; set; }

    public string? ShieldDocType { get; set; }

    public string ShieldTemplate { get; set; } = string.Empty;

    public ShieldPlacement ShieldPlacement { get; set; }

    public bool HasMermaidBlock { get; set; }

    public bool HasRaciTable { get; set; }

    public bool HasJsaTable { get; set; }

    public bool HasChecklist { get; set; }

    public bool HasAppendixChecklist { get; set; }

    public string AbstractTemplate { get; set; } = string.Empty;

    public List<SectionDefinition> Sections { get; set; } = new();

    public string TrainingTemplate { get; set; } = string.Empty;

    public string LinksTemplate { get; set; } = string.Empty;

    public string ChangeLogTemplate { get; set; } = string.Empty;

    public string CopyrightTemplate { get; set; } = "®© ООО «БурСервис», 2026 г.";
}
