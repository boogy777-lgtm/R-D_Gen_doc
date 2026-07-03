using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Реализация <see cref="IChangeLogPromptService"/>, делегирующая отображение
/// диалога <see cref="IDialogService"/> и мутацию движку
/// <see cref="IDocumentChangeEngine"/>.
///
/// Сервис хранит «последние известные» значения <c>Revision</c> и
/// <c>EffectiveDate</c> — после успешного показа диалога (даже отклонённого)
/// эти значения помечаются как «обработанные», чтобы пользователю не задавали
/// тот же вопрос повторно при последующих изменениях того же поля.
/// </summary>
public sealed class ChangeLogPromptService : IChangeLogPromptService
{
    private readonly IDocumentChangeEngine engine;
    private readonly IDialogService dialogService;
    private readonly SemaphoreSlim promptLock = new(1, 1);

    /// <summary>
    /// Последняя «известная» ревизия (обработанная пользователем).
    /// </summary>
    private string lastKnownRevision = string.Empty;

    /// <summary>
    /// Последняя «известная» дата вступления в силу (обработанная пользователем).
    /// </summary>
    private string lastKnownEffectiveDate = string.Empty;

    public ChangeLogPromptService(IDocumentChangeEngine engine, IDialogService dialogService)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <summary>
    /// Инициализирует «известные» значения из метаданных. Должна вызываться
    /// после замены движка/документа, чтобы пользователю не задавали
    /// автоматический вопрос при первом рендере.
    /// </summary>
    public void SeedKnownValues(DocumentMetadata metadata)
    {
        if (metadata is null)
        {
            return;
        }

        lastKnownRevision = metadata.Revision;
        lastKnownEffectiveDate = metadata.EffectiveDate;
    }

    /// <summary>
    /// Отмечает текущие значения метаданных как обработанные без показа диалога.
    /// Используется, когда пользователь только что добавил строку через
    /// прямой вызов <see cref="IDocumentChangeEngine.AddChangeLogRow"/>.
    /// </summary>
    public void MarkCurrentAsKnown(DocumentMetadata metadata)
    {
        if (metadata is null)
        {
            return;
        }

        lastKnownRevision = metadata.Revision;
        lastKnownEffectiveDate = metadata.EffectiveDate;
    }

    /// <summary>
    /// Регистрирует изменение ревизии/даты из метаданных. Если значения
    /// изменились по сравнению с last-known, инициирует асинхронный показ
    /// диалога (fire-and-forget безопасен: семафор защищает от гонок).
    /// </summary>
    public void NotifyMetadataChanged(DocumentMetadata metadata)
    {
        if (metadata is null)
        {
            return;
        }

        var revisionChanged = !string.Equals(metadata.Revision, lastKnownRevision, StringComparison.Ordinal);
        var dateChanged = !string.Equals(metadata.EffectiveDate, lastKnownEffectiveDate, StringComparison.Ordinal);

        if (!revisionChanged && !dateChanged)
        {
            return;
        }

        var reason = (revisionChanged, dateChanged) switch
        {
            (true, true) => "номер редакции и дата",
            (true, false) => "номер редакции",
            (false, true) => "дата редакции",
            _ => "данные",
        };

        // Fire-and-forget: VM живёт столько же, сколько окно; исключения
        // логируются внутри PromptAndAddAsync.
        _ = PromptAndAddAsync(reason);
    }

    /// <inheritdoc />
    public async Task PromptAndAddAsync(string reason, CancellationToken cancellationToken = default)
    {
        var metadata = engine.Metadata;
        if (metadata is null)
        {
            return;
        }

        if (!await promptLock.WaitAsync(0, cancellationToken))
        {
            // Уже идёт другой диалог.
            return;
        }

        try
        {
            var confirmed = await dialogService.ShowConfirmationAsync(
                "Добавить запись в ЛРИ?",
                $"Вы изменили {reason}. Добавить новую строку в Лист регистрации изменений?");

            if (confirmed)
            {
                // UX-аудит #6.12: при ручном добавлении через popup
                // используем autoIncrementRevision=true, чтобы новая строка
                // соответствовала СЛЕДУЮЩЕЙ ревизии (а не текущей).
                // Иначе баннер "не отражено в ЛРИ" не сбросится — формально
                // строка добавлена, но с тем же Revision, что и предыдущая.
                engine.AddChangeLogRow(autoIncrementRevision: true);
            }

            // Обновляем «известные» значения независимо от ответа,
            // чтобы не спрашивать повторно по той же причине.
            lastKnownRevision = metadata.Revision;
            lastKnownEffectiveDate = metadata.EffectiveDate;
        }
        catch (OperationCanceledException)
        {
            // Окно закрыто / отмена — игнорируем.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChangeLogPromptService.PromptAndAddAsync] {ex}");
        }
        finally
        {
            promptLock.Release();
        }
    }
}
