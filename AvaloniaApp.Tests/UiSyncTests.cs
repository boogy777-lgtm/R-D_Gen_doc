using System.Collections.ObjectModel;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using AvaloniaApp.Views.Fields;
using Xunit;

namespace AvaloniaApp.Tests;

/// <summary>
/// Регрессионные тесты: после Engine.Changed UI-коллекция синхронизируется с Body.
/// Это критично для UX: иначе после Engine.AddTerm() пользователь не видит новую строку.
/// </summary>
public class UiSyncTests
{
    [Fact]
    public void SyncCollection_AddsMissingItems()
    {
        var ui = new ObservableCollection<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
        };
        var body = new System.Collections.Generic.List<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
            new() { Position = 1, Term = "B", Definition = "2" },
            new() { Position = 2, Term = "C", Definition = "3" },
        };

        SyncCollectionHelper.Sync(ui, body);

        Assert.Equal(3, ui.Count);
        Assert.Equal("A", ui[0].Term);
        Assert.Equal("B", ui[1].Term);
        Assert.Equal("C", ui[2].Term);
    }

    [Fact]
    public void SyncCollection_RemovesExtraItems()
    {
        var ui = new ObservableCollection<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
            new() { Position = 1, Term = "B", Definition = "2" },
            new() { Position = 2, Term = "C", Definition = "3" },
        };
        var body = new System.Collections.Generic.List<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
        };

        SyncCollectionHelper.Sync(ui, body);

        Assert.Single(ui);
        Assert.Equal("A", ui[0].Term);
    }

    [Fact]
    public void SyncCollection_UpdatesChangedItems()
    {
        var ui = new ObservableCollection<TermPair>
        {
            new() { Position = 0, Term = "OLD", Definition = "OLD" },
        };
        var body = new System.Collections.Generic.List<TermPair>
        {
            new() { Position = 0, Term = "NEW", Definition = "NEW" },
        };

        SyncCollectionHelper.Sync(ui, body);

        Assert.Single(ui);
        Assert.Equal("NEW", ui[0].Term);
        Assert.Equal("NEW", ui[0].Definition);
    }

    [Fact]
    public void SyncCollection_HandlesEmptySource()
    {
        var ui = new ObservableCollection<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
        };
        var body = new System.Collections.Generic.List<TermPair>();

        SyncCollectionHelper.Sync(ui, body);

        Assert.Empty(ui);
    }

    [Fact]
    public void SyncCollection_HandlesEmptyUi()
    {
        var ui = new ObservableCollection<TermPair>();
        var body = new System.Collections.Generic.List<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
            new() { Position = 1, Term = "B", Definition = "2" },
        };

        SyncCollectionHelper.Sync(ui, body);

        Assert.Equal(2, ui.Count);
    }

    [Fact]
    public void Engine_AddTerm_TriggersSyncOfUiCollection()
    {
        // Имитируем FormControl: создаём UI-поле с копией body.Terms
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var uiCollection = new ObservableCollection<TermPair>(body.Terms);

        // Подписываемся на Engine.Changed
        engine.Changed += (_, _) =>
        {
            SyncCollectionHelper.Sync(uiCollection, body.Terms);
        };

        // Act: добавляем термин через engine
        engine.AddTerm();

        // Assert: UI-коллекция выросла
        Assert.Single(uiCollection);
        Assert.Empty(uiCollection[0].Term);
    }

    [Fact]
    public void Engine_AddMultipleTerms_AllAppearInUi()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());
        var uiCollection = new ObservableCollection<TermPair>(body.Terms);

        engine.Changed += (_, _) =>
        {
            SyncCollectionHelper.Sync(uiCollection, body.Terms);
        };

        engine.AddTerm();
        engine.AddTerm();
        engine.AddTerm();

        Assert.Equal(3, uiCollection.Count);
    }

    [Fact]
    public void Engine_RemoveTerm_RemovesFromUi()
    {
        var body = new DocumentBody();
        var engine = new DocumentChangeEngine(body, new DocumentMetadata());

        // Сначала добавляем через engine
        engine.AddTerm();
        engine.AddTerm();
        engine.AddTerm();

        // Затем заполняем
        engine.ReplaceTerms(new System.Collections.Generic.List<TermPair>
        {
            new() { Position = 0, Term = "A", Definition = "1" },
            new() { Position = 1, Term = "B", Definition = "2" },
            new() { Position = 2, Term = "C", Definition = "3" },
        });

        var uiCollection = new ObservableCollection<TermPair>(body.Terms);
        engine.Changed += (_, _) =>
        {
            SyncCollectionHelper.Sync(uiCollection, body.Terms);
        };

        // Удаляем средний
        engine.RemoveTerm(body.Terms[1]);

        Assert.Equal(2, uiCollection.Count);
        Assert.Equal("A", uiCollection[0].Term);
        Assert.Equal("C", uiCollection[1].Term);
    }
}
