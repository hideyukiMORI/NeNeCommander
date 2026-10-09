using System;
using System.Globalization;
using System.Text;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Composes the one localized line a pane's status text shows: the status alone when nothing is
/// ordered, otherwise the status and the sort indication joined by the line separator. The
/// separator is part of the status-line design rather than of a language, so it is a literal
/// format here and resources supply only the words. The host resolves resources and assigns the
/// result; it decides nothing about the line.
/// </summary>
public static class PaneStatusLine
{
    private static readonly CompositeFormat SortLineFormat = CompositeFormat.Parse("{0} · {1}");

    /// <summary>Composes the localized status line of one pane.</summary>
    /// <param name="status">Status the pane shows, already chosen by its presentation.</param>
    /// <param name="sortStatus">Sort indication of the listed rows, or absence when nothing is listed.</param>
    /// <param name="localize">Resolves one localization resource key to its text.</param>
    /// <returns>The complete localized line.</returns>
    public static string Compose(
        PaneStatus status,
        PaneSortStatus? sortStatus,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(localize);
        string statusText = localize(status.ResourceKey);
        return sortStatus is null
            ? statusText
            : string.Format(
                CultureInfo.InvariantCulture,
                SortLineFormat,
                statusText,
                localize(sortStatus.ResourceKey));
    }
}
