using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed record LiveWslDeclaredEntry
{
    private LiveWslDeclaredEntry(string relativePath, DirectoryEntryKind kind, long length)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        RelativePath = relativePath;
        Kind = kind;
        Length = length;
    }

    internal string RelativePath { get; }

    internal DirectoryEntryKind Kind { get; }

    internal long Length { get; }

    internal static LiveWslDeclaredEntry Create(string relativePath, DirectoryEntryKind kind, long length)
    {
        return new LiveWslDeclaredEntry(relativePath, kind, length);
    }
}
