using System;
using System.Globalization;
using System.Text;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Composes the one localized line a pane's status text shows: the status, sort indication, and
/// observed location indication when present, joined by the line separator. The separator is
/// part of the status-line design rather than of a language, so it is a literal
/// format here and resources supply only the words. The host resolves resources and assigns the
/// result; it decides nothing about the line.
/// </summary>
public static class PaneStatusLine
{
    private static readonly CompositeFormat SegmentFormat = CompositeFormat.Parse("{0} · {1}");

    /// <summary>Composes the localized status line of one pane.</summary>
    /// <param name="status">Status the pane shows, already chosen by its presentation.</param>
    /// <param name="sortStatus">Sort indication of the listed rows, or absence when nothing is listed.</param>
    /// <param name="locationStatus">Indication of an observed location fact, or absence.</param>
    /// <param name="localize">Resolves one localization resource key to its text.</param>
    /// <returns>The complete localized line.</returns>
    public static string Compose(
        PaneStatus status,
        PaneSortStatus? sortStatus,
        PaneLocationStatus? locationStatus,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(localize);
        string statusText = localize(status.ResourceKey);
        if (sortStatus is not null)
        {
            statusText = string.Format(
                CultureInfo.InvariantCulture,
                SegmentFormat,
                statusText,
                localize(sortStatus.ResourceKey));
        }
        return locationStatus is null
            ? statusText
            : string.Format(
                CultureInfo.InvariantCulture,
                SegmentFormat,
                statusText,
                localize(locationStatus.ResourceKey));
    }
}
