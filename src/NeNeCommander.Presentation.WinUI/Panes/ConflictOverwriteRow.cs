using System;
using System.Globalization;
using System.Text;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Shows one exact source and target pair with the original file kind, size and modification time.</summary>
public sealed record ConflictOverwriteRow
{
    internal ConflictOverwriteRow(TransferOverwriteEvidence evidence, EntryMetadataFormat metadataFormat, Func<string, string> localize)
    {
        SourceText = FormatFile(evidence.Source, metadataFormat, localize("ConflictOverwriteSourceDetails"));
        TargetText = FormatFile(evidence.Target, metadataFormat, localize("ConflictOverwriteTargetDetails"));
    }

    /// <summary>Gets the full localized original source details.</summary>
    public string SourceText { get; }
    /// <summary>Gets the full localized original target details.</summary>
    public string TargetText { get; }

    private static string FormatFile(TransferOverwriteFileObservation file, EntryMetadataFormat metadataFormat, string template)
    {
        return string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(template), file.Observation.Entry.Path.CanonicalText,
            EntryMetadataFormatter.FormatSize(file.Size, metadataFormat), EntryMetadataFormatter.FormatModified(file.Modified, metadataFormat));
    }
}
