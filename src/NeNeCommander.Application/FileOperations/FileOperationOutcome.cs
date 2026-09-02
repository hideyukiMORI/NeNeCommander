using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents a complete typed file-operation result, including every completed side effect.
/// </summary>
public sealed record FileOperationOutcome
{
    private readonly ReadOnlyCollection<FileOperationEffect> _effects;

    private FileOperationOutcome(
        FileOperationCompletionKind completion,
        ReadOnlyCollection<FileOperationEffect> effects,
        FileOperationFailureKind? failure)
    {
        Completion = completion;
        _effects = effects;
        Failure = failure;
    }

    /// <summary>Gets the closed overall completion state.</summary>
    public FileOperationCompletionKind Completion { get; }

    /// <summary>Gets the exact ordered side effects completed before return.</summary>
    public IReadOnlyList<FileOperationEffect> Effects => _effects;

    /// <summary>Gets the normalized failure, or absence for success and cancellation.</summary>
    public FileOperationFailureKind? Failure { get; }

    internal static FileOperationOutcome Succeeded(IReadOnlyList<FileOperationEffect> effects)
    {
        return Create(FileOperationCompletionKind.Succeeded, effects, null);
    }

    internal static FileOperationOutcome Cancelled(IReadOnlyList<FileOperationEffect> effects)
    {
        return Create(FileOperationCompletionKind.Cancelled, effects, null);
    }

    internal static FileOperationOutcome Failed(
        IReadOnlyList<FileOperationEffect> effects,
        FileOperationFailureKind failure)
    {
        FileOperationCompletionKind completion = effects.Count == 0
            ? FileOperationCompletionKind.Rejected
            : FileOperationCompletionKind.PartiallyCompleted;
        return Create(completion, effects, failure);
    }

    private static FileOperationOutcome Create(
        FileOperationCompletionKind completion,
        IReadOnlyList<FileOperationEffect> effects,
        FileOperationFailureKind? failure)
    {
        List<FileOperationEffect> ownedEffects = [.. effects];
        return new FileOperationOutcome(completion, ownedEffects.AsReadOnly(), failure);
    }
}
