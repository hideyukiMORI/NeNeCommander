using System;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Settings;

/// <summary>Holds both panes' last accepted sort orders in the complete settings value.</summary>
public sealed record PaneSortPreferences
{
    private PaneSortPreferences(PaneSortOrder left, PaneSortOrder right)
    {
        Left = left;
        Right = right;
    }

    /// <summary>Gets name ascending for both panes.</summary>
    public static PaneSortPreferences Default { get; } = Create(PaneSortOrder.Default, PaneSortOrder.Default);

    /// <summary>Gets the left pane's next-launch order.</summary>
    public PaneSortOrder Left { get; }

    /// <summary>Gets the right pane's next-launch order.</summary>
    public PaneSortOrder Right { get; }

    /// <summary>Creates one complete immutable preference pair.</summary>
    public static PaneSortPreferences Create(PaneSortOrder left, PaneSortOrder right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new PaneSortPreferences(left, right);
    }
}
