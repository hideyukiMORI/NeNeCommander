using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Retains only the accepted first tap's owned listing and entry across row projections.</summary>
internal sealed record PanePointerCandidate
{
    internal PanePointerCandidate(PaneSide side, DirectoryListing listing, DirectoryEntry entry)
    {
        Side = side;
        Listing = listing;
        Entry = entry;
    }

    internal PaneSide Side { get; }

    internal DirectoryListing Listing { get; }

    internal DirectoryEntry Entry { get; }
}
