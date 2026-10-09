using System;
using System.Globalization;
using System.Text;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Turns the size and modification time a provider reported into the fixed-width texts a row
/// shows. Sizes below 1024 bytes are whole bytes; larger sizes use 1024 boundaries with one
/// decimal rounded half away from zero, move to the next unit once rounding reaches 1024, and
/// saturate at the top unit so the text never exceeds 9 glyphs. Times are shown in the supplied
/// zone. Unknown values show the one unknown glyph. Every result is a pure function of its
/// arguments. The arrangement of number and unit is part of the column design rather than of a
/// language, so it is a literal format here and resources supply only the unit words.
/// </summary>
public static class EntryMetadataFormatter
{
    private const decimal UnitStep = 1024m;
    private const decimal TopUnitCeiling = 1023.9m;
    private const int DecimalDigits = 1;
    private static readonly CompositeFormat SizeIntegerFormat = CompositeFormat.Parse("{0} {1}");
    private static readonly CompositeFormat SizeDecimalFormat = CompositeFormat.Parse("{0:0.0} {1}");

    /// <summary>Formats the size of a file entry.</summary>
    /// <param name="size">Closed size the provider reported.</param>
    /// <param name="format">Localized formats resolved once by the host.</param>
    /// <returns>The size text, or the unknown glyph when no byte count was reported.</returns>
    public static string FormatSize(EntrySize size, EntryMetadataFormat format)
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(format);
        return size is KnownEntrySize known ? FormatBytes(known.Bytes, format) : format.UnknownText;
    }

    /// <summary>Formats a modification time in the zone the format carries.</summary>
    /// <param name="modified">Closed modification time the provider reported.</param>
    /// <param name="format">Localized formats and zone resolved once by the host.</param>
    /// <returns>The local time text, or the unknown glyph when no time was reported.</returns>
    public static string FormatModified(EntryTimestamp modified, EntryMetadataFormat format)
    {
        ArgumentNullException.ThrowIfNull(modified);
        ArgumentNullException.ThrowIfNull(format);
        return modified is KnownEntryTimestamp known
            ? TimeZoneInfo.ConvertTime(known.Utc, format.Zone).ToString(format.ModifiedFormat, CultureInfo.InvariantCulture)
            : format.UnknownText;
    }

    private static string FormatBytes(long bytes, EntryMetadataFormat format)
    {
        if (bytes < UnitStep)
        {
            return string.Format(CultureInfo.InvariantCulture, SizeIntegerFormat, bytes, format.SizeUnits[0]);
        }

        int topUnit = format.SizeUnits.Count - 1;
        int unit = 1;
        decimal value = bytes / UnitStep;
        while (unit < topUnit && Round(value) >= UnitStep)
        {
            value /= UnitStep;
            unit++;
        }
        return string.Format(
            CultureInfo.InvariantCulture,
            SizeDecimalFormat,
            Math.Min(Round(value), TopUnitCeiling),
            format.SizeUnits[unit]);
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, DecimalDigits, MidpointRounding.AwayFromZero);
    }
}
