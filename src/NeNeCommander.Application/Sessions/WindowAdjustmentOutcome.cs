namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed most recent result of the open window-adjustment mode. It is rendered,
/// never thrown, and it names only what was decided, never what the desktop then did.
/// </summary>
public abstract record WindowAdjustmentOutcome
{
    /// <summary>Gets the outcome of an open mode in which no action has been decided yet.</summary>
    public static WindowAdjustmentOutcome None { get; } = new NoOutcome();

    private protected WindowAdjustmentOutcome()
    {
    }

    private sealed record NoOutcome : WindowAdjustmentOutcome;
}
