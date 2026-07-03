using System;
using System.Text;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Сервис предложения номера документа без проверки уникальности.
/// </summary>
public static class DocumentNumberingService
{
    /// <summary>
    /// Предлагает идентификатор документа по правилам WM-RUS-BUR-QHSE-401A.
    /// (Упрощённая версия без опциональных сегментов; для полного шифра используйте BuildDocId.)
    /// </summary>
    public static string ProposeDocId(string docTypeCode, string domainCode, int nextSequence = 1)
    {
        if (string.IsNullOrWhiteSpace(docTypeCode))
        {
            return string.Empty;
        }

        if (string.Equals(docTypeCode, "Basis", StringComparison.OrdinalIgnoreCase))
        {
            return $"BC-{DateTime.Now:yyyy}-{nextSequence:D3}";
        }

        var type = docTypeCode.ToUpperInvariant();
        var domain = string.IsNullOrWhiteSpace(domainCode)
            ? "QHSE"
            : domainCode.ToUpperInvariant();

        return $"{type}-RUS-BUR-{domain}-{nextSequence:D3}";
    }

    /// <summary>
    /// Собирает полный шифр документа из метаданных, включая опциональные сегменты:
    /// {Type}-{Geography}-BUR-{Department}[-{Division}][-{Discipline}]-{Seq}{Index}[-{Language}]
    ///
    /// Параметр <paramref name="departmentCode"/> — это код КОРНЕВОГО подразделения
    /// (parent_id, например «SD»). Опциональный сегмент отделения передаётся через
    /// <see cref="DocumentMetadata.DivisionCode"/>, причём в нём хранится ТОЛЬКО
    /// child_id (например «NNB», а НЕ «SD-NNB»). Итоговый шифр формируется как
    /// {parent}-{child_id}, чтобы избежать дублирования parent_id (баг #4).
    /// </summary>
    public static string BuildDocId(DocumentMetadata metadata, string departmentCode = "QHSE", string geographyCode = "RUS", string sequenceNumber = "001")
    {
        if (metadata is null || string.IsNullOrWhiteSpace(metadata.DocType))
        {
            return string.Empty;
        }

        // Basis — особый формат
        if (string.Equals(metadata.DocType, "Basis", StringComparison.OrdinalIgnoreCase))
        {
            return $"BC-{DateTime.Now:yyyy}-{sequenceNumber:D3}";
        }

        var sb = new StringBuilder();
        sb.Append(metadata.DocType.ToUpperInvariant());
        sb.Append('-');
        sb.Append(string.IsNullOrWhiteSpace(geographyCode) ? "RUS" : geographyCode.ToUpperInvariant());
        sb.Append("-BUR-");
        sb.Append(string.IsNullOrWhiteSpace(departmentCode) ? "QHSE" : departmentCode.ToUpperInvariant());

        if (metadata.DivisionEnabled && !string.IsNullOrWhiteSpace(metadata.DivisionCode))
        {
            // DivisionCode хранит только child_id (см. MainWindowViewModel.RefreshChildDepartments).
            // Дописываем как {parent}-{child_id}, чтобы шифр соответствовал WM-RUS-BUR-QHSE-401A.
            sb.Append('-');
            sb.Append(metadata.DivisionCode.Trim().ToUpperInvariant());
        }

        if (metadata.DisciplineEnabled && !string.IsNullOrWhiteSpace(metadata.DisciplineCode))
        {
            sb.Append('-');
            sb.Append(metadata.DisciplineCode.ToUpperInvariant());
        }

        sb.Append('-');
        sb.Append(string.IsNullOrWhiteSpace(sequenceNumber) ? "001" : sequenceNumber.Trim());

        if (metadata.IndexEnabled && !string.IsNullOrWhiteSpace(metadata.IndexCode))
        {
            sb.Append(metadata.IndexCode.Trim().ToUpperInvariant());
        }

        if (metadata.LanguageEnabled && !string.IsNullOrWhiteSpace(metadata.LanguageCode))
        {
            sb.Append('-');
            sb.Append(metadata.LanguageCode.Trim());
        }

        return sb.ToString();
    }
}
