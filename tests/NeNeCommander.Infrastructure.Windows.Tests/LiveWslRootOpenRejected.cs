namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslRootOpenRejected : LiveWslRootOpenOutcome
{
    internal LiveWslRootOpenRejected(LiveWslRootFailureKind failure)
    {
        Failure = failure;
    }

    internal LiveWslRootFailureKind Failure { get; }
}
