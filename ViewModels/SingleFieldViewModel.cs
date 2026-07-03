using AvaloniaApp.Models;
using AvaloniaApp.Models.EngineRoles;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp.ViewModels;

/// <summary>
    /// ViewModel для одиночного текстового поля (<see cref="Views.Fields.SingleField"/>).
    /// Зависит только от ролевого интерфейса <see cref="ISingleFieldMutator"/>
    /// (ISP): SetPurpose/SetScope/SetPrimaryTraining/SetRecurrentTraining/SetParentDocument.
    /// </summary>
public partial class SingleFieldViewModel : ViewModelBase, IEngineAware
{
    private readonly SingleFieldDescriptor descriptor;
    private readonly DocumentBody body;
    private ISingleFieldMutator? engine;

    /// <summary>
    /// Gets or sets значение поля.
    /// </summary>
    [ObservableProperty]
    private string value;

    public SingleFieldViewModel(SingleFieldDescriptor descriptor, DocumentBody body, ISingleFieldMutator? engine)
    {
        this.descriptor = descriptor;
        this.body = body;
        this.engine = engine;
        value = descriptor.ValueGetter(body) ?? string.Empty;
    }

    /// <summary>
    /// Gets метка поля.
    /// </summary>
    public string Label => descriptor.Label;

    /// <summary>
    /// Gets placeholder.
    /// </summary>
    public string? Placeholder => descriptor.Placeholder;

    /// <summary>
    /// Gets признак обязательного поля.
    /// </summary>
    public bool IsRequired => descriptor.IsRequired;

    /// <summary>
    /// Gets признак многострочного поля.
    /// </summary>
    public bool IsMultiline => descriptor.IsMultiline;

    /// <summary>
    /// Заменяет движок мутаций. Используется при смене метаданных без
    /// пересоздания ViewModel и UI-коллекций.
    /// </summary>
    public void UpdateEngine(ISingleFieldMutator? newEngine)
    {
        engine = newEngine;
    }

    /// <inheritdoc />
    void IEngineAware.UpdateEngine(IDocumentChangeEngine? newEngine)
    {
        UpdateEngine(newEngine as ISingleFieldMutator);
    }

    partial void OnValueChanged(string value)
    {
        // ValueSetter объявлен как Action<DocumentBody, string?, IDocumentChangeEngine?>
        // (общий контракт для совместимости с делегатами в SingleFieldDescriptor).
        // Узкая роль ISingleFieldMutator upcast'ится к IDocumentChangeEngine только явно,
        // т.к. ролевой интерфейс не наследует общий контракт (ISP).
        descriptor.ValueSetter(body, value, engine as IDocumentChangeEngine);
    }
}
