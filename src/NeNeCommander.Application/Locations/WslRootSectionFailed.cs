using System;
using NeNeCommander.Application.Wsl;

namespace NeNeCommander.Application.Locations;

/// <summary>Represents a WSL section whose discovery failed; it lists nothing and is never success.</summary>
public sealed record WslRootSectionFailed : WslRootSection
{
    internal WslRootSectionFailed(WslDistributionCatalogFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the normalized discovery failure.</summary>
    public WslDistributionCatalogFailureKind Failure { get; }
}
