namespace NeNeCommander.Application.Panes;

/// <summary>
/// Receives the dual-pane snapshot each time a running operation's progress changes, so a host
/// can render intermediate state before <see cref="DualPaneSession.HandleAsync"/> completes.
/// </summary>
public interface IDualPaneProgressObserver
{
    /// <summary>Reports the snapshot that carries the updated <see cref="OperationRunning.Progress"/>.</summary>
    /// <param name="snapshot">Current dual-pane snapshot.</param>
    public void OperationProgressed(DualPaneSnapshot snapshot);
}
