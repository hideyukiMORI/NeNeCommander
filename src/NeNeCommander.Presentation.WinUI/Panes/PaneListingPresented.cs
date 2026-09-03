using System.Collections.Generic;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents a successfully read listing projected onto one pane.</summary>
public sealed record PaneListingPresented : PanePresentation
{
    internal PaneListingPresented(DirectoryListing listing, PaneStatus status, DirectoryEntry? focusEntry)
    {
        Listing = listing;
        Status = status;
        FocusEntry = focusEntry;
    }

    /// <summary>Gets the validated listing behind the rows.</summary>
    public DirectoryListing Listing { get; }

    /// <inheritdoc />
    public override PaneStatus Status { get; }

    /// <inheritdoc />
    public override IReadOnlyList<DirectoryEntry> Entries => Listing.Entries;

    /// <inheritdoc />
    public override DirectoryEntry? FocusEntry { get; }
}
