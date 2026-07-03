using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты для вложенных секций (Children) и приложений (Appendices).
/// </summary>
public class HierarchyAndAppendicesTests
{
    [Fact]
    public void AddAppendix_AddsToBodyAppendices()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddAppendix();
        engine.AddAppendix();
        engine.AddAppendix();

        Assert.Equal(3, body.Appendices.Count);
        Assert.Equal(1, body.Appendices[0].Position);
        Assert.Equal(2, body.Appendices[1].Position);
        Assert.Equal(3, body.Appendices[2].Position);
    }

    [Fact]
    public void RemoveAppendix_RenumbersPositions()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddAppendix();
        engine.AddAppendix();
        engine.AddAppendix();
        engine.RemoveAppendix(body.Appendices[1]);

        Assert.Equal(2, body.Appendices.Count);
        Assert.Equal(1, body.Appendices[0].Position);
        Assert.Equal(2, body.Appendices[1].Position);
    }

    [Fact]
    public void AddSection_AddsToTopLevel_WhenNoParent()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection(parent: null);
        engine.AddSection(parent: null);

        Assert.Equal(2, body.Sections.Count);
        Assert.Equal(1, body.Sections[0].Level);
        Assert.Equal(1, body.Sections[1].Level);
    }

    [Fact]
    public void AddSubsection_AddsAsChild_WithIncreasedLevel()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection(parent: null);
        var parent = body.Sections[0];
        engine.AddSection(parent: parent);
        var subsection = parent.Children[0];

        Assert.Single(parent.Children);
        Assert.Equal(2, subsection.Level);
        Assert.Equal(parent.Id, subsection.ParentId);

        // Position — индекс среди соседей (0-based). Первый подраздел получает 0.
        Assert.Equal(0, subsection.Position);

        // HierarchyPath — «4.1» (4 — родительский верхний уровень, 1 — индекс среди сиблингов + 1).
        Assert.Equal("4.1", subsection.HierarchyPath);
    }

    [Fact]
    public void NestedSubsections_BuildHierarchy()
    {
        // 4 → 4.1 → 4.1.1
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection(parent: null);
        var s4 = body.Sections[0];
        engine.AddSection(parent: s4);
        var s41 = s4.Children[0];
        engine.AddSection(parent: s41);

        Assert.Equal(1, s4.Level);
        Assert.Equal(2, s41.Level);
        Assert.Equal(3, s41.Children[0].Level);
    }

    [Fact]
    public void RemoveParentSection_RemovesAllChildren()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection(parent: null);
        engine.AddSection(parent: body.Sections[0]);
        engine.AddSection(parent: body.Sections[0].Children[0]);

        Assert.Equal(3, CountAllSections(body));

        engine.RemoveSection(body.Sections[0]);

        Assert.Equal(0, CountAllSections(body));
    }

    [Fact]
    public void RenumberSections_UpdatesLevelAndParentIdRecursively()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection(parent: null);
        engine.AddSection(parent: null);
        var s1 = body.Sections[0];
        var s2 = body.Sections[1];
        engine.AddSection(parent: s1);
        engine.AddSection(parent: s2);
        engine.AddSection(parent: s1.Children[0]);

        // Проверяем что уровни и ParentId корректны после любых мутаций.
        Assert.Equal(1, s1.Level);
        Assert.Equal(1, s2.Level);
        Assert.Equal(2, s1.Children[0].Level);
        Assert.Equal(2, s2.Children[0].Level);
        Assert.Equal(3, s1.Children[0].Children[0].Level);
        Assert.Equal(s1.Id, s1.Children[0].ParentId);
        Assert.Equal(s2.Id, s2.Children[0].ParentId);
        Assert.Equal(s1.Children[0].Id, s1.Children[0].Children[0].ParentId);
    }

    [Fact]
    public void SectionsTreeViewModel_AddRoot_UpdatesRootNodes()
    {
        // Проверяем, что после Engine.AddSection() SectionsTreeViewModel
        // автоматически обновляет RootNodes (через подписку на Engine.Changed).
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var vm = new SectionsTreeViewModel(body, engine);

        Assert.Empty(vm.RootNodes); // body.Sections пуст изначально

        // Симулируем клик по кнопке «+ Добавить раздел»
        vm.AddRootCommand.Execute(null);

        Assert.Single(vm.RootNodes);
        Assert.Equal("4", vm.RootNodes[0].Section.HierarchyPath);
    }

    [Fact]
    public void SectionsTreeViewModel_AddSubsection_UpdatesRootNodes()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        // Добавляем корневую секцию
        engine.AddSection(null);

        var vm = new SectionsTreeViewModel(body, engine);
        Assert.Single(vm.RootNodes);

        // Добавляем подраздел через команду SectionNodeViewModel
        var root = vm.RootNodes[0];
        root.AddSubsectionCommand.Execute(null);

        // RootNodes должен по-прежнему содержать 1 корневой узел,
        // но у этого узла должен появиться дочерний элемент
        Assert.Single(vm.RootNodes);
        Assert.Single(vm.RootNodes[0].Children);
        Assert.Equal("4.1", vm.RootNodes[0].Children[0].Section.HierarchyPath);
    }

    [Fact]
    public void SectionsTreeViewModel_Remove_UpdatesRootNodes()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection(null);
        engine.AddSection(null);

        var vm = new SectionsTreeViewModel(body, engine);
        Assert.Equal(2, vm.RootNodes.Count);

        // Удаляем первую секцию
        var root = vm.RootNodes[0];
        root.RemoveCommand.Execute(null);

        Assert.Single(vm.RootNodes);
        Assert.Equal("4", vm.RootNodes[0].Section.HierarchyPath);
    }

    private static int CountAllSections(DocumentBody body)
    {
        int count = body.Sections.Count;
        foreach (var s in body.Sections)
        {
            count += CountChildren(s);
        }

        return count;
    }

    private static int CountChildren(BodySection s)
    {
        int count = s.Children.Count;
        foreach (var child in s.Children)
        {
            count += CountChildren(child);
        }

        return count;
    }
}
