using System;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the complete immutable state of both panes, which one is active, and the
/// file-operation activity that spans them.
/// </summary>
public sealed record DualPaneSnapshot
{
    internal DualPaneSnapshot(PaneSnapshot left, PaneSnapshot right, PaneSide activeSide, OperationActivity operation)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(activeSide);
        ArgumentNullException.ThrowIfNull(operation);
        Left = left;
        Right = right;
        ActiveSide = activeSide;
        Operation = operation;
    }

    /// <summary>Gets the left pane snapshot.</summary>
    public PaneSnapshot Left { get; }

    /// <summary>Gets the right pane snapshot.</summary>
    public PaneSnapshot Right { get; }

    /// <summary>Gets the side that receives navigation and file-operation intents.</summary>
    public PaneSide ActiveSide { get; }

    /// <summary>Gets the file-operation activity.</summary>
    public OperationActivity Operation { get; }

    /// <summary>Gets whether this snapshot admits opening its listed active location in the opposite pane.</summary>
    public bool CanOpenCurrentLocationInOtherPane =>
        Of(ActiveSide).Content is PaneContentListed &&
        !HasExternalWork &&
        Operation is not (OperationRunning or OperationAwaitingConfirmation or OperationAwaitingName or OperationAwaitingConflict);
    internal bool HasExternalWork => Left.Activity is PaneLoading or PaneLaunching || Right.Activity is PaneLoading or PaneLaunching;

    internal bool HasReadInProgress => Left.Activity is PaneLoading || Right.Activity is PaneLoading;

    /// <summary>Gets the snapshot of one side.</summary>
    /// <param name="side">Requested side.</param>
    /// <returns>The pane snapshot of that side.</returns>
    public PaneSnapshot Of(PaneSide side)
    {
        ArgumentNullException.ThrowIfNull(side);
        return side == PaneSide.Left ? Left : Right;
    }
}
