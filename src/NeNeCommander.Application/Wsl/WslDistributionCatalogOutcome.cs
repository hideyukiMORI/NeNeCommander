using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Wsl;

/// <summary>Represents the closed success, cancellation, or expected failure of discovery.</summary>
public abstract record WslDistributionCatalogOutcome
{
    internal WslDistributionCatalogOutcome()
    {
    }

    /// <summary>Creates a successful discovery outcome.</summary>
    /// <param name="roots">Validated distribution roots.</param>
    /// <returns>The successful outcome.</returns>
    public static WslDistributionCatalogOutcome Succeeded(IReadOnlyList<WslPath> roots)
    {
        return new WslDistributionCatalogSucceeded(roots);
    }

    /// <summary>Creates the cancelled discovery outcome.</summary>
    /// <returns>The cancelled outcome.</returns>
    public static WslDistributionCatalogOutcome Cancelled()
    {
        return new WslDistributionCatalogCancelled();
    }

    /// <summary>Creates an expected failed discovery outcome.</summary>
    /// <param name="failure">Normalized failure.</param>
    /// <returns>The failed outcome.</returns>
    public static WslDistributionCatalogOutcome Failed(WslDistributionCatalogFailureKind failure)
    {
        return new WslDistributionCatalogFailed(failure);
    }
}
