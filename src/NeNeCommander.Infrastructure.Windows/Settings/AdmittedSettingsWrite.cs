using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// The state one write carries out of baseline preflight: the serialized document, the approved
/// location and destination baselines, and the attempt's own temporary candidate.
/// </summary>
internal sealed record AdmittedSettingsWrite
{
    internal AdmittedSettingsWrite(
        byte[] serialized,
        SafeLocation location,
        ExistingDocumentSnapshot existing,
        WindowsLocalPath temporaryPath)
    {
        Serialized = serialized;
        Location = location;
        Existing = existing;
        TemporaryPath = temporaryPath;
    }

    internal byte[] Serialized { get; }

    internal SafeLocation Location { get; }

    internal ExistingDocumentSnapshot Existing { get; }

    internal WindowsLocalPath TemporaryPath { get; }
}
