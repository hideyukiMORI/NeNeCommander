using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Holds the localized formats and the local time zone that turn entry metadata into row text.
/// The host creates one format from its resources and the zone it owns, then supplies the same
/// instance to every projection. Rows are reused only while the projection receives the same
/// instance, so a new format re-projects every row and no row keeps text from another format.
/// </summary>
public sealed class EntryMetadataFormat
{
    private EntryMetadataFormat(TimeZoneInfo zone, Func<string, string> localize)
    {
        Zone = zone;
        SizeUnits =
        [
            localize("PaneRowSizeUnitBytes"),
            localize("PaneRowSizeUnitKilobytes"),
            localize("PaneRowSizeUnitMegabytes"),
            localize("PaneRowSizeUnitGigabytes"),
            localize("PaneRowSizeUnitTerabytes"),
        ];
        SizeIntegerFormat = localize("PaneRowSizeFormatInteger");
        SizeDecimalFormat = localize("PaneRowSizeFormatDecimal");
        ModifiedFormat = localize("PaneRowModifiedFormat");
        UnknownText = localize("PaneRowMetadataUnknown");
    }

    /// <summary>Gets the time zone a known modification time is shown in.</summary>
    internal TimeZoneInfo Zone { get; }

    /// <summary>Gets the size unit labels from bytes to the top unit, each 1024 times the previous.</summary>
    internal IReadOnlyList<string> SizeUnits { get; }

    /// <summary>Gets the composite format of a byte count below the first 1024 boundary.</summary>
    internal string SizeIntegerFormat { get; }

    /// <summary>Gets the composite format of a size shown with one decimal in a larger unit.</summary>
    internal string SizeDecimalFormat { get; }

    /// <summary>Gets the custom date and time format of a known modification time.</summary>
    internal string ModifiedFormat { get; }

    /// <summary>Gets the text of a size or time the provider did not report.</summary>
    internal string UnknownText { get; }

    /// <summary>
    /// Resolves every metadata resource once. The zone is supplied by the host because reading the
    /// process time zone is ambient state that Presentation does not own (CS-010).
    /// </summary>
    /// <param name="zone">Time zone the user reads modification times in.</param>
    /// <param name="localize">Resolves one localization resource key to its text.</param>
    /// <returns>The format every row projection uses.</returns>
    public static EntryMetadataFormat Create(TimeZoneInfo zone, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(localize);
        return new EntryMetadataFormat(zone, localize);
    }
}
