using System;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>Represents a pane showing one listed location with its focus and selection state.</summary>
public sealed record PaneContentListed : PaneContent
{
    internal PaneContentListed(PaneState state, DirectoryListing listing)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(listing);
        State = state;
        Listing = listing;
    }

    /// <summary>Gets the immutable focus and selection state over the listing.</summary>
    public PaneState State { get; }

    /// <summary>Gets the listing whose entries the state addresses.</summary>
    public DirectoryListing Listing { get; }

    /// <summary>
    /// Finds the listing entry the focus item names, using filesystem identity so provider case
    /// rules decide. This is the sole way to reach the provider-reported name and kind of the
    /// focus item. An empty listing has no focus item and therefore no focused entry.
    /// </summary>
    /// <returns>The focused entry, or absence when the pane has no focus item.</returns>
    internal DirectoryEntry? FindFocusedEntry()
    {
        return Listing.Entries.FirstOrDefault(entry =>
            FileSystemPathIdentityComparer.Instance.Equals(entry.Path, State.FocusItem));
    }
}
