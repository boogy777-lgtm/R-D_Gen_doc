using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.Services.Defaults;
using AvaloniaApp.ViewModels;
using DocumentFormat.OpenXml.Packaging;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Тесты привязки полей ввода к модели: текст из SingleFieldViewModel
/// должен попадать в <see cref="DocumentBody"/>, Markdown и сгенерированный DOCX.
/// </summary>
public class FormFieldBindingTests : System.IDisposable
{
    private readonly string tempDir;

    public FormFieldBindingTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"form-binding-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void SingleFieldViewModel_ValueChange_UpdatesBodyAndMarkdown()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocId = "BIND-001", DocType = "ST", Title = "Тест" };
        var engine = new DocumentChangeEngine(body, meta);
        var descriptor = Forms.GetTemplate("ST").HeaderFields
            .OfType<SingleFieldDescriptor>()
            .First(d => d.Key == "Purpose");

        var vm = new SingleFieldViewModel(descriptor, body, engine);

        const string expected = "Обеспечить безопасность работ";
        vm.Value = expected;

        Assert.Equal(expected, body.Purpose);

        var markdown = DocumentBodyToMarkdown.Convert(meta, body, Forms.GetTemplate("ST"));
        Assert.Contains(expected, markdown);
    }

    [Fact]
    public void MainWindowViewModel_BuildFormFieldViewModels_CreatesFieldsWithEngine()
    {
        var vm = new MainWindowViewModel(
            new NullDialogServiceStub(),
            new MarkdownFileService(),
            new DocumentExportService(),
            new DocumentSaveService(),
            new MarkdownPreviewBuilder(),
            new NullClipboardServiceStub(),
            new NullFileSystemServiceStub(),
            new NullMdFileStore());

        vm.Metadata = new DocumentMetadata { DocId = "ENG-001", DocType = "ST", Title = "Тест" };
        vm.FormTemplate = Forms.GetTemplate("ST");
        vm.DocumentBody = new DocumentBody();
        DocumentBodyFactory.EnsureRequiredFields(vm.DocumentBody, vm.Metadata);

        Assert.NotNull(vm.FormViewModel);
        Assert.NotEmpty(vm.FormViewModel.FormFieldViewModels);
        Assert.Contains(vm.FormViewModel.FormFieldViewModels, x => x is SingleFieldViewModel);
        Assert.Contains(vm.FormViewModel.FormFieldViewModels, x => x is SectionsTreeViewModel);
        Assert.All(
            vm.FormViewModel.FormFieldViewModels.OfType<SingleFieldViewModel>(),
            field => Assert.NotNull(field.GetType().GetField("engine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(field)));
    }

    [Fact]
    public async Task SectionNodeViewModel_TextChange_UpdatesBodyAndDocx()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata { DocId = "BIND-002", DocType = "ST", Title = "Тест" };
        var engine = new DocumentChangeEngine(body, meta);
        engine.AddSection();
        var section = body.Sections[0];
        var nodeVm = new SectionNodeViewModel(section, body, engine);

        const string title = "Описание процесса";
        const string content = "Процесс начинается с подачи заявки.";
        nodeVm.Section.Title = title;
        nodeVm.Section.Body = content;

        var markdown = DocumentBodyToMarkdown.Convert(meta, body, Forms.GetTemplate("ST"));
        Assert.Contains(title, markdown);
        Assert.Contains(content, markdown);

        var service = new DocumentExportService();
        var path = Path.Combine(tempDir, "section.docx");
        await service.ExportDocxAsync(meta, body, path);

        using var doc = WordprocessingDocument.Open(path, false);
        var docxText = string.Concat(doc.MainDocumentPart!.Document.Body!.InnerText);
        Assert.Contains(title, docxText);
        Assert.Contains(content, docxText);
    }

    private sealed class NullDialogServiceStub : IDialogService
    {
        public Task<string?> OpenMarkdownFileAsync() => Task.FromResult<string?>(null);

        public Task<string?> SaveMarkdownFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task<string?> SaveDocxFileAsync(string? defaultFileName = null) => Task.FromResult<string?>(null);

        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;

        public Task ShowSuccessAsync(string title, string message, string filePath, bool offerOpenInExplorer = true) => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);

        public Task<string?> ShowNewDocumentDialogAsync(System.Collections.Generic.IReadOnlyList<TemplateSummary> templates) => Task.FromResult<string?>(null);

        public Task ShowAboutAsync() => Task.CompletedTask;
    }

    private sealed class NullClipboardServiceStub : IClipboardService
    {
        public Task SetTextAsync(string text) => Task.CompletedTask;
    }

    private sealed class NullFileSystemServiceStub : IFileSystemService
    {
        public bool FileExists(string path) => false;

        public bool DirectoryExists(string path) => false;

        public string? GetDirectoryName(string path) => null;

        public void StartExplorer(string path)
        {
        }

        public void StartExplorerWithSelect(string path)
        {
        }
    }
}
