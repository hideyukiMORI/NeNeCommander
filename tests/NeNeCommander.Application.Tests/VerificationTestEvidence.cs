using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Creates declared-port evidence for deterministic orchestration fakes without filesystem access.</summary>
internal static class VerificationTestEvidence
{
    internal static CopyVerificationOutcome FromStep(ProviderStepOutcome step, FileEntrySnapshot source, FileSystemPath destination)
    {
        if (step.Failure is FileOperationFailureKind failure)
        {
            return CopyVerificationOutcome.Failed(failure);
        }
        string text = source.Path.CanonicalText;
        string name = text[(text.LastIndexOf('\\') + 1)..];
        FileSystemPath target = source.TransferTarget ?? ((PathParseSuccess)destination.Child(name)).Path;
        return ForTarget(source, target);
    }

    internal static CopyVerificationOutcome ForTarget(FileEntrySnapshot source, FileSystemPath target)
    {
        FileObjectIdentity identity = FileObjectIdentity.Create(source.Identity.Value);
        VerifiedCopyEntry entry = VerifiedCopyEntry.File(
            VerifiedEntrySnapshot.Create(source.Path, source.Identity, identity),
            VerifiedEntrySnapshot.Create(target, source.Identity, identity),
            FileContentHash.Create(new string('0', 64)));
        return CopyVerificationOutcome.Succeeded(VerifiedCopyReceipt.Create(source, entry, VerifiedTreeBoundary.Create([], [])));
    }
}
