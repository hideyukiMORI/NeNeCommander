using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslRootEntry
{
    private LiveWslRootEntry(
        WslPath path,
        string resolvedPath,
        string identity,
        LiveWslRootEntryKind kind)
    {
        Path = path;
        ResolvedPath = resolvedPath;
        Identity = identity;
        Kind = kind;
    }

    internal WslPath Path { get; }

    internal string ResolvedPath { get; }

    internal string Identity { get; }

    internal LiveWslRootEntryKind Kind { get; }

    internal static LiveWslRootEntry Create(
        WslPath path,
        string resolvedPath,
        string identity,
        LiveWslRootEntryKind kind)
    {
        return new LiveWslRootEntry(path, resolvedPath, identity, kind);
    }
}
