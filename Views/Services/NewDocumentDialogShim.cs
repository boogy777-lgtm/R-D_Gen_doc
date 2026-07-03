using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;

namespace AvaloniaApp.Views.Services;

/// <summary>
/// Делегат к <see cref="NewDocumentDialog.ShowAsync"/> для
/// <see cref="AvaloniaDialogService"/>. Изолирует единственный
/// оставшийся кейс прямой зависимости от View в одном месте.
/// </summary>
internal static class NewDocumentDialogShim
{
    public static async Task<string?> ShowAsync(IReadOnlyList<TemplateSummary> templates)
    {
        // AvaloniaDialogService работает в контексте UI-потока,
        // а MainWindow — owner. Берём его через Avalonia.Application.Current.
        var app = Avalonia.Application.Current;
        var lifetime = app?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.MainWindow;
        if (owner == null)
        {
            return null;
        }

        return await NewDocumentDialog.ShowAsync(owner, templates.ToList());
    }
}
