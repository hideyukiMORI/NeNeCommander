using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Drives;

/// <summary>
/// Represents one complete snapshot of the listed volumes, each root at most once, plus the number
/// of reported roots that could not be represented as a drive root and are therefore not shown.
/// </summary>
public sealed record DriveCatalogSucceeded : DriveCatalogOutcome
{
    private readonly ReadOnlyCollection<DriveLocation> _drives;

    internal DriveCatalogSucceeded(IReadOnlyList<DriveLocation> drives, int unrepresentableRootCount)
    {
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentOutOfRangeException.ThrowIfNegative(unrepresentableRootCount);
        List<DriveLocation> snapshot = new(drives.Count);
        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        foreach (DriveLocation drive in drives)
        {
            ArgumentNullException.ThrowIfNull(drive);
            if (!identities.Add(drive.Root))
            {
                throw new ArgumentException("Drive roots must be unique.", nameof(drives));
            }
            snapshot.Add(drive);
        }
        _drives = snapshot.AsReadOnly();
        UnrepresentableRootCount = unrepresentableRootCount;
    }

    /// <summary>Gets the validated owned snapshot in provider order.</summary>
    public IReadOnlyList<DriveLocation> Drives => _drives;

    /// <summary>Gets the number of reported roots that are not drive roots and are not listed.</summary>
    public int UnrepresentableRootCount { get; }
}
