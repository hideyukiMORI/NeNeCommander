using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents one immutable, provider-neutral request to read the direct entries of a location.
/// </summary>
public sealed record DirectoryReadRequest
{
    private DirectoryReadRequest(FileSystemPath location, int entryBoundary)
    {
        Location = location;
        EntryBoundary = entryBoundary;
    }

    /// <summary>Gets the validated location to read.</summary>
    public FileSystemPath Location { get; }

    /// <summary>
    /// Gets the positive number of entries after which an adapter must stop enumerating
    /// and report a bounded listing.
    /// </summary>
    public int EntryBoundary { get; }

    /// <summary>
    /// Creates a validated read request.
    /// </summary>
    /// <param name="location">Validated location to read.</param>
    /// <param name="entryBoundary">Entry boundary between one and <see cref="DirectoryListing.EntryBoundaryLimit"/>.</param>
    /// <returns>An accepted request or a typed rejection.</returns>
    public static DirectoryReadRequestCreation Create(FileSystemPath location, int entryBoundary)
    {
        ArgumentNullException.ThrowIfNull(location);
        return entryBoundary is < 1 or > DirectoryListing.EntryBoundaryLimit
            ? new DirectoryReadRequestRejected()
            : new DirectoryReadRequestAccepted(new DirectoryReadRequest(location, entryBoundary));
    }
}
