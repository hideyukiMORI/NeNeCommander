namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslRootCleanupRejected : LiveWslRootCleanupOutcome
{
    internal LiveWslRootCleanupRejected(LiveWslRootFailureKind failure)
    {
        Failure = failure;
    }

    internal LiveWslRootFailureKind Failure { get; }
}
