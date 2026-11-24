using System;
using System.Globalization;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Composes the localized count text for the exact listing whose rows remain displayed.</summary>
public static class PaneCountsLine
{
    /// <summary>
    /// Distinguishes unavailable counts from an empty listing, shown counts from loaded counts,
    /// incomplete reads from whole-directory totals, and retained content from a refreshed listing.
    /// The same complete text is available to the visible label, tooltip and accessibility peer.
    /// </summary>
    /// <param name="presentation">The current pane projection, including its exact source snapshot.</param>
    /// <param name="localize">Resolves a complete localized template or state qualifier.</param>
    /// <returns>Complete localized count text without filesystem queries or ambient culture.</returns>
    public static string Compose(PanePresentation presentation, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(localize);
        if (presentation.Counts is not PaneCounts counts ||
            presentation.SourceSnapshot.Content is not PaneContentListed listed)
        {
            return localize("PaneCountsUnavailable");
        }
        string text = counts.Visible.Total == counts.Loaded.Total
            ? Format(localize("PaneCountsFormat"),
                counts.Visible.Files, counts.Visible.Directories, counts.Selected)
            : Format(localize("PaneCountsVisibleFormat"),
                counts.Visible.Files, counts.Visible.Directories, counts.Selected,
                counts.Loaded.Files, counts.Loaded.Directories);
        if (listed.Listing.Completeness == DirectoryListingCompleteness.Bounded ||
            listed.Listing.UnrepresentableEntryCount > 0)
        {
            text = Format(localize("PaneCountsLoadedOnlyFormat"), text);
        }
        return presentation.SourceSnapshot.Activity is PaneLoading or PaneReadCancelled or PaneReadAbandoned or PaneReadFailed
            ? Format(localize("PaneCountsPreviousFormat"), text)
            : text;
    }

    private static string Format(string format, params object[] values)
    {
        return string.Format(CultureInfo.InvariantCulture, format, values);
    }
}
