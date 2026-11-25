using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains an object observation and an explicit qualification of its path association.</summary>
public sealed record ObservedTransferEntry
{
    private ObservedTransferEntry(VerifiedEntrySnapshot entry, TransferPathObservation pathObservation)
    {
        Entry = entry;
        PathObservation = pathObservation;
    }

    /// <summary>Gets the last handle-derived full and stable identities and typed path.</summary>
    public VerifiedEntrySnapshot Entry { get; }

    /// <summary>Gets whether the path was current at this observation, never a perpetual identity guarantee.</summary>
    public TransferPathObservation PathObservation { get; }

    /// <summary>Creates an observation without reinterpreting or reopening its path.</summary>
    public static ObservedTransferEntry Create(VerifiedEntrySnapshot entry, TransferPathObservation pathObservation)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(pathObservation);
        return new ObservedTransferEntry(entry, pathObservation);
    }
}
