namespace NeNeCommander.Infrastructure.Windows.Tests;

internal abstract record LiveWslRootEntryKind
{
    internal static LiveWslRootEntryKind Regular { get; } = new RegularEntry();

    internal static LiveWslRootEntryKind Link { get; } = new LinkEntry();

    private LiveWslRootEntryKind()
    {
    }

    private sealed record RegularEntry : LiveWslRootEntryKind;

    private sealed record LinkEntry : LiveWslRootEntryKind;
}
