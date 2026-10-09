using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the fixed-width size and local modification texts of a row.</summary>
[TestClass]
public sealed class EntryMetadataFormatterTests
{
    private const long Kibibyte = 1024L;
    private const long Mebibyte = Kibibyte * 1024L;
    private const long Gibibyte = Mebibyte * 1024L;
    private const long Tebibyte = Gibibyte * 1024L;

    private static readonly string[] MetadataResourceKeys =
    [
        "PaneRowSizeUnitBytes",
        "PaneRowSizeUnitKilobytes",
        "PaneRowSizeUnitMegabytes",
        "PaneRowSizeUnitGigabytes",
        "PaneRowSizeUnitTerabytes",
        "PaneRowSizeFormatInteger",
        "PaneRowSizeFormatDecimal",
        "PaneRowModifiedFormat",
        "PaneRowMetadataUnknown",
    ];

    /// <summary>Proves sizes below the first 1024 boundary are whole bytes.</summary>
    [TestMethod]
    public void FormatSizeWhenBelowOneKibibyteShowsWholeBytes()
    {
        Assert.AreEqual("0 B", Size(0));
        Assert.AreEqual("812 B", Size(812));
        Assert.AreEqual("1023 B", Size(1023));
    }

    /// <summary>Proves each 1024 boundary starts the next unit with one decimal.</summary>
    [TestMethod]
    public void FormatSizeWhenAtEachBoundaryStartsNextUnit()
    {
        Assert.AreEqual("1.0 KB", Size(Kibibyte));
        Assert.AreEqual("1.0 MB", Size(Mebibyte));
        Assert.AreEqual("1.0 GB", Size(Gibibyte));
        Assert.AreEqual("1.0 TB", Size(Tebibyte));
        Assert.AreEqual("12.0 MB", Size(12 * Mebibyte));
        Assert.AreEqual("1.5 GB", Size(3 * Gibibyte / 2));
    }

    /// <summary>
    /// Proves a value that rounds up to 1024 of a unit moves to the next unit instead of widening
    /// the text, and the last value that does not round up stays in its unit.
    /// </summary>
    [TestMethod]
    public void FormatSizeWhenRoundingReachesBoundaryMovesToNextUnit()
    {
        Assert.AreEqual("1023.9 KB", Size(1048524));
        Assert.AreEqual("1.0 MB", Size(1048525));
        Assert.AreEqual("1.0 MB", Size(Mebibyte - 1));
        Assert.AreEqual("1.0 GB", Size(Gibibyte - 1));
        Assert.AreEqual("1.0 TB", Size(Tebibyte - 1));
    }

    /// <summary>Proves one decimal rounds half away from zero rather than to even.</summary>
    [TestMethod]
    public void FormatSizeWhenValueIsAtMidpointRoundsAwayFromZero()
    {
        Assert.AreEqual("1.3 KB", Size(1280));
        Assert.AreEqual("2.3 KB", Size(2304));
        Assert.AreEqual("999.9 KB", Size(1023948));
        Assert.AreEqual("1000.0 KB", Size(1023949));
    }

    /// <summary>Proves the top unit saturates so the text never exceeds 9 glyphs.</summary>
    [TestMethod]
    public void FormatSizeWhenBeyondTopUnitSaturates()
    {
        Assert.AreEqual("1023.9 TB", Size(Tebibyte * 10239 / 10));
        Assert.AreEqual("1023.9 TB", Size((1024 * Tebibyte) - 1));
        Assert.AreEqual("1023.9 TB", Size(1024 * Tebibyte));
        Assert.AreEqual("1023.9 TB", Size(long.MaxValue));
        Assert.IsLessThanOrEqualTo(9, Size(long.MaxValue).Length);
    }

    /// <summary>Proves an unknown size shows the unknown glyph.</summary>
    [TestMethod]
    public void FormatSizeWhenUnknownShowsUnknownGlyph()
    {
        string text = EntryMetadataFormatter.FormatSize(EntrySize.Unknown, TestMetadataFormats.Utc);

        Assert.AreEqual("\u2014", text);
    }

