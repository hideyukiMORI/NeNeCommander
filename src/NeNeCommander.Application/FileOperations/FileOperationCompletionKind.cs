namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Identifies one closed overall file-operation completion state.
/// </summary>
public abstract record FileOperationCompletionKind
{
    /// <summary>Gets the state in which every requested effect completed.</summary>
    public static FileOperationCompletionKind Succeeded { get; } = new SucceededCompletion();

    /// <summary>Gets the state in which cancellation stopped new work.</summary>
    public static FileOperationCompletionKind Cancelled { get; } = new CancelledCompletion();

    /// <summary>Gets the state rejected before any side effect.</summary>
    public static FileOperationCompletionKind Rejected { get; } = new RejectedCompletion();

    /// <summary>Gets the state with reported side effects followed by a failure.</summary>
    public static FileOperationCompletionKind PartiallyCompleted { get; } = new PartiallyCompletedCompletion();

    private FileOperationCompletionKind()
    {
    }

    private sealed record SucceededCompletion : FileOperationCompletionKind;
    private sealed record CancelledCompletion : FileOperationCompletionKind;
    private sealed record RejectedCompletion : FileOperationCompletionKind;
    private sealed record PartiallyCompletedCompletion : FileOperationCompletionKind;
}
