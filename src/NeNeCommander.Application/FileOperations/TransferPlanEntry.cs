using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Fixes the exact target and disposition for one frozen source.</summary>
public sealed record TransferPlanEntry
{
    private TransferPlanEntry(
        FileEntrySnapshot source,
        FileSystemPath target,
        TransferDisposition disposition)
    {
        Source = source;
        Target = target;
        Disposition = disposition;
    }

    /// <summary>Gets the original frozen source.</summary>
    public FileEntrySnapshot Source { get; }
    /// <summary>Gets the exact target reserved by preflight.</summary>
    public FileSystemPath Target { get; }
    /// <summary>Gets whether the source is transferred or skipped.</summary>
    public TransferDisposition Disposition { get; }

    /// <summary>Creates an entry that transfers to an exact target.</summary>
    public static TransferPlanEntry Transfer(FileEntrySnapshot source, FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return new TransferPlanEntry(source, target, TransferDisposition.Transfer);
    }

    /// <summary>Creates an entry that records an explicit non-effect.</summary>
    public static TransferPlanEntry Skip(FileEntrySnapshot source, FileSystemPath target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return new TransferPlanEntry(source, target, TransferDisposition.Skip);
    }
}
