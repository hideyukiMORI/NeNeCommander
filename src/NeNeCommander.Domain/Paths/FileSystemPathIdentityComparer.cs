using System;
using System.Collections.Generic;

namespace NeNeCommander.Domain.Paths;

/// <summary>Compares validated paths using provider-aware lexical identity.</summary>
/// <remarks>
/// <para>
/// Windows local and UNC paths compare without regard to case. WSL distribution names compare
/// without regard to case, while Linux paths use ordinal, case-sensitive comparison.
/// </para>
/// <para>
/// This comparer performs no filesystem query, operating-system file identity check, or alias resolution.
/// </para>
/// </remarks>
public sealed class FileSystemPathIdentityComparer : IEqualityComparer<FileSystemPath>
{
    private FileSystemPathIdentityComparer()
    {
    }

    /// <summary>Gets the sole provider-aware path identity comparer.</summary>
    public static FileSystemPathIdentityComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(FileSystemPath? left, FileSystemPath? right)
    {
        return ReferenceEquals(left, right) ||
            (left is not null && right is not null && left.HasSameIdentity(right));
    }

    /// <inheritdoc />
    public int GetHashCode(FileSystemPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.GetIdentityHashCode();
    }
}
