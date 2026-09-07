namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslRootOpened : LiveWslRootOpenOutcome
{
    internal LiveWslRootOpened(LiveWslTestRoot root)
    {
        Root = root;
    }

    internal LiveWslTestRoot Root { get; }
}
