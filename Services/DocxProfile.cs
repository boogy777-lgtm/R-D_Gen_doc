using System;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Профиль генерации DOCX, выбираемый по типу документа.
/// </summary>
public class DocxProfile
{
    public string FontName { get; init; } = "Roboto";

    public double FontSizePt { get; init; } = 9;

    public double TitleFontSizePt { get; init; } = 22;

    public string ApprovalRoleRu { get; init; } = string.Empty;

    public string HeaderSubtitleRu { get; init; } = string.Empty;

    public bool RenderMermaidAsMonospace { get; init; }

    public bool RenderRaciAsMonospace { get; init; }

    public bool RenderJsaAsMonospace { get; init; }

    public bool RenderShieldAsCallout { get; init; }

    public bool IncludeBasisCardReference { get; init; }

    /// <summary>
    /// Создаёт профиль по метаданным и шаблону. Для карточки документа — минимальный профиль.
    /// </summary>
    public static DocxProfile Create(DocumentMetadata metadata, TemplateDefinition? def)
    {
        if (def is null || string.Equals(metadata.DocType, "Basis", StringComparison.OrdinalIgnoreCase))
        {
            return new DocxProfile
            {
                // Даже без шаблона: если в метаданных есть утверждающий — показываем таблицу утверждения.
                ApprovalRoleRu = !string.IsNullOrWhiteSpace(metadata.ApproverRole)
                    ? metadata.ApproverRole
                    : string.Empty,
                HeaderSubtitleRu = string.Empty,
                RenderMermaidAsMonospace = false,
                RenderRaciAsMonospace = false,
                RenderJsaAsMonospace = false,
                RenderShieldAsCallout = false,
                IncludeBasisCardReference = false,
            };
        }

        // Уровень ИСМ берём из прямого поля HmsLevel (раньше парсили из level-X тега,
        // но это запутывало — Aliases/Tags больше не показываются пользователю).
        var subtitle = def.HmsLevel > 0
            ? $"Документ уровня {def.HmsLevel}"
            : string.Empty;

        return new DocxProfile
        {
            ApprovalRoleRu = !string.IsNullOrWhiteSpace(metadata.ApproverRole)
                ? metadata.ApproverRole
                : def.DefaultApproverRole,
            HeaderSubtitleRu = subtitle,
            RenderMermaidAsMonospace = def.HasMermaidBlock,
            RenderRaciAsMonospace = def.HasRaciTable,
            RenderJsaAsMonospace = def.HasJsaTable,
            RenderShieldAsCallout = def.HasShield,
            IncludeBasisCardReference = false,
        };
    }
}
