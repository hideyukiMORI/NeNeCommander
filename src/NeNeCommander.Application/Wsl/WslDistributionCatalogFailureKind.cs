namespace NeNeCommander.Application.Wsl;

/// <summary>Identifies one closed expected failure of WSL distribution discovery.</summary>
public abstract record WslDistributionCatalogFailureKind
{
    /// <summary>The WSL discovery provider could not be started or returned failure.</summary>
    public static WslDistributionCatalogFailureKind ProviderUnavailable { get; } = new ProviderUnavailableFailure();

    /// <summary>The provider returned output that cannot be represented safely.</summary>
    public static WslDistributionCatalogFailureKind MalformedOutput { get; } = new MalformedOutputFailure();

    private WslDistributionCatalogFailureKind()
    {
    }

    private sealed record ProviderUnavailableFailure : WslDistributionCatalogFailureKind;

    private sealed record MalformedOutputFailure : WslDistributionCatalogFailureKind;
}
