using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the session-owned window-adjustment mode state.</summary>
public abstract record WindowAdjustmentState
{
    /// <summary>Gets the closed state with no pending focus effect.</summary>
    public static WindowAdjustmentState Closed { get; } = new WindowAdjustmentClosed(null);

    private protected WindowAdjustmentState()
    {
    }

    internal static WindowAdjustmentState CloseFocusing(PaneSide side)
    {
        return new WindowAdjustmentClosed(side);
    }
}
