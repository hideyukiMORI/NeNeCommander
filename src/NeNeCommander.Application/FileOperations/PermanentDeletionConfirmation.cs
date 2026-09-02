using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents explicit UI confirmation for one exact permanent-deletion source set.
/// </summary>
public sealed record PermanentDeletionConfirmation
{
    private readonly ReadOnlyCollection<FileSystemPath> _sources;

    private PermanentDeletionConfirmation(ReadOnlyCollection<FileSystemPath> sources)
    {
        _sources = sources;
    }

    /// <summary>Creates confirmation for one already displayed and accepted request.</summary>
    /// <param name="request">Accepted request whose exact sources were named by the UI.</param>
    /// <returns>Confirmation bound to an owned immutable snapshot.</returns>
    public static PermanentDeletionConfirmation CreateFor(FileOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        List<FileSystemPath> ownedSources = [.. request.Sources];
        return new PermanentDeletionConfirmation(ownedSources.AsReadOnly());
    }

    internal bool Covers(IReadOnlyList<FileSystemPath> sources)
    {
        if (_sources.Count != sources.Count)
        {
            return false;
        }
        for (int index = 0; index < sources.Count; index++)
        {
            if (!FileSystemPathIdentityComparer.Instance.Equals(_sources[index], sources[index]))
            {
                return false;
            }
        }
        return true;
    }
}
