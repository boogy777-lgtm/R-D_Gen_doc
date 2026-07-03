using System.Collections.ObjectModel;

namespace AvaloniaApp.Tests;

/// <summary>
/// Helper для тестирования SyncCollection (метод private static в FormControl).
/// Логика идентична FormControl.SyncCollection.
/// </summary>
internal static class SyncCollectionHelper
{
    public static void Sync<T>(ObservableCollection<T> ui, System.Collections.Generic.IEnumerable<T> source)
    {
        if (ui is null || source is null)
        {
            return;
        }

        var sourceList = source as System.Collections.Generic.IList<T> ?? source.ToList();

        while (ui.Count > sourceList.Count)
        {
            ui.RemoveAt(ui.Count - 1);
        }

        while (ui.Count < sourceList.Count)
        {
            ui.Add(sourceList[ui.Count]);
        }

        for (int i = 0; i < sourceList.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(ui[i], sourceList[i]))
            {
                ui[i] = sourceList[i];
            }
        }
    }
}
