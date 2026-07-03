using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaApp.Models;

namespace AvaloniaApp.Services;

/// <summary>
/// Каталог фабричных шаблонов в памяти.
/// </summary>
public class TemplateCatalog
{
    private readonly Dictionary<string, TemplateDefinition> byCode;

    public TemplateCatalog()
        : this(AppSettings.FactoryRoot)
    {
    }

    public TemplateCatalog(string factoryPath)
    {
        byCode = TemplateLoader.LoadFromDirectory(factoryPath)
            .ToDictionary(d => d.DocTypeCode, StringComparer.OrdinalIgnoreCase);
    }

    public TemplateDefinition? GetByDocType(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return byCode.TryGetValue(code, out var def) ? def : null;
    }
}
