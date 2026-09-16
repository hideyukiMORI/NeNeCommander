namespace NeNeCommander.Infrastructure.Windows.Tests;

internal abstract record LiveWslRootCheckOutcome
{
    internal static LiveWslRootCheckOutcome Accepted { get; } = new LiveWslRootCheckAccepted();

    private protected LiveWslRootCheckOutcome()
    {
    }
}
