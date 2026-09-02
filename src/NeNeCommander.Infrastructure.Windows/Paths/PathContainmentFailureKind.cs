namespace NeNeCommander.Infrastructure.Windows.Paths;

/// <summary>Identifies one closed reason a path is outside an operation root.</summary>
public abstract record PathContainmentFailureKind
{
    /// <summary>Gets the failure for paths owned by different providers.</summary>
    public static PathContainmentFailureKind ProviderMismatch { get; } = new ProviderMismatchFailure();

    /// <summary>Gets the failure for a same-provider path outside the exact segment boundary.</summary>
    public static PathContainmentFailureKind OutsideRoot { get; } = new OutsideRootFailure();

    private PathContainmentFailureKind()
    {
    }

    private sealed record ProviderMismatchFailure : PathContainmentFailureKind;
    private sealed record OutsideRootFailure : PathContainmentFailureKind;
}
