namespace NeNeCommander.Infrastructure.Windows.Tests;

internal abstract record LiveWslRootEntryKind
{
    internal static LiveWslRootEntryKind Directory { get; } = new DirectoryEntry();

    internal static LiveWslRootEntryKind File { get; } = new FileEntry();

    internal static LiveWslRootEntryKind Link { get; } = new LinkEntry();

    private LiveWslRootEntryKind()
    {
    }

    private sealed record DirectoryEntry : LiveWslRootEntryKind;

    private sealed record FileEntry : LiveWslRootEntryKind;

    private sealed record LinkEntry : LiveWslRootEntryKind;
}
