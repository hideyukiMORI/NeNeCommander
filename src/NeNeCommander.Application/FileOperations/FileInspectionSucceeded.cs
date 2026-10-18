using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Represents one complete, successful provider inspection.</summary>
public sealed record FileInspectionSucceeded : FileInspectionOutcome
{
    private protected override void RestrictDerivation()
    {
    }

    internal FileInspectionSucceeded(FileEntrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    /// <summary>Gets the complete frozen provider snapshot.</summary>
    public FileEntrySnapshot Snapshot { get; }
}
