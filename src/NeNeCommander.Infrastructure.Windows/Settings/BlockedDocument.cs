using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>An observed document state that rejects every write with one failure kind.</summary>
internal sealed record BlockedDocument : ExistingDocumentSnapshot
{
    internal BlockedDocument(SettingsWriteFailureKind failure)
    {
        Failure = failure;
    }

    internal SettingsWriteFailureKind Failure { get; }
}
