using System;
using System.Collections.Generic;

namespace NeNeCommander.Domain.Paths;

/// <summary>Compares validated paths using their provider's native identity rules.</summary>
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
