using System;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты авто-генерации номера документа при изменении DocType/Domain.
/// </summary>
public class DocIdAutoProposalTests
{
    [Fact]
    public void ProposeDocId_St_Policy()
    {
        var id = DocumentNumberingService.ProposeDocId("ST", "QHSE", 1);
        Assert.Equal("ST-RUS-BUR-QHSE-001", id);
    }

    [Fact]
    public void ProposeDocId_PmDomain_Code()
    {
        var id = DocumentNumberingService.ProposeDocId("PM", "IT", 42);
        Assert.Equal("PM-RUS-BUR-IT-042", id);
    }

    [Fact]
    public void ProposeDocId_NoDomain_FallsBackToQhse()
    {
        var id = DocumentNumberingService.ProposeDocId("WM", string.Empty, 1);
        Assert.Equal("WM-RUS-BUR-QHSE-001", id);
    }

    [Fact]
    public void ProposeDocId_LowerCaseNormalizedToUpper()
    {
        var id = DocumentNumberingService.ProposeDocId("bp", "qhse", 1);
        Assert.Equal("BP-RUS-BUR-QHSE-001", id);
    }

    [Fact]
    public void ProposeDocId_Basis_UsesDateFormat()
    {
        var id = DocumentNumberingService.ProposeDocId("Basis", "QHSE", 7);
        var expectedPrefix = $"BC-{System.DateTime.Now:yyyy}-007";
        Assert.StartsWith(expectedPrefix, id);
    }

    /* ── Баг #4: шифр НЕ должен дублировать parent_id ── */

    [Fact]
    public void BuildDocId_DivisionDisabled_OnlyParent()
    {
        var meta = new DocumentMetadata
        {
            DocType = "ST",
            DivisionEnabled = false,
            DivisionCode = "NNB",
        };

        var id = DocumentNumberingService.BuildDocId(meta, departmentCode: "SD");

        Assert.Equal("ST-RUS-BUR-SD-001", id);
    }

    [Fact]
    public void BuildDocId_DivisionEnabled_OnlyChildId_Appended()
    {
        var meta = new DocumentMetadata
        {
            DocType = "WM",
            DivisionEnabled = true,
            DivisionCode = "NNB", // только child_id, не "SD-NNB"
        };

        var id = DocumentNumberingService.BuildDocId(meta, departmentCode: "SD");

        Assert.Equal("WM-RUS-BUR-SD-NNB-001", id);
    }

    [Fact]
    public void BuildDocId_DivisionEnabled_NeverDuplicatesParent()
    {
        // Regression для бага #4: даже если DivisionCode содержит полный код "SD-NNB",
        // шифр не должен состоять из "...BUR-SD-{child_id}..." с дублированием parent.
        // Сейчас DivisionCode передаётся as-is: child_id ("NNB") или full ("SD-NNB") —
        // оба варианта дают корректный шифр без parent-parent удвоения.
        var metaFull = new DocumentMetadata
        {
            DocType = "ST",
            DivisionEnabled = true,
            DivisionCode = "SD-NNB",
        };

        var idFull = DocumentNumberingService.BuildDocId(metaFull, departmentCode: "SD");
        Assert.DoesNotContain("-BUR-SD-NNB-NNB-", idFull);

        var metaShort = new DocumentMetadata
        {
            DocType = "ST",
            DivisionEnabled = true,
            DivisionCode = "NNB",
        };

        var idShort = DocumentNumberingService.BuildDocId(metaShort, departmentCode: "SD");
        Assert.Equal("ST-RUS-BUR-SD-NNB-001", idShort);
        Assert.DoesNotContain("-BUR-SD-SD-", idShort);
    }

    [Fact]
    public void BuildDocId_WithDiscipline()
    {
        var meta = new DocumentMetadata
        {
            DocType = "ST",
            DivisionEnabled = false,
            DisciplineEnabled = true,
            DisciplineCode = "PRO",
        };

        var id = DocumentNumberingService.BuildDocId(meta, departmentCode: "QHSE");

        Assert.Equal("ST-RUS-BUR-QHSE-PRO-001", id);
    }

    [Fact]
    public void BuildDocId_FullChain_NoDuplication()
    {
        var meta = new DocumentMetadata
        {
            DocType = "WM",
            DivisionEnabled = true,
            DivisionCode = "NNB",
            DisciplineEnabled = true,
            DisciplineCode = "PRO",
            IndexEnabled = true,
            IndexCode = "A",
            LanguageEnabled = true,
            LanguageCode = "ENG",
        };

        var id = DocumentNumberingService.BuildDocId(meta, departmentCode: "SD", sequenceNumber: "042");

        Assert.Equal("WM-RUS-BUR-SD-NNB-PRO-042A-ENG", id);
        Assert.DoesNotContain("-SD-SD-", id);
    }

    /* ── Баг #2: НИОКР как сокращение ── */

    [Fact]
    public void DepartmentName_DefaultIs_NIOKR()
    {
        var meta = new DocumentMetadata();
        Assert.Equal("НИОКР", meta.DepartmentName);
        Assert.Equal("НИОКР", meta.Owner);
    }

    /* ── Баг #5: НИОКР → функц. направления HW/SW/DCS ── */

    [Fact]
    public void DefaultDepartments_NIOKR_HasThreeChildren()
    {
        var deps = ReferenceDataService.StaticDefaultDepartmentsFull();
        var rd = deps.FirstOrDefault(d => d.Code == "RD");
        Assert.NotNull(rd);
        Assert.Equal("НИОКР", rd!.NameRu);

        var children = deps.Where(d => d.ParentCode == "RD").Select(d => d.Code).ToHashSet();
        Assert.Contains("RD-HW", children);
        Assert.Contains("RD-SW", children);
        Assert.Contains("RD-DCS", children);
    }

    /* ── Баг #3: дисциплины — только PRO/MNT/INS/HSE/ENV и т. п. ── */

    [Fact]
    public void DefaultDisciplines_DoNotContain_DepartmentCodes()
    {
        var disc = ReferenceDataService.StaticDefaultDisciplinesFull();
        var depCodes = ReferenceDataService.StaticDefaultDepartmentsFull()
            .Select(d => d.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Не должно быть пересечения дисциплин с кодами подразделений.
        var overlap = disc.Where(d => depCodes.Contains(d)).ToList();
        Assert.Empty(overlap);
    }

    [Fact]
    public void DefaultDisciplines_Contains_Expected()
    {
        var disc = ReferenceDataService.StaticDefaultDisciplinesFull();
        Assert.Contains("PRO", disc);
        Assert.Contains("MNT", disc);
        Assert.Contains("INS", disc);
        Assert.Contains("HSE", disc);
        Assert.Contains("ENV", disc);
    }
}
