using System;

namespace NeNeCommander.Application.Wsl;

/// <summary>Represents one expected WSL distribution discovery failure.</summary>
public sealed record WslDistributionCatalogFailed : WslDistributionCatalogOutcome
{
    internal WslDistributionCatalogFailed(WslDistributionCatalogFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized failure.</summary>
    public WslDistributionCatalogFailureKind Failure { get; }
}
