using System;
using System.Collections.Generic;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Supplies metadata formats built from the shipped resource values and fixed, rule-free zones so
/// no test depends on the machine's time zone.
/// </summary>
internal static class TestMetadataFormats
{
    private static readonly Dictionary<string, string> ShippedValues = new()
    {
        ["PaneRowSizeUnitBytes"] = "B",
        ["PaneRowSizeUnitKilobytes"] = "KB",
        ["PaneRowSizeUnitMegabytes"] = "MB",
        ["PaneRowSizeUnitGigabytes"] = "GB",
        ["PaneRowSizeUnitTerabytes"] = "TB",
        ["PaneRowModifiedFormat"] = "yyyy-MM-dd HH:mm",
        ["PaneRowMetadataUnknown"] = "\u2014",
    };

    /// <summary>Gets a fixed zone nine hours ahead of UTC.</summary>
    internal static TimeZoneInfo PlusNine { get; } =
        TimeZoneInfo.CreateCustomTimeZone("Test+09:00", TimeSpan.FromHours(9), "Test+09:00", "Test+09:00");

    /// <summary>Gets a fixed zone five and a half hours behind UTC.</summary>
    internal static TimeZoneInfo MinusFiveThirty { get; } =
        TimeZoneInfo.CreateCustomTimeZone("Test-05:30", new TimeSpan(-5, -30, 0), "Test-05:30", "Test-05:30");

    /// <summary>Gets the format that shows times in a fixed zone equal to UTC.</summary>
    internal static EntryMetadataFormat Utc { get; } = Create(
        TimeZoneInfo.CreateCustomTimeZone("Test+00:00", TimeSpan.Zero, "Test+00:00", "Test+00:00"));

    /// <summary>Creates a format with the shipped resource values and the given zone.</summary>
    /// <param name="zone">Fixed zone the format shows times in.</param>
    /// <returns>A new format instance.</returns>
    internal static EntryMetadataFormat Create(TimeZoneInfo zone)
    {
        return EntryMetadataFormat.Create(zone, Localize);
    }

    /// <summary>Resolves a metadata resource key to its shipped value.</summary>
    /// <param name="key">Resource key.</param>
    /// <returns>The shipped value.</returns>
    internal static string Localize(string key)
    {
        return ShippedValues[key];
    }
}
