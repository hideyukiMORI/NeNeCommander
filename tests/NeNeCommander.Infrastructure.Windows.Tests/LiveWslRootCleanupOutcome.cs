namespace NeNeCommander.Infrastructure.Windows.Tests;

internal abstract record LiveWslRootCleanupOutcome
{
    internal static LiveWslRootCleanupOutcome Completed { get; } = new LiveWslRootCleanupCompleted();

    private protected LiveWslRootCleanupOutcome()
    {
    }
}
