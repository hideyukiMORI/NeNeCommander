using System;

namespace NeNeCommander.Application.Settings;

/// <summary>Represents a rejected settings write with separate directory and temporary effects.</summary>
public sealed record SettingsWriteRejected : SettingsWriteOutcome
{
    internal SettingsWriteRejected(
        SettingsWriteFailureKind failure,
        SettingsDirectoryEffect directoryEffect,
        SettingsWriteEffect temporaryEffect)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(directoryEffect);
        ArgumentNullException.ThrowIfNull(temporaryEffect);
        Failure = failure;
        DirectoryEffect = directoryEffect;
        TemporaryEffect = temporaryEffect;
    }

    /// <summary>Gets the normalized rejection reason.</summary>
    public SettingsWriteFailureKind Failure { get; }

    /// <summary>Gets the observed settings-directory effect of this attempt.</summary>
    public SettingsDirectoryEffect DirectoryEffect { get; }

    /// <summary>Gets the temporary-artifact residue of this attempt.</summary>
    public SettingsWriteEffect TemporaryEffect { get; }
}