    /// <summary>Proves sizes use the localized unit labels and composite formats.</summary>
    [TestMethod]
    public void FormatSizeWhenResourcesDifferUsesLocalizedFormats()
    {
        EntryMetadataFormat format = EntryMetadataFormat.Create(TimeZoneInfo.Utc, LocalizeAlternative);

        Assert.AreEqual("[5|b]", EntryMetadataFormatter.FormatSize(EntrySize.Create(5), format));
        Assert.AreEqual("<1.5|k>", EntryMetadataFormatter.FormatSize(EntrySize.Create(1536), format));
        Assert.AreEqual("<1.0|m>", EntryMetadataFormatter.FormatSize(EntrySize.Create(Mebibyte), format));
        Assert.AreEqual("<1.0|g>", EntryMetadataFormatter.FormatSize(EntrySize.Create(Gibibyte), format));
        Assert.AreEqual("<1.0|t>", EntryMetadataFormatter.FormatSize(EntrySize.Create(Tebibyte), format));
        Assert.AreEqual("?", EntryMetadataFormatter.FormatSize(EntrySize.Unknown, format));
        Assert.AreEqual("?", EntryMetadataFormatter.FormatModified(EntryTimestamp.Unknown, format));
    }

    /// <summary>Proves a known time is shown in the supplied zone with a 24-hour invariant format.</summary>
    [TestMethod]
    public void FormatModifiedWhenKnownShowsTimeInSuppliedZone()
    {
        EntryTimestamp modified = EntryTimestamp.Create(new DateTimeOffset(2026, 10, 9, 14, 59, 30, TimeSpan.Zero));

        string ahead = EntryMetadataFormatter.FormatModified(modified, TestMetadataFormats.Create(TestMetadataFormats.PlusNine));
        string behind = EntryMetadataFormatter.FormatModified(modified, TestMetadataFormats.Create(TestMetadataFormats.MinusFiveThirty));
        string utc = EntryMetadataFormatter.FormatModified(modified, TestMetadataFormats.Utc);

        Assert.AreEqual("2026-10-09 23:59", ahead);
        Assert.AreEqual("2026-10-09 09:29", behind);
        Assert.AreEqual("2026-10-09 14:59", utc);
    }

    /// <summary>Proves the zone conversion moves the calendar date when it crosses midnight.</summary>
    [TestMethod]
    public void FormatModifiedWhenZoneCrossesMidnightShowsLocalDate()
    {
        EntryTimestamp modified = EntryTimestamp.Create(new DateTimeOffset(2026, 12, 31, 20, 5, 0, TimeSpan.Zero));

        string text = EntryMetadataFormatter.FormatModified(modified, TestMetadataFormats.Create(TestMetadataFormats.PlusNine));

        Assert.AreEqual("2027-01-01 05:05", text);
    }

    /// <summary>Proves an unknown time shows the unknown glyph.</summary>
    [TestMethod]
    public void FormatModifiedWhenUnknownShowsUnknownGlyph()
    {
        string text = EntryMetadataFormatter.FormatModified(EntryTimestamp.Unknown, TestMetadataFormats.Utc);

        Assert.AreEqual("\u2014", text);
    }

    /// <summary>Proves the format resolves each resource key exactly once.</summary>
    [TestMethod]
    public void CreateWhenCalledResolvesEveryMetadataResourceOnce()
    {
        List<string> keys = [];

        _ = EntryMetadataFormat.Create(TimeZoneInfo.Utc, key =>
        {
            keys.Add(key);
            return TestMetadataFormats.Localize(key);
        });

        CollectionAssert.AreEquivalent(MetadataResourceKeys, keys);
    }

    /// <summary>Proves every entry point rejects absent arguments.</summary>
    [TestMethod]
    public void FormatWhenArgumentIsNullThrowsArgumentNullException()
    {
        EntryMetadataFormat format = TestMetadataFormats.Utc;

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => EntryMetadataFormat.Create(null!, TestMetadataFormats.Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => EntryMetadataFormat.Create(TimeZoneInfo.Utc, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => EntryMetadataFormatter.FormatSize(null!, format));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => EntryMetadataFormatter.FormatSize(EntrySize.Unknown, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => EntryMetadataFormatter.FormatModified(null!, format));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => EntryMetadataFormatter.FormatModified(EntryTimestamp.Unknown, null!));
    }

    private static string Size(long bytes)
    {
        return EntryMetadataFormatter.FormatSize(EntrySize.Create(bytes), TestMetadataFormats.Utc);
    }

    private static string LocalizeAlternative(string key)
    {
        Dictionary<string, string> values = new()
        {
            ["PaneRowSizeUnitBytes"] = "b",
            ["PaneRowSizeUnitKilobytes"] = "k",
            ["PaneRowSizeUnitMegabytes"] = "m",
            ["PaneRowSizeUnitGigabytes"] = "g",
            ["PaneRowSizeUnitTerabytes"] = "t",
            ["PaneRowSizeFormatInteger"] = "[{0}|{1}]",
            ["PaneRowSizeFormatDecimal"] = "<{0:0.0}|{1}>",
            ["PaneRowModifiedFormat"] = "HH:mm",
            ["PaneRowMetadataUnknown"] = "?",
        };
        return values[key];
    }
}
