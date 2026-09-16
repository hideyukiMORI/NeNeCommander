namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslRootCheckRejected : LiveWslRootCheckOutcome
{
    internal LiveWslRootCheckRejected(LiveWslRootFailureKind failure)
    {
        Failure = failure;
    }

    internal LiveWslRootFailureKind Failure { get; }
}
