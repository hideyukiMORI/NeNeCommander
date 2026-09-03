using System;
using NeNeCommander.Application.Directories;

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
}
