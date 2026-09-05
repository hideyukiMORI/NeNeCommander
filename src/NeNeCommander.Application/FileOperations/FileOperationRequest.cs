using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents one immutable request accepted by the filesystem mutation gateway.
/// </summary>
public abstract record FileOperationRequest
{
    private const int MaximumSourceCount = 10000;

    internal FileOperationRequest(IReadOnlyList<FileSystemPath> sources)
    {
        Sources = sources;
    }

    /// <summary>Gets the frozen ordered source set.</summary>
    public IReadOnlyList<FileSystemPath> Sources { get; }

    private protected static FileOperationRequestFailureKind? ValidateSourceSet(
        IReadOnlyList<FileSystemPath> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Count == 0)
        {
            return FileOperationRequestFailureKind.EmptySources;
        }
        if (sources.Count > MaximumSourceCount)
        {
            return FileOperationRequestFailureKind.TooManySources;
        }

        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        foreach (FileSystemPath source in sources)
        {
            if (source is null)
            {
                return FileOperationRequestFailureKind.NullSource;
            }
            if (!identities.Add(source))
            {
                return FileOperationRequestFailureKind.DuplicateSource;
            }
        }
        return null;
    }

    private protected static FileOperationRequestFailureKind? ValidateTransfer(
        IReadOnlyList<FileSystemPath> sources,
        FileSystemPath destination)
    {
        FileOperationRequestFailureKind? failure = ValidateSourceSet(sources);
        return failure ??
            (sources.Any(source => FileSystemPathIdentityComparer.Instance.Equals(source, destination))
                ? FileOperationRequestFailureKind.DestinationIsSource
                : null);
    }
}
