using System;

namespace AvaloniaApp.Models;

/// <summary>
/// Карточка основания (basis card) для документа.
/// </summary>
public class BasisCard
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string DocTypeCode { get; set; } = string.Empty;

    public string DomainCode { get; set; } = string.Empty;

    public string? ProposedDocId { get; set; }

    public string? LinkedDocId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string Status { get; set; } = "черновик";

    public bool Sealed { get; set; }
}
