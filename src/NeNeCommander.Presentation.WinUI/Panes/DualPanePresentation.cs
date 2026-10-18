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
    private readonly PanePairPresentation _panes;
    private readonly OperationBarPresentation _operationBar;
    private readonly OperationInteractionPresentation _interaction;

    internal DualPanePresentation(
        PanePairPresentation panes,
        OperationBarPresentation operationBar,
        OperationInteractionPresentation interaction)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(operationBar);
        ArgumentNullException.ThrowIfNull(interaction);
        _panes = panes;
        _operationBar = operationBar;
        _interaction = interaction;
    }

    /// <summary>Gets the left pane presentation.</summary>
    public PanePresentation Left => _panes.Left;

    /// <summary>Gets the left pane frame.</summary>
    public PaneFrame LeftFrame => _panes.LeftFrame;

    /// <summary>Gets the right pane presentation.</summary>
    public PanePresentation Right => _panes.Right;

    /// <summary>Gets the right pane frame.</summary>
    public PaneFrame RightFrame => _panes.RightFrame;

    /// <summary>Gets the side whose file list should hold keyboard focus.</summary>
    public PaneSide ActiveSide => _panes.ActiveSide;

    /// <summary>Gets the status of the file-operation activity.</summary>
    public OperationStatus OperationStatus => _operationBar.Status;

    /// <summary>Gets the deletion-confirmation attached identifier while pending, otherwise an empty value that permits the framework's element-name fallback.</summary>
    public string DeletionConfirmationAutomationId => OperationStatus == OperationStatus.DeleteAwaitingConfirmation ? "DeletionConfirmation" : string.Empty;

    /// <summary>Gets the closed numeric detail shown beside the status: none, a confirmation's item count, or running progress.</summary>
    public OperationDetail Detail => _operationBar.Detail;

    /// <summary>Gets the closed tone the operation bar shows for the current activity.</summary>
    public OperationBarTone Tone => _operationBar.Tone;

    /// <summary>Gets the ordered shortcut hints the operation bar shows, generated from the canonical key map.</summary>
    public IReadOnlyList<KeyHint> KeyHints => _operationBar.KeyHints;

    /// <summary>Gets whether the host shows and focuses the name editor and, when it does, the text it starts from.</summary>
    public NameEntryPresentation NameEntry => _interaction.NameEntry;

    /// <summary>Gets the transfer conflict modal presentation.</summary>
    public ConflictModalPresentation ConflictModal => _interaction.ConflictModal;

    /// <summary>
    /// Gets the keyboard context the operation state imposes on the file list: modal while a
    /// confirmation or a name entry is pending, otherwise the file-list context.
    /// </summary>
    public KeyboardContext InputContext => _interaction.InputContext;
}
