using System;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Format / Unformat a single value using MdSyntax template.
/// Round-trip: Unformat(Format(x, s), s) == x.
/// </summary>
public static class MdFormatHelper
{
    /// <summary>
    /// Formats value for markdown output. EscapeMarkdown is applied first.
    /// </summary>
    public static string Format(string value, MdSyntax syntax)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        // Idempotent: if already wrapped, don't double-wrap.
        var inner = syntax switch
        {
            MdSyntax.Bold => Unformat(trimmed, MdSyntax.Bold),
            MdSyntax.Wikilink => Unformat(trimmed, MdSyntax.Wikilink),
            _ => trimmed,
        };

        var escaped = EscapeMarkdown(inner);
        return syntax switch
        {
            MdSyntax.Bold => $"**{escaped}**",
            MdSyntax.Wikilink => $"[[{escaped}]]",
            _ => escaped,
        };
    }

    /// <summary>
    /// Strips markdown syntax — reverse of Format.
    /// "**SOP**" → "SOP", "[[BSD-401]]" → "BSD-401".
    /// </summary>
    public static string Unformat(string value, MdSyntax syntax)
    {
        var trimmed = value.Trim();
        return syntax switch
        {
            MdSyntax.Bold => StripDelimiters(trimmed, "**", "**"),
            MdSyntax.Wikilink => StripDelimiters(trimmed, "[[", "]]"),
            _ => trimmed,
        };
    }

    private static string StripDelimiters(string value, string open, string close)
    {
        if (value.StartsWith(open, StringComparison.Ordinal) &&
            value.EndsWith(close, StringComparison.Ordinal))
        {
            var inner = value.Substring(open.Length, value.Length - open.Length - close.Length).Trim();
            return string.IsNullOrWhiteSpace(inner) ? value : inner;
        }

        return value;
    }

    /// <summary>
    /// Escapes markdown special chars: \, *, _, `, |, \n, \r.
    /// Does NOT escape [ and ] — wikilinks must stay intact.
    /// </summary>
    private static string EscapeMarkdown(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        return s
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("`", "\\`")
            .Replace("|", "\\|")
            .Replace("\r", string.Empty)
            .Replace("\n", " ");
    }
}
