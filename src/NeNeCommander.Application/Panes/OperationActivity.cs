namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the closed file-operation activity of the dual-pane session: nothing in progress,
/// an operation running, or the typed result of the most recent operation.
/// </summary>
public abstract record OperationActivity
{
    private protected OperationActivity()
    {
    }

    /// <summary>Gets the activity when no operation is running and none has been reported.</summary>
    public static OperationActivity Idle { get; } = new OperationIdle();

    private sealed record OperationIdle : OperationActivity;
}
