using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the render-ready projection of both panes and their activation frames.
/// </summary>
public sealed record DualPanePresentation
{
    internal DualPanePresentation(
        PanePresentation left,
        PaneFrame leftFrame,
        PanePresentation right,
        PaneFrame rightFrame,
        PaneSide activeSide)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(leftFrame);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(rightFrame);
        ArgumentNullException.ThrowIfNull(activeSide);
        Left = left;
        LeftFrame = leftFrame;
        Right = right;
        RightFrame = rightFrame;
        ActiveSide = activeSide;
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
}
