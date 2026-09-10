using System;

namespace NeNeCommander.Application.Launching;

/// <summary>
/// Represents the closed acceptance, pre-handoff cancellation, or expected failure of one file
/// launch request. Acceptance means only that the external provider accepted the path handoff.
/// </summary>
public abstract record FileLaunchOutcome
{
    internal FileLaunchOutcome()
    {
    }

    /// <summary>Creates an outcome for a path handoff accepted by the external provider.</summary>
    /// <returns>The accepted outcome.</returns>
    public static FileLaunchOutcome Accepted()
    {
        return new FileLaunchAccepted();
    }

    /// <summary>Creates an outcome for cancellation observed before the path handoff began.</summary>
    /// <returns>The cancelled outcome.</returns>
    public static FileLaunchOutcome Cancelled()
    {
        return new FileLaunchCancelled();
    }

    /// <summary>Creates an outcome for an expected handoff failure.</summary>
    /// <param name="failure">Normalized reason the provider did not accept the handoff.</param>
    /// <returns>The failed outcome.</returns>
    public static FileLaunchOutcome Failed(FileLaunchFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new FileLaunchFailed(failure);
    }
}
