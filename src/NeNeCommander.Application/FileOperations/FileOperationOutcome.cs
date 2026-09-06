using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a complete typed file-operation result, including every completed side effect.
/// </summary>
public sealed record FileOperationOutcome
{
    private readonly ReadOnlyCollection<FileOperationEffect> _effects;
    private readonly ReadOnlyCollection<FileSystemPath> _notTransferred;

    private FileOperationOutcome(
        FileOperationCompletionKind completion,
        ReadOnlyCollection<FileOperationEffect> effects,
        ReadOnlyCollection<FileSystemPath> notTransferred,
        FileOperationFailureKind? failure,
        ConflictSet? conflicts,
        TransferContinuation? continuation)
    {
        Completion = completion;
        _effects = effects;
        _notTransferred = notTransferred;
        Failure = failure;
        Conflicts = conflicts;
        Continuation = continuation;
    }

    /// <summary>Gets the closed overall completion state.</summary>
    public FileOperationCompletionKind Completion { get; }

    /// <summary>Gets the exact ordered side effects completed before return.</summary>
    public IReadOnlyList<FileOperationEffect> Effects => _effects;

    /// <summary>Gets sources deliberately left untransferred by explicit Skip decisions.</summary>
    public IReadOnlyList<FileSystemPath> NotTransferred => _notTransferred;

    /// <summary>Gets the normalized failure, or absence for success and cancellation.</summary>
    public FileOperationFailureKind? Failure { get; }

    /// <summary>Gets the complete collision set awaiting a decision, or absence for a completed outcome.</summary>
    public ConflictSet? Conflicts { get; }

    /// <summary>Gets the operation-owned frozen continuation paired with <see cref="Conflicts"/>.</summary>
    public TransferContinuation? Continuation { get; }

    internal static FileOperationOutcome Succeeded(IReadOnlyList<FileOperationEffect> effects)
    {
        return Create(FileOperationCompletionKind.Succeeded, effects, [], null, null, null);
    }

    internal static FileOperationOutcome Cancelled(IReadOnlyList<FileOperationEffect> effects)
    {
        return Create(FileOperationCompletionKind.Cancelled, effects, [], null, null, null);
    }

    internal static FileOperationOutcome Cancelled(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred)
    {
        return Create(FileOperationCompletionKind.Cancelled, effects, notTransferred, null, null, null);
    }

    internal static FileOperationOutcome Failed(
        IReadOnlyList<FileOperationEffect> effects,
        FileOperationFailureKind failure)
    {
        FileOperationCompletionKind completion = effects.Count == 0
            ? FileOperationCompletionKind.Rejected
            : FileOperationCompletionKind.PartiallyCompleted;
        return Create(completion, effects, [], failure, null, null);
    }

    internal static FileOperationOutcome Failed(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred,
        FileOperationFailureKind failure)
    {
        FileOperationCompletionKind completion = effects.Count == 0 && notTransferred.Count == 0
            ? FileOperationCompletionKind.Rejected
            : FileOperationCompletionKind.PartiallyCompleted;
        return Create(completion, effects, notTransferred, failure, null, null);
    }

    internal static FileOperationOutcome Succeeded(
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred)
    {
        return Create(FileOperationCompletionKind.Succeeded, effects, notTransferred, null, null, null);
    }

    internal static FileOperationOutcome AwaitingConflict(
        ConflictSet conflicts,
        TransferContinuation continuation)
    {
        return Create(FileOperationCompletionKind.Rejected, [], [], FileOperationFailureKind.Conflict, conflicts, continuation);
    }

    private static FileOperationOutcome Create(
        FileOperationCompletionKind completion,
        IReadOnlyList<FileOperationEffect> effects,
        IReadOnlyList<FileSystemPath> notTransferred,
        FileOperationFailureKind? failure,
        ConflictSet? conflicts,
        TransferContinuation? continuation)
    {
        List<FileOperationEffect> ownedEffects = [.. effects];
        List<FileSystemPath> ownedNotTransferred = [.. notTransferred];
        return new FileOperationOutcome(
            completion,
            ownedEffects.AsReadOnly(),
            ownedNotTransferred.AsReadOnly(),
            failure,
            conflicts,
            continuation);
    }
}
