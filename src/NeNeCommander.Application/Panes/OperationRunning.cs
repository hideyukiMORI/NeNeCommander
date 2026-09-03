namespace NeNeCommander.Application.Panes;

/// <summary>Represents a move running through the gateway; every pane intent is frozen meanwhile.</summary>
public sealed record OperationRunning : OperationActivity
{
    internal OperationRunning()
    {
    }
}
