using System;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents the render action attached to one address-editor state transition.</summary>
public sealed record AddressEditorPresentation
{
    internal AddressEditorPresentation(
        AddressEditorState sourceState,
        PaneSide? editingSide,
        string? replacementText,
        PaneStatus? status,
        PaneSide? fileListFocusSide)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        SourceState = sourceState;
        EditingSide = editingSide;
        ReplacementText = replacementText;
        Status = status;
        FileListFocusSide = fileListFocusSide;
        SelectAll = sourceState is AddressEditing;
    }

    /// <summary>Gets the exact application state that produced this presentation.</summary>
    public AddressEditorState SourceState { get; }

    /// <summary>Gets the address control that remains open, or absence when editing is closed.</summary>
    public PaneSide? EditingSide { get; }

    /// <summary>Gets text to assign once for this transition, or absence when native text is preserved.</summary>
    public string? ReplacementText { get; }

    /// <summary>Gets the status overriding the editing pane, or absence for its ordinary pane status.</summary>
    public PaneStatus? Status { get; }

    /// <summary>Gets the file list to focus once, or absence when focus must remain unchanged.</summary>
    public PaneSide? FileListFocusSide { get; }

    /// <summary>Gets whether the replacement text is selected once after address focus.</summary>
    public bool SelectAll { get; }
}
