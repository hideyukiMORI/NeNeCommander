namespace NeNeCommander.Infrastructure.Windows.Tests;

internal abstract record LiveWslRootFailureKind
{
    internal static LiveWslRootFailureKind Unexecuted { get; } = new UnexecutedFailure();

    internal static LiveWslRootFailureKind InvalidConfiguration { get; } = new InvalidConfigurationFailure();

    internal static LiveWslRootFailureKind DistributionUnavailable { get; } = new DistributionUnavailableFailure();

    internal static LiveWslRootFailureKind UnsafeRoot { get; } = new UnsafeRootFailure();

    internal static LiveWslRootFailureKind RootUnavailable { get; } = new RootUnavailableFailure();

    internal static LiveWslRootFailureKind RootNotEmpty { get; } = new RootNotEmptyFailure();

    internal static LiveWslRootFailureKind IdentityChanged { get; } = new IdentityChangedFailure();

    internal static LiveWslRootFailureKind LinkDetected { get; } = new LinkDetectedFailure();

    internal static LiveWslRootFailureKind ForeignResidue { get; } = new ForeignResidueFailure();

    internal static LiveWslRootFailureKind CleanupFailed { get; } = new CleanupFailedFailure();

    private LiveWslRootFailureKind()
    {
    }

    private sealed record UnexecutedFailure : LiveWslRootFailureKind;

    private sealed record InvalidConfigurationFailure : LiveWslRootFailureKind;

    private sealed record DistributionUnavailableFailure : LiveWslRootFailureKind;

    private sealed record UnsafeRootFailure : LiveWslRootFailureKind;

    private sealed record RootUnavailableFailure : LiveWslRootFailureKind;

    private sealed record RootNotEmptyFailure : LiveWslRootFailureKind;

    private sealed record IdentityChangedFailure : LiveWslRootFailureKind;

    private sealed record LinkDetectedFailure : LiveWslRootFailureKind;

    private sealed record ForeignResidueFailure : LiveWslRootFailureKind;

    private sealed record CleanupFailedFailure : LiveWslRootFailureKind;
}
