using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves verification fixtures never turn an interrupted mutation into completed evidence.</summary>
[TestClass]
public sealed class VerificationTestEvidenceTests
{
    /// <summary>Proves success and failure remain distinct and cancellation has no verification conversion.</summary>
    [TestMethod]
    [DataRow("succeeded")]
    [DataRow("failed")]
    [DataRow("cancelled")]
    public void FromStepWhenCompletionIsExplicitDoesNotInventSuccess(string completion)
    {
        FileSystemPath path = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("C:\\source.txt")).Path;
        FileSystemPath destination = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("D:\\destination")).Path;
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("source")).Identity;
        FileEntrySnapshot source = FileEntrySnapshot.Create(path, identity, DeletionCapability.NotRequested);
        ProviderStepCompletion status = completion == "succeeded" ? ProviderStepCompletion.Succeeded
            : completion == "cancelled" ? ProviderStepCompletion.Cancelled : ProviderStepCompletion.Failed(FileOperationFailureKind.Verification);
        ProviderStepOutcome step = ProviderStepOutcome.Create(status, ProviderStepReceipt.Empty);

        if (completion == "cancelled")
        {
            _ = Assert.ThrowsExactly<ArgumentException>(() => VerificationTestEvidence.FromStep(step, source, destination));
            return;
        }
        CopyVerificationOutcome result = VerificationTestEvidence.FromStep(step, source, destination);
        Assert.AreSame(status.Failure, result.Failure);
        if (completion == "succeeded")
        {
            _ = Assert.IsInstanceOfType<CopyVerified>(result);
        }
    }
}
