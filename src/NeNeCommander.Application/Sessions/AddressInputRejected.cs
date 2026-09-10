using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents one rejected raw address that remains open for correction.</summary>
public sealed record AddressInputRejected : AddressEditorState
{
    internal AddressInputRejected(
        PaneSide side,
        FileSystemPath originalLocation,
        string rawText,
        PathParseFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(originalLocation);
        ArgumentNullException.ThrowIfNull(rawText);
        ArgumentNullException.ThrowIfNull(failure);
        Side = side;
        OriginalLocation = originalLocation;
        RawText = rawText;
        Failure = failure;
    }

    /// <summary>Gets the pane whose address remains open.</summary>
    public PaneSide Side { get; }

    /// <summary>Gets the canonical location shown when editing began.</summary>
    public FileSystemPath OriginalLocation { get; }

    /// <summary>Gets the exact rejected submission without repair or normalization.</summary>
    public string RawText { get; }

    /// <summary>Gets the closed parser rejection reason.</summary>
    public PathParseFailureKind Failure { get; }
}
