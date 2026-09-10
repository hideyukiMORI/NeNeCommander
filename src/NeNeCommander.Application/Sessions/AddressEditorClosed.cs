using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents a closed address editor and its optional one-time file-list focus request.</summary>
public sealed record AddressEditorClosed : AddressEditorState
{
    internal AddressEditorClosed(PaneSide? fileListFocusSide)
    {
        FileListFocusSide = fileListFocusSide;
    }

    /// <summary>Gets the file list to focus once, or absence when focus must remain where it moved.</summary>
    public PaneSide? FileListFocusSide { get; }
}
