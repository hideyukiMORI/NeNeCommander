using System;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Represents a settings document rejected without applying partial state.</summary>
public sealed record SettingsValidationRejected : SettingsValidationOutcome
{
    internal SettingsValidationRejected(SettingsValidationFailureKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public SettingsValidationFailureKind Kind { get; }
}
