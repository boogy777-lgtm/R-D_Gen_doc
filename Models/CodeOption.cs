namespace AvaloniaApp.Models;

/// <summary>
/// Опция для dropdown (код + метка).
/// </summary>
public class CodeOption
{
    public CodeOption(string code, string label)
    {
        Code = code;
        Label = label;
    }

    public string Code { get; }

    public string Label { get; }

    public override string ToString() => $"{Code} — {Label}";
}
