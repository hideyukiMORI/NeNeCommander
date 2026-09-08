using System;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Projects address-editor transitions without repeating native text or focus effects.</summary>
public static class AddressEditorPresenter
{
    /// <summary>Projects an address editor state for its first render.</summary>
    public static AddressEditorPresentation Present(AddressEditorState state)
    {
        return Present(state, null);
    }

    internal static AddressEditorPresentation Present(
        AddressEditorState state,
        AddressEditorPresentation? previous)
    {
        ArgumentNullException.ThrowIfNull(state);
        return previous is not null && ReferenceEquals(previous.SourceState, state)
            ? previous
            : state switch
            {
                AddressEditing editing => new AddressEditorPresentation(
                    state,
                    editing.Side,
                    editing.OriginalLocation.CanonicalText,
                    null,
                    null),
                AddressInputRejected rejected => new AddressEditorPresentation(
                    state,
                    rejected.Side,
                    rejected.RawText,
                    PaneStatus.InvalidAddress,
                    null),
                AddressEditorClosed closed => new AddressEditorPresentation(
                    state,
                    null,
                    null,
                    null,
                    closed.FileListFocusSide),
                _ => throw new InvalidOperationException("Unsupported address editor state."),
            };
    }
}
