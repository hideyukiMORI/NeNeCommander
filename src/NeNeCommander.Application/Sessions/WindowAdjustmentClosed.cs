using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents a closed mode and its optional one-time file-list focus request.</summary>
public sealed record WindowAdjustmentClosed : WindowAdjustmentState
{
    internal WindowAdjustmentClosed(PaneSide? fileListFocusSide)
    {
        FileListFocusSide = fileListFocusSide;
    }

    /// <summary>Gets the file list focused after a qualified leave, or absence when the mode never opened.</summary>
    public PaneSide? FileListFocusSide { get; }
}
