using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Creates typed, deterministic content and path observations without filesystem access.</summary>
internal static class ReceiptObservationTestContext
{
    internal static ObservedTransferEntry Entry(string text, TransferPathObservation observation)
    {
        FileSystemPath path = GatewayTestContext.ParsePath(text);
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("full:" + text)).Identity;
        return ObservedTransferEntry.Create(VerifiedEntrySnapshot.Create(path, identity, FileObjectIdentity.Create("object:" + text)), observation);
    }

    internal static TransferArtifactEntry ArtifactEntry(string text, TransferPathObservation observation)
    {
        ObservedTransferEntry entry = Entry(text, observation);
        return TransferArtifactEntry.Create(Assert.IsInstanceOfType<WindowsLocalPath>(entry.Entry.Path), entry.Entry.ObjectIdentity,
            ArtifactFullIdentityObservation.Observed(entry.Entry.Identity), observation);
    }

    internal static FileContentEvidence Content(long length)
    {
        return FileContentEvidence.Create(Assert.IsInstanceOfType<KnownEntrySize>(EntrySize.Create(length)), FileContentHash.Create(new string('A', 64)));
    }

    internal static IdentifiedTransferArtifact Artifact(TransferArtifactRole role, ArtifactLinkObservation link)
    {
        string path = role == TransferArtifactRole.PreparedContent ? "D:\\destination\\stage" : "D:\\destination\\backup";
        TransferArtifactEntry entry = ArtifactEntry(path, TransferPathObservation.LastKnownPath);
        return Assert.IsInstanceOfType<IdentifiedTransferArtifact>(TransferArtifactReceipt.Identified(
            role, entry, ArtifactContentObservation.Verified(Content(0)), link));
    }

    internal static RecoveryFrameObservation RecoveryFrame(long length, RecoveryFlushObservation flush)
    {
        FileContentEvidence content = Content(length);
        return RecoveryFrameObservation.Create(RecoveryFrameEvidence.Create(content.Size, content.Hash), flush);
    }
}
