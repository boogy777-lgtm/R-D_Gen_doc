namespace AvaloniaApp.Services.DocxGeneration;

/// <summary>
/// Геометрия и размеры DOCX-документа: ширины колонок верхнего колонтитула,
/// таблицы утверждения, отступы страницы. Эталон: convert_from_scratch.py.
/// Вынесены из <c>DocxGenerator</c> для устранения God-class (SRP).
/// </summary>
internal static class DocxPageLayout
{
    /// <summary>Шрифт для monospace-таблиц (RACI / JSA) и code blocks.</summary>
    public const string MonospaceFont = "Consolas";

    // Ширины колонок верхнего колонтитула (cm).
    public const double HeaderCol1Cm = 6.49;
    public const double HeaderCol2Cm = 4.09;
    public const double HeaderCol3Cm = 2.54;
    public const double HeaderCol4Cm = 2.43;
    public const double HeaderCol5Cm = 2.56;

    /// <summary>Правая колонка таблицы утверждения = сумма последних трёх колонок.</summary>
    public const double ApprovalRightColCm = HeaderCol3Cm + HeaderCol4Cm + HeaderCol5Cm; // 7.53

    // A4 (twips, 1 cm = 567 dxa ≈ 1701/3 twips).
    public const int PageWidthTwips = 11906;
    public const int PageHeightTwips = 16838;

    // Поля страницы (twips, 2 cm = 1134, 3.5 cm = 1984).
    public const int TopMarginTwips = 1984;     // 3.5 cm
    public const int BottomMarginTwips = 1134;  // 2.0 cm
    public const int LeftMarginTwips = 1134;    // 2.0 cm
    public const int RightMarginTwips = 1134;   // 2.0 cm

    /// <summary>1 cm = 567 dxa (Document Format Architecture).</summary>
    public const int DxaPerCm = 567;

    /// <summary>1 cm = 1701/3 twips; целочисленное приближение.</summary>
    public const double TwipsPerCm = 1701.0 / 3.0;
}
