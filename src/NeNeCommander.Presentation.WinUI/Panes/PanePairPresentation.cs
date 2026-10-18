using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Captures both projected panes and the Application-owned active side.</summary>
internal sealed record PanePairPresentation
{
    private PanePairPresentation(PanePresentation left, PanePresentation right, PaneSide activeSide)
    {
        Left = left;
        Right = right;
        ActiveSide = activeSide;
    }

    internal PanePresentation Left { get; }
    internal PanePresentation Right { get; }
    internal PaneSide ActiveSide { get; }
    internal PaneFrame LeftFrame => Left.SourceFrame;
    internal PaneFrame RightFrame => Right.SourceFrame;

    internal static PanePairPresentation Create(PanePresentation left, PanePresentation right, PaneSide activeSide)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(activeSide);
        return new PanePairPresentation(left, right, activeSide);
    }
}
