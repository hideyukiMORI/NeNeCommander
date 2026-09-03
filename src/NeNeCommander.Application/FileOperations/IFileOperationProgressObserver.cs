namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Receives the gateway's progress once per completed source. Implementations run on the
/// gateway's continuation and must not mutate the filesystem or start another operation.
/// </summary>
public interface IFileOperationProgressObserver
{
    /// <summary>Reports that one more source completed every step of the running request.</summary>
    /// <param name="progress">Completed and total source counts.</param>
    public void Report(FileOperationProgress progress);
}
