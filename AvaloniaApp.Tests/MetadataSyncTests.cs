using AvaloniaApp.Models;
using AvaloniaApp.Services;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионные тесты: изменения свойств DocumentMetadata через DocumentChangeEngine
/// стреляют событие Changed, что позволяет MainWindowViewModel.RefreshPreview обновлять
/// MD Preview без переприсвоения ссылки Metadata.
///
/// Без этого подписчика UI вкладки "Метаданные" был неполноценным — изменения в TextBox
/// обновляли модель, но не обновляли preview, пока пользователь явно не переключал вкладку.
/// </summary>
public class MetadataSyncTests
{
    [Fact]
    public void Engine_TracksMetadata()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        Assert.Same(meta, engine.Metadata);
        Assert.Same(body, engine.Body);
    }

    [Fact]
    public void ChangingTitle_FiresChanged()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        meta.Title = "Новое название";

        Assert.Equal(1, fired);
        Assert.Equal("Новое название", engine.Metadata.Title);
    }

    [Fact]
    public void ChangingDocId_FiresChanged()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        meta.DocId = "BP-001";

        Assert.Equal(1, fired);
    }

    [Fact]
    public void ChangingDocumentNameFacade_DoesNotDoubleFire()
    {
        // DocumentName — фасад для Title. Если пользователь редактирует
        // поле "Наименование документа" (привязанное к DocumentName), то
        // PropertyChanged стреляет сначала для DocumentName, потом для Title.
        // Если оба вызовут RaiseChanged, получим двойное обновление preview.
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        meta.DocumentName = "Через фасад";

        Assert.Equal(1, fired);
        Assert.Equal("Через фасад", meta.Title);
    }

    [Fact]
    public void ChangingAllFacadeProperties_DoesNotDoubleFire()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        meta.DocumentName = "A";
        meta.DocumentCode = "C";
        meta.ApprovedBy = "B";
        meta.DateApproved = "D";

        // 4 фасада → 4 обновления Title/DocId/Approver/EffectiveDate → 4 Changed
        Assert.Equal(4, fired);
    }

    [Fact]
    public void TrySetMetadata_UpdatesProperty()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        Assert.True(engine.TrySetMetadata(nameof(DocumentMetadata.Title), "Через TrySet"));
        Assert.Equal("Через TrySet", meta.Title);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void TrySetMetadata_ReturnsFalseForUnknownProperty()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        Assert.False(engine.TrySetMetadata("NonExistent", "value"));
        Assert.False(engine.TrySetMetadata(string.Empty, "value"));
        Assert.Equal(0, fired);
    }

    [Fact]
    public void TrySetMetadata_HandlesTypeConRevision()
    {
        // Convert.ChangeType успешно конвертирует int → string ("123").
        // Это допустимое поведение — пользователь может случайно передать int
        // там, где ожидается string. Engine не должен падать.
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        Assert.True(engine.TrySetMetadata(nameof(DocumentMetadata.Revision), 123));
        Assert.Equal("123", meta.Revision);
    }

    [Fact]
    public void ReplaceMetadata_CopiesProperties()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var newMeta = new DocumentMetadata
        {
            Title = "Replaced",
            DocId = "NEW-ID",
            DocType = "BP",
            DepartmentName = "НИОКР",
            Revision = "2.0",
            Author = "Иванов",
        };

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        engine.ReplaceMetadata(newMeta);

        Assert.Equal("Replaced", meta.Title);
        Assert.Equal("NEW-ID", meta.DocId);
        Assert.Equal("BP", meta.DocType);
        Assert.Equal("2.0", meta.Revision);
        Assert.Equal("Иванов", meta.Author);

        // RaiseChanged вызывается один раз явно, остальные PropertyChanged
        // от ObservableProperty могли стрелять — поэтому >= 1
        Assert.True(fired >= 1);
    }

    [Fact]
    public void Dispose_UnsubscribesFromMetadata()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        engine.Dispose();

        meta.Title = "After dispose";

        Assert.Equal(0, fired);
    }

    [Fact]
    public void Dispose_AllowsReuseOfMetadata()
    {
        // После Dispose старый Metadata больше не должен получать события от старого Engine.
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        var engine1 = new DocumentChangeEngine(body, meta);
        engine1.Dispose();

        // Создаём новый Engine с тем же Metadata — должен подписаться заново.
        using var engine2 = new DocumentChangeEngine(body, meta);
        var fired = 0;
        engine2.Changed += (_, _) => fired++;

        meta.Title = "From engine2";

        Assert.Equal(1, fired);
    }

    [Fact]
    public void MetadataAndBody_FiresChangedOnceEach()
    {
        var body = new DocumentBody();
        var meta = new DocumentMetadata();
        using var engine = new DocumentChangeEngine(body, meta);

        var fired = 0;
        engine.Changed += (_, _) => fired++;

        meta.Title = "Meta change";
        engine.AddTerm();

        Assert.Equal(2, fired);
    }
}
