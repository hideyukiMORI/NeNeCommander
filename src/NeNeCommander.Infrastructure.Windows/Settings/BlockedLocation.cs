using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>An observed ancestor chain that rejects every write with one failure kind.</summary>
internal sealed record BlockedLocation : SettingsLocationSnapshot
{
    internal BlockedLocation(SettingsWriteFailureKind failure)
    {
        Failure = failure;
    }

    internal SettingsWriteFailureKind Failure { get; }
}
