using System;
using NeNeCommander.Application.Directories;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains exact local file identity and displayed facts supplied by one qualified provider observation.</summary>
public sealed record TransferOverwriteFileObservation
{
    private TransferOverwriteFileObservation(ObservedTransferEntry observation, DirectoryEntryKind kind, KnownEntrySize size, KnownEntryTimestamp modified)
    {
        Observation = observation;
        Kind = kind;
        Size = size;
        Modified = modified;
    }

    /// <summary>Gets the exact full/stable identity and the path confirmed at observation time.</summary>
    public ObservedTransferEntry Observation { get; }
    /// <summary>Gets the observed regular-file kind, without inferring native eligibility from it.</summary>
    public DirectoryEntryKind Kind { get; }
    /// <summary>Gets the displayed non-negative byte count.</summary>
    public KnownEntrySize Size { get; }
    /// <summary>Gets the displayed modification instant in UTC.</summary>
    public KnownEntryTimestamp Modified { get; }

    /// <summary>Retains known file facts; the provider remains responsible for actual NTFS, stream, attribute and link admission.</summary>
    public static TransferOverwriteFileObservation Create(
        ObservedTransferEntry observation, DirectoryEntryKind kind, KnownEntrySize size, KnownEntryTimestamp modified)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(modified);
        return observation.Entry.Path is not WindowsLocalPath || observation.Entry.Path.Parent is null ||
            observation.PathObservation != TransferPathObservation.CurrentPathVerified
            ? throw new ArgumentException("Overwrite evidence requires a currently observed non-root Windows local path.", nameof(observation))
            : kind == DirectoryEntryKind.File
                ? new TransferOverwriteFileObservation(observation, kind, size, modified)
                : throw new ArgumentException("Overwrite evidence requires a regular file observation.", nameof(kind));
    }
}
