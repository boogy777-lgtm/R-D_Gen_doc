namespace AvaloniaApp.Models;

/// <summary>
/// Markdown syntax template for a single field value.
/// Generator and parser use the same enum — guaranteed round-trip.
/// </summary>
public enum MdSyntax
{
    /// <summary>Plain text: value as-is (no wrapping).</summary>
    Plain,

    /// <summary>Bold: **value**.</summary>
    Bold,

    /// <summary>Wikilink for Obsidian Graph: [[value]].</summary>
    Wikilink,
}
