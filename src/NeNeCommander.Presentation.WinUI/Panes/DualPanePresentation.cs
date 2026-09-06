using System;
using System.Collections.Generic;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the render-ready projection of both panes, their activation frames, the
/// file-operation status, and the keyboard context the operation state imposes.
/// </summary>
public sealed record DualPanePresentation
{
    internal DualPanePresentation(
        PanePresentation left,
        PaneFrame leftFrame,
        PanePresentation right,
        PaneFrame rightFrame,
        PaneSide activeSide,
        OperationStatus operationStatus,
        OperationDetail detail,
        OperationBarTone tone,
        IReadOnlyList<KeyHint> keyHints,
        NameEntryPresentation nameEntry,
        ConflictModalPresentation conflictModal,
        KeyboardContext inputContext)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(leftFrame);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(rightFrame);
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(operationStatus);
        ArgumentNullException.ThrowIfNull(detail);
        ArgumentNullException.ThrowIfNull(tone);
        ArgumentNullException.ThrowIfNull(keyHints);
        ArgumentNullException.ThrowIfNull(nameEntry);
        ArgumentNullException.ThrowIfNull(conflictModal);
        ArgumentNullException.ThrowIfNull(inputContext);
        Left = left;
        LeftFrame = leftFrame;
        Right = right;
        RightFrame = rightFrame;
        ActiveSide = activeSide;
        OperationStatus = operationStatus;
        Detail = detail;
        Tone = tone;
        KeyHints = keyHints;
        NameEntry = nameEntry;
        ConflictModal = conflictModal;
        InputContext = inputContext;
    }

    /// <summary>Gets the left pane presentation.</summary>
    public PanePresentation Left { get; }

    /// <summary>Gets the left pane frame.</summary>
    public PaneFrame LeftFrame { get; }

    /// <summary>Gets the right pane presentation.</summary>
    public PanePresentation Right { get; }

    /// <summary>Gets the right pane frame.</summary>
    public PaneFrame RightFrame { get; }

    /// <summary>Gets the side whose file list should hold keyboard focus.</summary>
    public PaneSide ActiveSide { get; }

    /// <summary>Gets the status of the file-operation activity.</summary>
    public OperationStatus OperationStatus { get; }

    /// <summary>Gets the closed numeric detail shown beside the status: none, a confirmation's item count, or running progress.</summary>
    public OperationDetail Detail { get; }

    /// <summary>Gets the closed tone the operation bar shows for the current activity.</summary>
    public OperationBarTone Tone { get; }

    /// <summary>Gets the ordered shortcut hints the operation bar shows, generated from the canonical key map.</summary>
    public IReadOnlyList<KeyHint> KeyHints { get; }

    /// <summary>Gets whether the host shows and focuses the name editor and, when it does, the text it starts from.</summary>
    public NameEntryPresentation NameEntry { get; }

    /// <summary>Gets the transfer conflict modal presentation.</summary>
    public ConflictModalPresentation ConflictModal { get; }

    /// <summary>
    /// Gets the keyboard context the operation state imposes on the file list: modal while a
    /// confirmation or a name entry is pending, otherwise the file-list context.
    /// </summary>
    public KeyboardContext InputContext { get; }
}
