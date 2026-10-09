namespace NeNeCommander.Application.Drives;

/// <summary>Identifies one closed expected failure of drive listing.</summary>
public abstract record DriveCatalogFailureKind
{
    /// <summary>The operating system could not list the volumes.</summary>
    public static DriveCatalogFailureKind ProviderUnavailable { get; } = new ProviderUnavailableFailure();

    /// <summary>The operating system denied listing the volumes.</summary>
    public static DriveCatalogFailureKind AccessDenied { get; } = new AccessDeniedFailure();

    private DriveCatalogFailureKind()
    {
    }

    private sealed record ProviderUnavailableFailure : DriveCatalogFailureKind;

    private sealed record AccessDeniedFailure : DriveCatalogFailureKind;
}
