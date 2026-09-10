using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents a closed palette and its optional one-time file-list focus request.</summary>
public sealed record CommandPaletteClosed : CommandPaletteState
{
    internal CommandPaletteClosed(PaneSide? fileListFocusSide)
    {
        FileListFocusSide = fileListFocusSide;
    }

    /// <summary>Gets the file list focused after valid cancellation, or absence after execution.</summary>
    public PaneSide? FileListFocusSide { get; }
}
