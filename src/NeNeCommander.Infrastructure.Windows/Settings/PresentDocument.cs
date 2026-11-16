using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>A valid regular settings document with its observed identity and exact bytes.</summary>
internal sealed record PresentDocument : ExistingDocumentSnapshot
{
    internal PresentDocument(FileIdentity identity, byte[] bytes)
    {
        Identity = identity;
        Bytes = bytes;
    }

    internal FileIdentity Identity { get; }

    internal byte[] Bytes { get; }
}
