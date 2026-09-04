using System;

namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents a stored document that was rejected without applying partial state. The stored
/// bytes are left exactly as they were so the person who wrote them can correct them.
/// </summary>
public sealed record SettingsRejected : SettingsReadOutcome
{
    internal SettingsRejected(SettingsReadFailureKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public SettingsReadFailureKind Kind { get; }
}
