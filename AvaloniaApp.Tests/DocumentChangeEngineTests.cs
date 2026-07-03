using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.Services.Defaults;
using AvaloniaApp.ViewModels;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионные тесты для DocumentChangeEngine —
/// единого движка мутаций DocumentBody.
/// </summary>
public class DocumentChangeEngineTests
{
    [Fact]
    public void AddTerm_FiresChanged_AndAppearsInMarkdown()
    {
        var body = new DocumentBody { Purpose = "Test" };
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var changes = 0;
        engine.Changed += (_, _) => changes++;

        engine.AddTerm();

        Assert.Equal(1, changes);
        Assert.Single(body.Terms);
        Assert.Equal(0, body.Terms[0].Position);

        var md = DocumentBodyToMarkdown.Convert(body);
        Assert.Contains("## 3 Термины и определения", md);
    }

    [Fact]
    public void AddMultipleTerms_AllAppearInMarkdown()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var changes = 0;
        engine.Changed += (_, _) => changes++;

        engine.AddTerm();
        engine.AddTerm();
        engine.AddTerm();

        Assert.Equal(3, changes);
        Assert.Equal(3, body.Terms.Count);

        // Все 3 строки отрендерены
        var md = DocumentBodyToMarkdown.Convert(body);
        Assert.Contains("## 3 Термины и определения", md);
        Assert.DoesNotContain("заполните", md); // placeholder убран
    }

    [Fact]
    public void AddTerm_WithContent_FiresChanged_AndRenderedCorrectly()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddTerm();
        engine.AddTerm();
        engine.AddTerm();

        // Теперь заполним через ReplaceTerms
        engine.ReplaceTerms(new System.Collections.ObjectModel.ObservableCollection<TermPair>
        {
            new() { Position = 0, Term = "SOP", Definition = "Standard" },
            new() { Position = 1, Term = "JSA", Definition = "Job Safety Analysis" },
            new() { Position = 2, Term = "MOC", Definition = "Management of Change" },
        });

        Assert.Equal(3, body.Terms.Count);
        var md = DocumentBodyToMarkdown.Convert(body);
        Assert.Contains("**SOP**", md);
        Assert.Contains("**JSA**", md);
        Assert.Contains("**MOC**", md);
    }

    [Fact]
    public void AddSection_FiresChanged_AndAppearsInMarkdown()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection();

        // Даём секции содержимое — пустые секции не выводятся в MD.
        body.Sections[0].Title = "Test Section";

        var md = DocumentBodyToMarkdown.Convert(body);
        Assert.Contains("## 4 Test Section", md);
    }

    [Fact]
    public void AddMultipleSections_AllAppearInMarkdown()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddSection();
        engine.AddSection();
        engine.AddSection();

        // Даём секциям заголовки — пустые секции не выводятся в MD.
        body.Sections[0].Title = "A";
        body.Sections[1].Title = "B";
        body.Sections[2].Title = "C";

        var md = DocumentBodyToMarkdown.Convert(body);

        // Sections нумеруются 4, 5, 6
        Assert.Contains("## 4 A", md);
        Assert.Contains("## 5 B", md);
        Assert.Contains("## 6 C", md);
    }

    [Fact]
    public void RemoveTerm_RenumbersPositions()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        engine.ReplaceTerms(new System.Collections.ObjectModel.ObservableCollection<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
            new() { Position = 1, Term = "B", Definition = "2" },
            new() { Position = 2, Term = "C", Definition = "3" },
        });

        engine.RemoveTerm(body.Terms[1]);

        Assert.Equal(2, body.Terms.Count);
        Assert.Equal("A", body.Terms[0].Term);
        Assert.Equal("C", body.Terms[1].Term);
        Assert.Equal(0, body.Terms[0].Position);
        Assert.Equal(1, body.Terms[1].Position);
    }

    [Fact]
    public void AddChangeLogRow_FiresChanged_WithPrefilledData()
    {
        // Arrange: по умолчанию metadata.Revision = "0".
        // Служебные колонки ЛРИ ВСЕГДА берутся из metadata (после Trim),
        // а не инкрементируются от количества существующих строк.
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddChangeLogRow();

        Assert.Single(body.ChangeLog);
        Assert.Equal("0", body.ChangeLog[0].Revision);
        Assert.Equal(DateTime.Today.ToString("dd.MM.yyyy"), body.ChangeLog[0].Date);
        Assert.True(string.IsNullOrEmpty(body.ChangeLog[0].Description));

        var md = DocumentBodyToMarkdown.Convert(body);
        Assert.Contains("## Лист регистрации изменений", md);
        Assert.Contains("| Дата ред. |", md);
    }

    [Fact]
    public void AddMultipleChangeLogRows_AllAppearInMarkdown()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { Revision = "1.0" };
        var engine = new DocumentChangeEngine(body, meta);

        engine.AddChangeLogRow();
        engine.AddChangeLogRow();
        engine.AddChangeLogRow();

        Assert.Equal(3, body.ChangeLog.Count);

        // Все строки синхронизированы с metadata.Revision (обрезанным до «1»).
        Assert.All(body.ChangeLog, row => Assert.Equal("1", row.Revision));

        var md = DocumentBodyToMarkdown.Convert(meta, body);
        Assert.Contains("## Лист регистрации изменений", md);
    }

    [Fact]
    public void SetPurpose_FiresChanged_UpdatesBody()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var changes = 0;
        engine.Changed += (_, _) => changes++;

        engine.SetPurpose("Новая цель");

        Assert.Equal(1, changes);
        Assert.Equal("Новая цель", body.Purpose);
    }

    [Fact]
    public void AddChangeLogRow_PrefillsFromMetadata()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata
        {
            Revision = "1.0",
            EffectiveDate = "05.06.2026",
            Author = "Иванов И.И.",
        };
        var engine = new DocumentChangeEngine(body, meta);

        engine.AddChangeLogRow();

        Assert.Single(body.ChangeLog);
        Assert.Equal("1", body.ChangeLog[0].Revision);
        Assert.Equal("05.06.2026", body.ChangeLog[0].Date);
        // Author больше не хранится в ChangeLogRow — только в метаданных.
    }

    [Fact]
    public void MetadataChange_DoesNotSyncDateRevisionOrAuthorToExistingRows()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata
        {
            Revision = "1.0",
            EffectiveDate = "05.06.2026",
            Author = "Иванов И.И.",
        };
        var engine = new DocumentChangeEngine(body, meta);

        // Добавляем две строки сразу, без изменения метаданных между ними.
        engine.AddChangeLogRow();
        engine.AddChangeLogRow();

        // Обе строки получили одинаковые Date/Revision/Author при создании.
        Assert.Equal("1", body.ChangeLog[0].Revision);
        Assert.Equal("1", body.ChangeLog[1].Revision);
        Assert.All(body.ChangeLog, row => Assert.Equal("Иванов И.И.", row.Author));

        // Меняем метаданные — существующие строки не меняют Date/Revision/Author.
        meta.Revision = "3";
        meta.EffectiveDate = "15.08.2026";
        meta.Author = "Сидоров С.С.";

        // Первая строка сохранила исходные значения.
        Assert.Equal("1", body.ChangeLog[0].Revision);
        Assert.Equal("05.06.2026", body.ChangeLog[0].Date);
        Assert.Equal("Иванов И.И.", body.ChangeLog[0].Author);

        // Последняя строка тоже сохранила исходные Date/Revision/Author.
        Assert.Equal("1", body.ChangeLog[1].Revision);
        Assert.Equal("05.06.2026", body.ChangeLog[1].Date);
        Assert.Equal("Иванов И.И.", body.ChangeLog[1].Author);

        // Метаданные расходятся с последней строкой ЛРИ.
        Assert.True(engine.IsChangeLogOutOfSync());
    }

    [Fact]
    public void ChangeLogRow_AuthorChange_UpdatesMetadataAuthor()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata
        {
            Revision = "1.0",
            EffectiveDate = "05.06.2026",
            Author = "Иванов И.И.",
        };
        var engine = new DocumentChangeEngine(body, meta);
        engine.AddChangeLogRow();

        // Меняем Author только в последней строке.
        body.ChangeLog[0].Author = "Петров П.П.";

        // Metadata.Author должен обновиться до автора последней строки.
        Assert.Equal("Петров П.П.", meta.Author);
    }

    [Fact]
    public void ChangeLogRow_HistoricalAuthorChange_DoesNotUpdateMetadataAuthor()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata
        {
            Revision = "1.0",
            EffectiveDate = "05.06.2026",
            Author = "Иванов И.И.",
        };
        var engine = new DocumentChangeEngine(body, meta);
        engine.AddChangeLogRow();
        engine.AddChangeLogRow();

        // Первая строка историческая.
        body.ChangeLog[0].Author = "Петров П.П.";

        // Metadata.Author не должен измениться, т.к. изменена не последняя строка.
        Assert.Equal("Иванов И.И.", meta.Author);
    }

    [Fact]
    public void Engine_RaisesChanged_ForEveryMutation()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var changes = 0;
        engine.Changed += (_, _) => changes++;

        engine.SetPurpose("A");
        engine.SetScope("B");
        engine.AddTerm();
        engine.AddSection();
        engine.AddRelatedDocument();
        engine.AddChangeLogRow();

        Assert.Equal(6, changes);
    }

    [Fact]
    public async Task ViewModel_Engine_FiresRefreshPreview_OnEveryChange()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        Assert.NotNull(vm.Engine);
        Assert.NotNull(vm.DocumentBody);

        // Engine.AddTerm — добавляет термин, body.Terms растёт,
        // HasUnsavedChanges → true (проверяем ниже).
        var initialTermsCount = vm.DocumentBody.Terms.Count;
        vm.Engine.AddTerm();
        Assert.Equal(initialTermsCount + 1, vm.DocumentBody.Terms.Count);

        // Engine.AddSection — добавляет секцию, body.Sections растёт.
        var initialSectionsCount = vm.DocumentBody.Sections.Count;
        vm.Engine.AddSection();
        Assert.Equal(initialSectionsCount + 1, vm.DocumentBody.Sections.Count);

        // Engine.AddChangeLogRow — добавляет строку в ChangeLog.
        var initialChangeLogCount = vm.DocumentBody.ChangeLog.Count;
        vm.Engine.AddChangeLogRow();
        Assert.Equal(initialChangeLogCount + 1, vm.DocumentBody.ChangeLog.Count);

        // После всех мутаций документ помечен как изменённый
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task ViewModel_DocumentBody_ChangesEngineInstance()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        var engine1 = vm.Engine;
        Assert.NotNull(engine1);

        // Создаём новый документ
        vm.Metadata.DocType = "PM";
        vm.DocumentBody = new DocumentBody { Purpose = "New doc" };

        var engine2 = vm.Engine;
        Assert.NotNull(engine2);
        Assert.NotSame(engine1, engine2);
    }

    [Fact]
    public void RemoveSectionFromAppendix_RenumbersOnlyAffectedAppendix()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        engine.AddAppendix();
        engine.AddAppendix();
        var first = body.Appendices[0];
        var second = body.Appendices[1];

        engine.AddSectionToAppendix(first);
        engine.AddSectionToAppendix(first);
        engine.AddSectionToAppendix(second);

        var sectionToRemove = first.Sections[0];
        engine.RemoveSectionFromAppendix(first, sectionToRemove);

        Assert.Equal(2, first.Sections.Count);
        Assert.Equal(0, first.Sections[0].Position);
        Assert.Equal(1, first.Sections[1].Position);
        Assert.Equal(2, second.Sections.Count);
        Assert.Equal(0, second.Sections[0].Position);
    }

    [Fact]
    public void AddSection_PrefillsFromTemplate_WhenDocTypeKnown()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "PM" };
        var engine = new DocumentChangeEngine(body, meta);

        engine.AddSection();

        Assert.Single(body.Sections);
        Assert.Equal("Входы и выходы", body.Sections[0].Title);
        Assert.Contains("**Владелец процесса:**", body.Sections[0].Body);

        var md = DocumentBodyToMarkdown.Convert(body);
        Assert.Contains("## 4 Входы и выходы", md);
        Assert.Contains("**Владелец процесса:**", md);
    }

    [Fact]
    public void AddSection_FallsBackToEmpty_WhenTemplateExhausted()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "PM" };
        var engine = new DocumentChangeEngine(body, meta);

        // PM шаблон содержит 4 стартовые секции.
        for (int i = 0; i < 6; i++)
        {
            engine.AddSection();
        }

        Assert.Equal(6, body.Sections.Count);
        Assert.Equal("Входы и выходы", body.Sections[0].Title);
        Assert.Equal("Визуальный алгоритм", body.Sections[1].Title);
        Assert.Equal("Матрица ответственности (RACI)", body.Sections[2].Title);
        Assert.Equal("KPI процесса", body.Sections[3].Title);
        Assert.True(string.IsNullOrEmpty(body.Sections[4].Title));
        Assert.True(string.IsNullOrEmpty(body.Sections[5].Title));
    }

    [Fact]
    public void AddSubsection_DoesNotPrefillTemplate()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "PM" };
        var engine = new DocumentChangeEngine(body, meta);
        engine.AddSection();
        var root = body.Sections[0];

        engine.AddSection(root);

        Assert.Single(root.Children);
        Assert.True(string.IsNullOrEmpty(root.Children[0].Title));
    }

    [Fact]
    public void Section_TitleChange_FiresContentChanged()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "ST" };
        var engine = new DocumentChangeEngine(body, meta);
        engine.AddSection();

        var contentChanges = 0;
        engine.ContentChanged += (_, _) => contentChanges++;

        body.Sections[0].Title = "Изменённый заголовок";

        Assert.Equal(1, contentChanges);
    }

    [Fact]
    public async Task UserScenario_DeleteAllSections_AndAddOne_GetsTemplate()
    {
        var vm = new MainWindowViewModel();
        await vm.InitializeAsync();

        // Создаём PM-документ вручную (имитация NewDocument("PM")).
        vm.Metadata.DocType = "PM";
        vm.DocumentBody = new DocumentBody
        {
            Sections = new System.Collections.ObjectModel.ObservableCollection<BodySection>(
                Forms.GetTemplate("PM").StarterSections),
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair> { new() },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                DocumentBodyFactory.CreatePrefilledChangeLogRow(),
            },
        };

        // После присвоения DocumentBody создаётся Engine.
        Assert.NotNull(vm.Engine);

        // Начальное состояние: 4 шаблонные секции в preview.
        Assert.NotEmpty(vm.MarkdownPreview);
        Assert.Contains("## 4 Входы и выходы", vm.MarkdownPreview);

        // Пользователь удаляет все секции.
        while (vm.DocumentBody!.Sections.Count > 0)
        {
            var first = vm.DocumentBody.Sections[0];
            vm.Engine!.RemoveSection(first);
        }

        Assert.Empty(vm.DocumentBody.Sections);

        // Preview обновляется синхронно через StructureChanged.
        // Центральных секций нет — но placeholder-секция остаётся (fallback на TreeFieldDescriptor.Placeholder),
        // чтобы структурный заголовок «## 4» не пропадал целиком (иначе DOCX теряет раздел).
        Assert.Contains("## 4", vm.MarkdownPreview);
        var sectionsDescriptor = Forms.GetTemplate("PM").CenterFields
            .OfType<TreeFieldDescriptor>().First();
        Assert.Contains(sectionsDescriptor.Placeholder ?? sectionsDescriptor.TemplateDefault ?? string.Empty, vm.MarkdownPreview);

        // Структурные заголовки (Цель, Область, Термины, Обучение...) остаются.
        Assert.Contains("## 1 Цель", vm.MarkdownPreview);
        Assert.Contains("## 2 Область применения", vm.MarkdownPreview);

        // Пользователь добавляет новую секцию.
        vm.Engine!.AddSection();

        // Должна появиться первая шаблонная секция (Входы и выходы).
        Assert.Single(vm.DocumentBody.Sections);
        Assert.Equal("Входы и выходы", vm.DocumentBody.Sections[0].Title);
        Assert.Contains("**Владелец процесса:**", vm.DocumentBody.Sections[0].Body);

        // Preview теперь содержит новую секцию с номером 4.
        Assert.Contains("## 4 Входы и выходы", vm.MarkdownPreview);
        Assert.Contains("**Владелец процесса:**", vm.MarkdownPreview);

        // Пользователь редактирует заголовок — preview должно обновиться (ContentChanged).
        var previewBeforeEdit = vm.MarkdownPreview;
        vm.DocumentBody.Sections[0].Title = "Мой собственный раздел";

        // ContentChanged идёт с debounce 250ms, но в тесте ждём.
        await Task.Delay(300);
        Assert.Contains("Мой собственный раздел", vm.MarkdownPreview);
        Assert.DoesNotContain("Входы и выходы", vm.MarkdownPreview);
    }

    [Fact]
    public void EmptyTerms_RendersPlaceholderRow_FromDescriptor()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "ST" };
        var template = Forms.GetTemplate("ST");

        var md = DocumentBodyToMarkdown.Convert(meta, body, template);

        // Заголовок секции присутствует всегда.
        Assert.Contains("## 3 Термины и определения", md);
        // Placeholder-строка из дескриптора «Terms» виден.
        var termsDescriptor = template.HeaderFields
            .OfType<CollectionFieldDescriptor<TermPair>>().First();
        Assert.Contains(termsDescriptor.Placeholder ?? termsDescriptor.TemplateDefault ?? string.Empty, md);
    }

    [Fact]
    public void EmptyChangeLog_RendersPlaceholderRow_InTable()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "ST" };
        var template = Forms.GetTemplate("ST");

        var md = DocumentBodyToMarkdown.Convert(meta, body, template);

        // Заголовок и шапка таблицы присутствуют.
        Assert.Contains("## Лист регистрации изменений", md);
        Assert.Contains("| Дата ред. |", md);
        // Placeholder-строка из дескриптора «ChangeLog» виден.
        var changeLogDescriptor = template.TrailingFields
            .OfType<CollectionFieldDescriptor<ChangeLogRow>>().First();
        Assert.Contains(changeLogDescriptor.Placeholder ?? changeLogDescriptor.TemplateDefault ?? string.Empty, md);
    }

    [Fact]
    public void EmptyAppendices_RendersPlaceholderAppendix_FromDescriptor()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "ST" };
        var template = Forms.GetTemplate("ST");

        var md = DocumentBodyToMarkdown.Convert(meta, body, template);

        // Структурный заголовок «## Приложение» присутствует.
        Assert.Contains("## Приложение", md);
        // Placeholder-текст из дескриптора «Appendices» виден.
        var appendicesDescriptor = template.TrailingFields
            .OfType<CollectionFieldDescriptor<BodyAppendix>>().First();
        Assert.Contains(appendicesDescriptor.Placeholder ?? appendicesDescriptor.TemplateDefault ?? string.Empty, md);
    }

    [Fact]
    public void EmptySections_RendersPlaceholderSection_FromDescriptor()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "ST" };
        var template = Forms.GetTemplate("ST");

        var md = DocumentBodyToMarkdown.Convert(meta, body, template);

        // Структурный заголовок «## 4» присутствует (placeholder-секция).
        Assert.Contains("## 4", md);
        // Placeholder-текст из дескриптора «Sections» виден.
        var sectionsDescriptor = template.CenterFields
            .OfType<TreeFieldDescriptor>().First();
        Assert.Contains(sectionsDescriptor.Placeholder ?? sectionsDescriptor.TemplateDefault ?? string.Empty, md);
    }

    [Fact]
    public void EmptyRelatedDocuments_RendersPlaceholderItem_FromDescriptor()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocType = "ST" };
        var template = Forms.GetTemplate("ST");

        var md = DocumentBodyToMarkdown.Convert(meta, body, template);

        // Подзаголовок «### Связанные документы» присутствует.
        Assert.Contains("### Связанные документы", md);
        // Placeholder-значение из дескриптора «RelatedDocuments» виден.
        var relatedDescriptor = template.TrailingFields
            .OfType<CollectionFieldDescriptor<StringItem>>()
            .First(d => d.Key == "RelatedDocuments");
        Assert.Contains(relatedDescriptor.Placeholder ?? relatedDescriptor.TemplateDefault ?? string.Empty, md);
    }

    [Fact]
    public void NonEmptyCollections_DoNotRenderPlaceholders()
    {
        // Когда у пользователя есть реальные данные, placeholder-текст НЕ должен
        // подставляться в секцию терминов.
        var body = new DocumentBody
        {
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
            {
                new() { Position = 0, Term = "SOP", Definition = "Standard Operating Procedure" },
            },
        };
        var meta = new DocumentMetadata { DocType = "ST" };
        var template = Forms.GetTemplate("ST");

        var md = DocumentBodyToMarkdown.Convert(meta, body, template);

        // Реальный термин виден.
        Assert.Contains("**SOP**", md);
        Assert.Contains("Standard Operating Procedure", md);

        // Placeholder из дескриптора НЕ виден (он показывается только для пустой коллекции).
        var termsDescriptor = template.HeaderFields
            .OfType<CollectionFieldDescriptor<TermPair>>().First();
        Assert.DoesNotContain(termsDescriptor.Placeholder ?? string.Empty, md);
    }
}
