namespace NeNeCommander.Application.Launching;

/// <summary>Identifies one closed expected reason a file handoff was not accepted.</summary>
public abstract record FileLaunchFailureKind
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the failure for a target or containing directory that no longer exists.</summary>
    public static FileLaunchFailureKind NotFound { get; } = new NotFoundFailure();

    /// <summary>Gets the failure for an operating-system access denial.</summary>
    public static FileLaunchFailureKind AccessDenied { get; } = new AccessDeniedFailure();

    /// <summary>Gets the failure for a target that has no available Windows association.</summary>
    public static FileLaunchFailureKind AssociationUnavailable { get; } = new AssociationUnavailableFailure();

    /// <summary>Gets the failure for a filesystem provider this launcher does not support.</summary>
    public static FileLaunchFailureKind ProviderUnavailable { get; } = new ProviderUnavailableFailure();

    /// <summary>Gets the closed fallback for another expected Windows Shell rejection.</summary>
    public static FileLaunchFailureKind ShellRejected { get; } = new ShellRejectedFailure();

    private FileLaunchFailureKind()
    {
    }

    private sealed record NotFoundFailure : FileLaunchFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AccessDeniedFailure : FileLaunchFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AssociationUnavailableFailure : FileLaunchFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ProviderUnavailableFailure : FileLaunchFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ShellRejectedFailure : FileLaunchFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
