using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Directories;

/// <summary>
/// Represents an immutable, deterministically ordered snapshot of one directory's direct entries.
/// </summary>
public sealed record DirectoryListing
{
    /// <summary>Gets the fixed upper boundary of entries any listing may contain.</summary>
    public const int EntryBoundaryLimit = 10000;

    private readonly ReadOnlyCollection<DirectoryEntry> _entries;

    private DirectoryListing(
        FileSystemPath location,
        ReadOnlyCollection<DirectoryEntry> entries,
        DirectoryListingCompleteness completeness,
        int unrepresentableEntryCount)
    {
        Location = location;
        _entries = entries;
        Completeness = completeness;
        UnrepresentableEntryCount = unrepresentableEntryCount;
    }

    /// <summary>Gets the validated location that was read.</summary>
    public FileSystemPath Location { get; }

    /// <summary>
    /// Gets the entries ordered by kind with directories first, then by name ignoring case,
    /// then by ordinal name so providers with case-sensitive names remain deterministic.
    /// </summary>
    public IReadOnlyList<DirectoryEntry> Entries => _entries;

    /// <summary>Gets whether enumeration read every entry or stopped at its boundary.</summary>
    public DirectoryListingCompleteness Completeness { get; }

    /// <summary>Gets the number of entries the provider reported but the path model rejected.</summary>
    public int UnrepresentableEntryCount { get; }

    /// <summary>
    /// Validates and orders adapter-supplied entries without depending on enumeration order.
    /// </summary>
    /// <param name="location">Validated location that was read.</param>
    /// <param name="entries">Direct entries in any order.</param>
    /// <param name="completeness">Whether enumeration stopped at its boundary.</param>
    /// <param name="unrepresentableEntryCount">Count of provider entries rejected by the path model.</param>
    /// <returns>An accepted listing or a typed rejection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The unrepresentable count is negative, which is an adapter defect.</exception>
    public static DirectoryListingCreation Create(
        FileSystemPath location,
        IReadOnlyList<DirectoryEntry> entries,
        DirectoryListingCompleteness completeness,
        int unrepresentableEntryCount)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(completeness);
        ArgumentOutOfRangeException.ThrowIfNegative(unrepresentableEntryCount);

        DirectoryListingFailureKind? failure = ValidateEntries(entries);
        if (failure is not null)
        {
            return new DirectoryListingRejected(failure);
        }

        List<DirectoryEntry> ordered = [.. entries
            .OrderBy(entry => entry.Kind == DirectoryEntryKind.Directory ? 0 : 1)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)];
        return new DirectoryListingAccepted(
            new DirectoryListing(location, ordered.AsReadOnly(), completeness, unrepresentableEntryCount));
    }

    private static DirectoryListingFailureKind? ValidateEntries(IReadOnlyList<DirectoryEntry> entries)
    {
        if (entries.Count > EntryBoundaryLimit)
        {
            return DirectoryListingFailureKind.TooManyEntries;
        }

        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        foreach (DirectoryEntry entry in entries)
        {
            if (entry is null)
            {
                return DirectoryListingFailureKind.NullEntry;
            }
            if (!identities.Add(entry.Path))
            {
                return DirectoryListingFailureKind.DuplicateEntry;
            }
        }
        return null;
    }
}
