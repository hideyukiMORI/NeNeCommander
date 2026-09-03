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

    /// <summary>Gets the snapshot of one side.</summary>
    /// <param name="side">Requested side.</param>
    /// <returns>The pane snapshot of that side.</returns>
    public PaneSnapshot Of(PaneSide side)
    {
        ArgumentNullException.ThrowIfNull(side);
        return side == PaneSide.Left ? Left : Right;
    }
}
