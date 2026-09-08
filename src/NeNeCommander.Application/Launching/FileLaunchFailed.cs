using System;

namespace NeNeCommander.Application.Launching;

/// <summary>Represents one expected file handoff failure normalized by its provider adapter.</summary>
public sealed record FileLaunchFailed : FileLaunchOutcome
{
    internal FileLaunchFailed(FileLaunchFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized reason the provider did not accept the handoff.</summary>
    public FileLaunchFailureKind Failure { get; }
}
