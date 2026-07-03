using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;

namespace AvaloniaApp.Services;

/// <summary>
/// Единый движок мутаций <see cref="DocumentBody"/> + <see cref="DocumentMetadata"/>.
///
/// Все изменения документа проходят через этот класс. Движок гарантирует:
/// <list type="number">
///   <item>Синхронное обновление DocumentBody / DocumentMetadata.</item>
///   <item>Уведомление подписчиков через <see cref="Changed"/> event.</item>
///   <item>Корректную нумерацию позиций после добавления/удаления.</item>
///   <item>Идемпотентность повторных вызовов.</item>
/// </list>
///
/// <para>
/// <b>Архитектура (SRP):</b> Engine — это тонкий Facade, делегирующий работу
/// трём независимым сервисам по регионам ответственности:
/// </para>
/// <list type="bullet">
///   <item><see cref="ChangeLogManager"/> — ЛРИ, ревизии, read-only флаги.</item>
///   <item><see cref="NumberingEngine"/> — Position/Level/HierarchyPath/GlobalTableIndex.</item>
///   <item><see cref="CollectionObserver"/> — подписки на изменения коллекций/элементов.</item>
/// </list>
/// <para>
/// Facade реализует 5 ролевых интерфейсов (ISP): <see cref="IDocumentChangeEngine"/>,
/// <see cref="ISingleFieldMutator"/>, <see cref="ICollectionMutator"/>,
/// <see cref="ITreeMutator"/>, <see cref="IEngineCore"/>.
/// </para>
/// </summary>
public class DocumentChangeEngine
    : IDocumentChangeEngine,
      ISingleFieldMutator,
      ICollectionMutator,
      ITreeMutator,
      IEngineCore,
      IDisposable
{
    /// <summary>
    /// Максимальная глубина вложенности разделов. Единственная хардкод-константа.
    /// Уровень 1 = «4», уровень 2 = «4.1», уровень 3 = «4.1.1», ...
    /// Всё остальное — динамическое (Position + 1 / index).
    /// </summary>
    public const int MaxSectionDepth = 5;

    private readonly ChangeLogManager changeLogManager;
    private readonly NumberingEngine numberingEngine;
    private readonly CollectionObserver collectionObserver;

    /// <summary>
    /// Создаёт Facade движка для указанного тела и метаданных.
    /// Инициализирует 3 сервиса, навешивает подписки, рассчитывает стартовую нумерацию.
    /// </summary>
    public DocumentChangeEngine(DocumentBody body, DocumentMetadata metadata)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

        changeLogManager = new ChangeLogManager(body, metadata);
        numberingEngine = new NumberingEngine(body, metadata);
        collectionObserver = new CollectionObserver(body, metadata);

        // Подписываемся на изменения свойств метаданных. Это критично для UI:
        // TextBox-ы вкладки "Метаданные" редактируют свойства Title/DocId/...
        // без переприсвоения ссылки Metadata, поэтому OnMetadataChanged в VM не стреляет.
        Metadata.PropertyChanged += OnMetadataPropertyChanged;

        // Коллекции: уведомляем подписчиков об изменении элемента.
        collectionObserver.ItemChanged += OnCollectionItemChanged;

        // Сразу рассчитываем HierarchyPath для существующих секций (вызывается
        // из NewDocument с шаблонными секциями, у которых HierarchyPath пустой).
        numberingEngine.RenumberSections();

        // Перенумеровываем подсекции (таблицы) внутри всех приложений.
        foreach (var appendix in Body.Appendices)
        {
            numberingEngine.RenumberAppendixSections(appendix);
        }

        // Рассчитываем сквозной GlobalTableIndex для таблиц во всех приложениях.
        numberingEngine.RecalculateGlobalTableIndex();

        // Подписываемся на изменения элементов коллекций.
        collectionObserver.HookAll();
        changeLogManager.UpdateReadOnlyFlags();
    }

    /// <summary>
    /// Событие изменения документа. Стреляет ПОСЛЕ применения мутации к Body/Metadata.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Структурное изменение: добавление/удаление секций, терминов, приложений, строк ЛРИ.
    /// Подписчики (дерево секций) перестраиваются немедленно.
    /// </summary>
    public event EventHandler? StructureChanged;

    /// <summary>
    /// Изменение контента: ввод текста в полях/элементах коллекций.
    /// Подписчики обновляют превью с debounce.
    /// </summary>
    public event EventHandler? ContentChanged;

    public DocumentBody Body { get; }

    /// <summary>
    /// Gets метаданные документа, за изменениями которых следит движок.
    /// </summary>
    public DocumentMetadata Metadata { get; }

    public void Dispose()
    {
        Metadata.PropertyChanged -= OnMetadataPropertyChanged;
        collectionObserver.ItemChanged -= OnCollectionItemChanged;
        collectionObserver.UnhookAll();
        Changed = null;
        StructureChanged = null;
        ContentChanged = null;
        GC.SuppressFinalize(this);
    }

    // === Простые string-поля (ISingleFieldMutator) ===

    public void SetPurpose(string value)
    {
        Body.Purpose = value ?? string.Empty;
        RaiseContentChanged();
    }

    public void SetScope(string value)
    {
        Body.Scope = value ?? string.Empty;
        RaiseContentChanged();
    }

    public void SetPrimaryTraining(string value)
    {
        Body.PrimaryTraining = value ?? string.Empty;
        RaiseContentChanged();
    }

    public void SetRecurrentTraining(string value)
    {
        Body.RecurrentTraining = value ?? string.Empty;
        RaiseContentChanged();
    }

    public void SetParentDocument(string value)
    {
        Body.ParentDocument = value ?? string.Empty;
        RaiseContentChanged();
    }

    // === Коллекции — точечные мутации (ICollectionMutator) ===

    public void ReplaceTerms(IEnumerable<TermPair> terms)
    {
        Body.Terms.Clear();
        if (terms != null)
        {
            foreach (var t in terms)
            {
                Body.Terms.Add(t);
            }
        }

        numberingEngine.RenumberTerms();
        RaiseStructureChanged();
    }

    public void AddTerm()
    {
        Body.Terms.Add(new TermPair
        {
            Position = Body.Terms.Count,
            Term = string.Empty,
            Definition = string.Empty,
        });
        numberingEngine.RenumberTermsFrom(Body.Terms.Count - 1);
        RaiseStructureChanged();
    }

    public void RemoveTerm(TermPair term)
    {
        if (term is null)
        {
            return;
        }

        int index = Body.Terms.IndexOf(term);
        if (index < 0)
        {
            return;
        }

        Body.Terms.RemoveAt(index);
        numberingEngine.RenumberTermsFrom(index);
        RaiseStructureChanged();
    }

    public void AddSectionToAppendix(BodyAppendix appendix)
    {
        if (appendix is null)
        {
            return;
        }

        appendix.Sections.Add(new BodySection
        {
            Title = string.Empty,
            Body = string.Empty,
            Level = 1,
            Position = appendix.Sections.Count,
        });

        numberingEngine.RenumberAppendixSections(appendix);
        numberingEngine.RecalculateGlobalTableIndex();
        RaiseStructureChanged();
    }

    public void RemoveSectionFromAppendix(BodyAppendix appendix, BodySection section)
    {
        if (appendix is null || section is null)
        {
            return;
        }

        if (!appendix.Sections.Remove(section))
        {
            return; // секция не принадлежит этому приложению — ничего не делаем.
        }

        numberingEngine.RenumberAppendixSections(appendix);
        numberingEngine.RecalculateGlobalTableIndex();
        RaiseStructureChanged();
    }

    public void AddAppendix()
    {
        var appendix = new BodyAppendix
        {
            Position = Body.Appendices.Count + 1,
            Title = string.Empty,
            Body = string.Empty,
        };

        // Новое приложение автоматически получает одну подсекцию-таблицу.
        appendix.Sections.Add(new BodySection
        {
            Title = string.Empty,
            Body = string.Empty,
            Level = 1,
            Position = 0,
        });

        Body.Appendices.Add(appendix);
        numberingEngine.RenumberAppendicesFrom(Body.Appendices.Count - 1);
        numberingEngine.RenumberAppendixSections(appendix);
        numberingEngine.RecalculateGlobalTableIndex();
        RaiseStructureChanged();
    }

    public void RemoveAppendix(BodyAppendix appendix)
    {
        if (appendix is null)
        {
            return;
        }

        int index = Body.Appendices.IndexOf(appendix);
        if (index < 0)
        {
            return;
        }

        Body.Appendices.RemoveAt(index);
        numberingEngine.RenumberAppendicesFrom(index);
        numberingEngine.RecalculateGlobalTableIndex();
        RaiseStructureChanged();
    }

    public void AddRelatedDocument()
    {
        Body.RelatedDocuments.Add(new StringItem { Value = string.Empty });
        RaiseStructureChanged();
    }

    public void RemoveRelatedDocument(StringItem doc)
    {
        if (doc is null)
        {
            return;
        }

        Body.RelatedDocuments.Remove(doc);
        RaiseStructureChanged();
    }

    public void AddChangeLogRow(bool autoIncrementRevision = false)
    {
        changeLogManager.AddRow(autoIncrementRevision);
        RaiseStructureChanged();
    }

    public void RemoveChangeLogRow(ChangeLogRow row)
    {
        changeLogManager.RemoveRow(row);
        RaiseStructureChanged();
    }

    // === Дерево секций (ITreeMutator) ===

    public void AddSection(BodySection? parent = null)
    {
        // Проверяем лимит глубины вложенности
        if (parent != null && parent.Level >= MaxSectionDepth)
        {
            // Не добавляем подраздел глубже MaxSectionDepth
            return;
        }

        var section = numberingEngine.CreateSectionWithTemplate(parent);

        if (parent != null)
        {
            section.ParentId = parent.Id;
            section.Level = parent.Level + 1;
            section.Position = parent.Children.Count;
            parent.Children.Add(section);
            numberingEngine.RenumberSectionsFrom(parent.Children, parent.Children.Count - 1, section.Level, parent!.Id, parent!.HierarchyPath);
        }
        else
        {
            section.Position = Body.Sections.Count;
            section.Level = 1;
            Body.Sections.Add(section);
            numberingEngine.RenumberSectionsFrom(Body.Sections, Body.Sections.Count - 1, 1, 0, string.Empty);
        }

        RaiseStructureChanged();
    }

    public void RemoveSection(BodySection section)
    {
        if (section is null)
        {
            return;
        }

        // Сначала рекурсивно удаляем всех потомков.
        NumberingEngine.RemoveAllDescendants(section);

        // Находим родительский контейнер (по ссылке, а не по ID — все новые секции имеют Id=0).
        ObservableCollection<BodySection> parentCollection;
        int removedIndex;
        int level;
        long parentId;
        string parentPath;

        var parent = NumberingEngine.FindParentSection(Body.Sections, section);
        if (parent == null)
        {
            parentCollection = Body.Sections;
            removedIndex = parentCollection.IndexOf(section);
            level = 1;
            parentId = 0;
            parentPath = string.Empty;
        }
        else
        {
            parentCollection = parent.Children;
            removedIndex = parentCollection.IndexOf(section);
            level = parent.Level + 1;
            parentId = parent.Id;
            parentPath = parent.HierarchyPath;
        }

        if (removedIndex < 0)
        {
            return;
        }

        parentCollection.RemoveAt(removedIndex);
        numberingEngine.RenumberSectionsFrom(parentCollection, removedIndex, level, parentId, parentPath);
        RaiseStructureChanged();
    }

    // === Replace-операции (для открытия .md файлов) ===

    public void ReplaceSections(IEnumerable<BodySection> sections)
    {
        Body.Sections.Clear();
        if (sections != null)
        {
            foreach (var s in sections)
            {
                Body.Sections.Add(s);
            }
        }

        numberingEngine.RenumberSections();
        numberingEngine.RecalculateGlobalTableIndex();
        RaiseStructureChanged();
    }

    public void ReplaceRelatedDocuments(IEnumerable<StringItem> docs)
    {
        Body.RelatedDocuments.Clear();
        if (docs != null)
        {
            foreach (var d in docs)
            {
                Body.RelatedDocuments.Add(d);
            }
        }

        numberingEngine.RecalculateGlobalTableIndex();
        RaiseStructureChanged();
    }

    public void ReplaceChangeLog(IEnumerable<ChangeLogRow> rows)
    {
        changeLogManager.Replace(rows);
        RaiseStructureChanged();
    }

    // === Метаданные (IEngineCore) ===

    /// <summary>
    /// Устанавливает произвольное свойство метаданных по имени.
    /// Используется UI-полями вкладки "Метаданные" через DataBinding.
    /// </summary>
    public bool TrySetMetadata(string propertyName, object? value)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        var prop = typeof(DocumentMetadata).GetProperty(propertyName);
        if (prop is null || !prop.CanWrite)
        {
            return false;
        }

        try
        {
            var coerced = value is null ? null : Convert.ChangeType(value, prop.PropertyType);
            prop.SetValue(Metadata, coerced);

            // OnMetadataPropertyChanged автоматически вызовет RaiseChanged.
            return true;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Заменяет все метаданные целиком (используется при открытии .md файла).
    /// Отписывается от старого объекта, подписывается на новый.
    /// </summary>
    public void ReplaceMetadata(DocumentMetadata newMetadata)
    {
        if (newMetadata is null)
        {
            throw new ArgumentNullException(nameof(newMetadata));
        }

        Metadata.PropertyChanged -= OnMetadataPropertyChanged;
        var oldMetadata = Metadata;

        // ReplaceMetadata не меняет ссылку — UI уже подписан на старую через DataContext.
        // Нужно обновить поля in-place, чтобы не терять подписку ObservableCollection.
        CopyMetadataProperties(newMetadata, oldMetadata);

        // Re-subscribe (idempotent в данном случае).
        oldMetadata.PropertyChanged += OnMetadataPropertyChanged;
        RaiseChanged();
    }

    /// <summary>
    /// Принудительно уведомляет подписчиков (используется при загрузке документа).
    /// </summary>
    public void NotifyChanged() => RaiseChanged();

    /// <summary>
    /// Возвращает последнюю строку ЛРИ или null, если строк нет.
    /// Делегирует в <see cref="ChangeLogManager"/>.
    /// </summary>
    public ChangeLogRow? GetLastChangeLogRow() => changeLogManager.GetLastRow();

    /// <summary>
    /// Проверяет, отличаются ли текущие метаданные (Revision/EffectiveDate)
    /// от последней строки ЛРИ.
    /// Делегирует в <see cref="ChangeLogManager"/>.
    /// </summary>
    public bool IsChangeLogOutOfSync() => changeLogManager.IsOutOfSync();

    // === Приватные хелперы ===

    private static void CopyMetadataProperties(DocumentMetadata source, DocumentMetadata target)
    {
        target.DocId = source.DocId;
        target.DocType = source.DocType;
        target.Title = source.Title;
        target.DepartmentName = source.DepartmentName;
        target.Revision = source.Revision;
        target.Status = source.Status;
        target.EffectiveDate = source.EffectiveDate;
        target.Author = source.Author;
        target.Owner = source.Owner;
        target.Approver = source.Approver;
        target.ApproverRole = source.ApproverRole;
        target.Protocol = source.Protocol;
        target.FooterText = source.FooterText;
    }

    private void OnMetadataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Не стреляем Changed для фасадных свойств — иначе двойной Notify при редактировании
        // DocumentName (которое проксирует Title). ObservableProperty сам стреляет
        // PropertyChanged для underlying Title, этого достаточно.
        if (e.PropertyName is nameof(DocumentMetadata.DocumentName)
            or nameof(DocumentMetadata.DocumentCode)
            or nameof(DocumentMetadata.ApprovedBy)
            or nameof(DocumentMetadata.DateApproved))
        {
            return;
        }

        // Date/Revision/Author строк ЛРИ НЕ синхронизируем автоматически с метаданными —
        // они историчны. Author последней строки транслируется в Metadata.Author
        // через OnCollectionItemChanged при редактировании пользователем.
        if (e.PropertyName is nameof(DocumentMetadata.Author))
        {
            // Автор теперь редактируется внутри строк ЛРИ; изменение Metadata.Author
            // извне (например, при загрузке YAML) не перезаписывает строки.
        }

        RaiseContentChanged();
    }

    /// <summary>
    /// Обработчик изменения элемента коллекции (вызывается из <see cref="CollectionObserver"/>).
    /// Содержит специальную логику синхронизации Author последней строки ЛРИ.
    /// </summary>
    private void OnCollectionItemChanged(object? sender, CollectionItemChangedEventArgs e)
    {
        // Author последней строки ЛРИ является автором текущей редакции документа.
        if (e.PropertyName == nameof(ChangeLogRow.Author)
            && e.Item is ChangeLogRow row
            && row == collectionObserver.GetLastChangeLogRow())
        {
            Metadata.Author = row.Author;
        }

        RaiseContentChanged();
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseStructureChanged()
    {
        StructureChanged?.Invoke(this, EventArgs.Empty);
        RaiseChanged();
    }

    private void RaiseContentChanged()
    {
        ContentChanged?.Invoke(this, EventArgs.Empty);
        RaiseChanged();
    }
}
