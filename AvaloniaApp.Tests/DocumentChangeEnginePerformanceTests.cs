using System.Diagnostics;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Производственные регрессионные тесты, имитирующие типовые сценарии UIA.
/// Проверяют, что массовые операции с коллекциями не приводят к зависанию UI.
/// </summary>
public class DocumentChangeEnginePerformanceTests
{
    [Fact]
    public void AddRemove_20Sections_UnderThreshold()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 20; i++)
        {
            engine.AddSection();
        }

        while (body.Sections.Count > 0)
        {
            engine.RemoveSection(body.Sections[^1]);
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 500, $"20 add/remove sections took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void AddRemove_20Terms_UnderThreshold()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 20; i++)
        {
            engine.AddTerm();
        }

        while (body.Terms.Count > 0)
        {
            engine.RemoveTerm(body.Terms[^1]);
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 500, $"20 add/remove terms took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void AddRemove_10AppendicesWithTables_UnderThreshold()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 10; i++)
        {
            engine.AddAppendix();
            var appendix = body.Appendices[^1];
            engine.AddSectionToAppendix(appendix);
            engine.AddSectionToAppendix(appendix);
        }

        while (body.Appendices.Count > 0)
        {
            engine.RemoveAppendix(body.Appendices[^1]);
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 500, $"10 appendices with tables took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void TypeText_100Chars_FiresContentChangedFast()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        int contentChanges = 0;
        engine.ContentChanged += (_, _) => contentChanges++;
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            engine.SetPurpose(engine.Body.Purpose + "a");
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 200, $"100 char events took {sw.ElapsedMilliseconds}ms");
        Assert.Equal(100, contentChanges);
    }

    [Fact]
    public void TreeWith_50Sections_RebuildsFast()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var vm = new SectionsTreeViewModel(body, engine);
        int rebuilds = 0;
        vm.RootNodes.CollectionChanged += (_, _) => rebuilds++;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++)
        {
            engine.AddSection();
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 500, $"50 sections tree took {sw.ElapsedMilliseconds}ms");
        Assert.Equal(50, body.Sections.Count);
    }
}
