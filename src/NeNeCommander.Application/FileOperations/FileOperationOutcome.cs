using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a complete typed file-operation result, including every completed side effect.
/// </summary>
public sealed record FileOperationOutcome
{
    private readonly FileOperationEffectSummary _summary;
    private readonly TransferConflictPause? _pause;

    private FileOperationOutcome(
        FileOperationCompletionKind completion,
        FileOperationEffectSummary summary,
        FileOperationFailureKind? failure,
        TransferConflictPause? pause)
    {
        Completion = completion;
        _summary = summary;
        Failure = failure;
        _pause = pause;
    }

    /// <summary>Gets the closed overall completion state.</summary>
    public FileOperationCompletionKind Completion { get; }

    /// <summary>Gets the exact ordered side effects completed before return.</summary>
    public IReadOnlyList<FileOperationEffect> Effects => _summary.Effects;

    /// <summary>Gets sources deliberately left untransferred by explicit Skip decisions.</summary>
    public IReadOnlyList<FileSystemPath> NotTransferred => _summary.NotTransferred;

    /// <summary>Gets the normalized failure, or absence for success and cancellation.</summary>
    public FileOperationFailureKind? Failure { get; }

    /// <summary>Gets the complete collision set awaiting a decision, or absence for a completed outcome.</summary>
    public ConflictSet? Conflicts => _pause?.Conflicts;

    /// <summary>Gets the operation-owned frozen continuation paired with <see cref="Conflicts"/>.</summary>
    public TransferContinuation? Continuation => _pause?.Continuation;

    internal static FileOperationOutcome Succeeded(IReadOnlyList<FileOperationEffect> effects)
    {
        return Create(FileOperationCompletionKind.Succeeded, FileOperationEffectSummary.Create(effects, []), null, null);
    }

    internal static FileOperationOutcome Cancelled(IReadOnlyList<FileOperationEffect> effects)
    {
        return Create(FileOperationCompletionKind.Cancelled, FileOperationEffectSummary.Create(effects, []), null, null);
    }

    internal static FileOperationOutcome Cancelled(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred)
    {
        return Create(FileOperationCompletionKind.Cancelled, FileOperationEffectSummary.Create(effects, notTransferred), null, null);
    }

    internal static FileOperationOutcome Failed(
        IReadOnlyList<FileOperationEffect> effects,
        FileOperationFailureKind failure)
    {
        FileOperationCompletionKind completion = effects.Count == 0
            ? FileOperationCompletionKind.Rejected
            : FileOperationCompletionKind.PartiallyCompleted;
        return Create(completion, FileOperationEffectSummary.Create(effects, []), failure, null);
    }

    internal static FileOperationOutcome Failed(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred,
        FileOperationFailureKind failure)
    {
        FileOperationCompletionKind completion = effects.Count == 0 && notTransferred.Count == 0
            ? FileOperationCompletionKind.Rejected
            : FileOperationCompletionKind.PartiallyCompleted;
        return Create(completion, FileOperationEffectSummary.Create(effects, notTransferred), failure, null);
    }

    internal static FileOperationOutcome Succeeded(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred)
    {
        return Create(FileOperationCompletionKind.Succeeded, FileOperationEffectSummary.Create(effects, notTransferred), null, null);
    }

    internal static FileOperationOutcome AwaitingConflict(
        ConflictSet conflicts,
        TransferContinuation continuation)
    {
        return Create(FileOperationCompletionKind.Rejected, FileOperationEffectSummary.Create([], []), FileOperationFailureKind.Conflict, TransferConflictPause.Create(conflicts, continuation));
    }

    private static FileOperationOutcome Create(
        FileOperationCompletionKind completion,
        FileOperationEffectSummary summary,
        FileOperationFailureKind? failure,
        TransferConflictPause? pause)
    {
        return new FileOperationOutcome(completion, summary, failure, pause);
    }
}
