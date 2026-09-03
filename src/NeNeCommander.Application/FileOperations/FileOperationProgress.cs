using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents how many of a request's sources the gateway has completed. Every instance keeps
/// <c>0 ≤ Completed ≤ Total</c> with at least one source, because a validated request never has fewer.
/// </summary>
public sealed record FileOperationProgress
{
    private FileOperationProgress(int completed, int total)
    {
        Completed = completed;
        Total = total;
    }

    /// <summary>Gets the number of sources whose every step completed.</summary>
    public int Completed { get; }

    /// <summary>Gets the number of sources in the request.</summary>
    public int Total { get; }

    /// <summary>Creates a progress value that satisfies the invariant.</summary>
    /// <param name="completed">Sources completed so far.</param>
    /// <param name="total">Sources in the request.</param>
    /// <returns>The progress value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The pair violates the invariant, which is a gateway defect.</exception>
    public static FileOperationProgress Create(int completed, int total)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(total, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(completed);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(completed, total);
        return new FileOperationProgress(completed, total);
    }
}
