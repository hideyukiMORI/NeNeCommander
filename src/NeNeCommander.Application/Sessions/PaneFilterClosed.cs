using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Closes filter editing with an optional one-time file-list focus effect.</summary>
public sealed record PaneFilterClosed : PaneFilterState
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneFilterClosed(PaneSide? fileListFocusSide)
    {
        FileListFocusSide = fileListFocusSide;
    }

    /// <summary>Gets the file list to focus once, or absence when native focus must remain untouched.</summary>
    public PaneSide? FileListFocusSide { get; }
}
