using System;

namespace NeNeCommander.Application.Settings;

/// <summary>Represents default session settings caused by a rejected startup document.</summary>
public sealed record SettingsPersistenceStartupRejected : SettingsPersistenceState
{
    private protected override void RestrictDerivation()
    {
    }

    internal SettingsPersistenceStartupRejected(SettingsReadFailureKind failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        Failure = failure;
    }

    /// <summary>Gets the original read rejection.</summary>
    public SettingsReadFailureKind Failure { get; }
}
