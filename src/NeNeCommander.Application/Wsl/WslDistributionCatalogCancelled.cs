namespace NeNeCommander.Application.Wsl;

/// <summary>Represents discovery cancelled without returning a partial snapshot.</summary>
public sealed record WslDistributionCatalogCancelled : WslDistributionCatalogOutcome
{
    internal WslDistributionCatalogCancelled()
    {
    }
}
