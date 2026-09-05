using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents the closed provider answer for whether one inspected source can be moved atomically
/// to a destination at the time of transfer preflight.
/// </summary>
public abstract record AtomicMoveCapabilityOutcome
{
    private protected AtomicMoveCapabilityOutcome()
    {
    }

    /// <summary>Gets the answer that permits the gateway to request one atomic provider move.</summary>
    public static AtomicMoveCapabilityOutcome Supported { get; } = new AtomicMoveSupported();

    /// <summary>Gets the answer that requires the gateway's existing copy-verify-delete move.</summary>
    public static AtomicMoveCapabilityOutcome Unsupported { get; } = new AtomicMoveUnsupported();

    /// <summary>Creates a failed capability query that stops the complete batch before mutation.</summary>
    /// <param name="failure">Normalized expected provider failure.</param>
    /// <returns>A failed capability outcome.</returns>
    public static AtomicMoveCapabilityOutcome Failed(FileOperationFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new AtomicMoveCapabilityFailed(failure);
    }

    private sealed record AtomicMoveSupported : AtomicMoveCapabilityOutcome;

    private sealed record AtomicMoveUnsupported : AtomicMoveCapabilityOutcome;
}
