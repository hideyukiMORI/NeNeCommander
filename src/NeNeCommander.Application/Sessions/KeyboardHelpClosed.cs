using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents a closed keyboard help surface and its optional one-time file-list focus request.</summary>
public sealed record KeyboardHelpClosed : KeyboardHelpState
{
    private protected override void RestrictDerivation()
    {
    }

    internal KeyboardHelpClosed(PaneSide? fileListFocusSide)
    {
        FileListFocusSide = fileListFocusSide;
    }

    /// <summary>Gets the file list focused after a qualified close, or absence when the surface never opened.</summary>
    public PaneSide? FileListFocusSide { get; }
}
