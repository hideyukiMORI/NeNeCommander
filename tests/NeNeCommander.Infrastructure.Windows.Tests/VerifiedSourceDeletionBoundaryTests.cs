using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves frozen-source binding and immediate parent admission independently of outer adapter checks.</summary>
[TestClass]
public sealed class VerifiedSourceDeletionBoundaryTests
{
    /// <summary>Rejects a frozen source whose sole changed field is identity, while the receipt and tree remain valid.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task DeleteVerifiedSourceWhenOnlyFrozenIdentityDiffersRetainsEverySourceAsync()
    {
        using WslToWindowsTransferFixture fixture = Tree();
        VerifiedCopyReceipt receipt = await ReceiptAsync(fixture);
        FileIdentity wrongIdentity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("different-source")).Identity;
        FrozenSourcePort port = new(FileEntrySnapshot.Create(receipt.Source.Path, wrongIdentity,
            receipt.Source.DeletionCapability), receipt.Root.Target.Path);
        using FileOperationGateway gateway = new(port);
        _ = await gateway.ExecuteAsync(fixture.Copy(receipt.Source.Path), IgnoredFileOperationProgress.Create(), CancellationToken.None);
        FileEntrySnapshot source = port.Frozen ?? throw new AssertFailedException("The gateway did not freeze the source.");
        Assert.AreEqual(receipt.Source.Path, source.Path);
        Assert.AreEqual(receipt.Source.TransferTarget, source.TransferTarget);
        Assert.AreEqual(receipt.Source.DeletionCapability, source.DeletionCapability);
        Assert.AreNotEqual(receipt.Source.Identity, source.Identity);

        ProviderStepOutcome outcome = fixture.FileSystem.DeleteVerifiedSource(source, receipt);

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\a.txt")));
        Assert.AreEqual("second", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\b.txt")));
    }

    /// <summary>Stops permanently after an observed parent inconsistency, retaining the first owned deletion effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("object")]
    [DataRow("membership")]
    [DataRow("capture-pair")]
    [DataRow("transient-object")]
    public async Task DeleteVerifiedSourceWhenParentReanchorObservesOneMismatchStopsAfterFirstLeafAsync(string change)
    {
        Func<string, WslHandleFacts> read = OwnedWslNamespace.ReadFacts;
        using WslToWindowsTransferFixture fixture = Tree(path => read(path));
        VerifiedCopyReceipt receipt = await ReceiptAsync(fixture);
        WslHandleFacts baseline = OwnedWslNamespace.ReadFacts(fixture.Root.ResolveVerbatim("wsl\\directory"));
        int observations = 0;
        read = path =>
        {
            WslHandleFacts current = OwnedWslNamespace.ReadFacts(path);
            if (!path.EndsWith("\\directory", StringComparison.Ordinal) || File.Exists(fixture.Root.Resolve("wsl\\directory\\a.txt")))
            {
                return current;
            }
            observations++;
            if (change == "membership" && observations == 1)
            {
                _ = fixture.Root.WriteFile("wsl\\directory\\late.txt", "unrecorded");
            }
            return ParentFacts(current, baseline, change, observations);
        };

        ProviderStepOutcome outcome = fixture.FileSystem.DeleteVerifiedSource(receipt.Source, receipt);

        Assert.IsTrue(observations >= (change == "capture-pair" ? 2 : 1));
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.AreSame(ProviderStepEffectKind.SourcePartiallyDeleted, outcome.Effect);
        Assert.IsFalse(File.Exists(fixture.Root.Resolve("wsl\\directory\\a.txt")));
        Assert.AreEqual("second", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\b.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\a.txt")));
        if (change == "membership")
        {
            Assert.AreEqual("unrecorded", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\late.txt")));
        }
    }

    /// <summary>Requires independent target bytes and metadata admission immediately before deleting a leaf.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("bytes")]
    [DataRow("transient-metadata")]
    public async Task DeleteVerifiedSourceWhenTargetChangesDuringLeafAdmissionRetainsLeafAsync(string change)
    {
        Func<string, WslHandleFacts> read = OwnedWslNamespace.ReadFacts;
        using WslToWindowsTransferFixture fixture = Tree(path => read(path));
        VerifiedCopyReceipt receipt = await ReceiptAsync(fixture);
        int firstLeafReads = 0;
        bool changed = false;
        DateTime modified = File.GetLastWriteTimeUtc(fixture.Root.Resolve("windows\\directory\\a.txt"));
        read = path =>
        {
            if (path.EndsWith("\\a.txt", StringComparison.Ordinal))
            {
                firstLeafReads++;
                if (firstLeafReads == 3)
                {
                    if (change == "bytes")
                    {
                        _ = fixture.Root.WriteFile("windows\\directory\\a.txt", "other");
                    }
                    File.SetLastWriteTimeUtc(fixture.Root.Resolve("windows\\directory\\a.txt"),
                        change == "bytes" ? modified : modified.AddTicks(10000000));
                    changed = true;
                }
                else if (change == "transient-metadata" && firstLeafReads == 4)
                {
                    File.SetLastWriteTimeUtc(fixture.Root.Resolve("windows\\directory\\a.txt"), modified);
                }
            }
            return OwnedWslNamespace.ReadFacts(path);
        };

        ProviderStepOutcome outcome = fixture.FileSystem.DeleteVerifiedSource(receipt.Source, receipt);

        Assert.IsTrue(changed);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\a.txt")));
        Assert.AreEqual(change == "bytes" ? "other" : "first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\a.txt")));
    }

    /// <summary>Normalizes denied child-name resolution during receipt structure validation before any deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task DeleteVerifiedSourceWhenStructureResolutionIsDeniedReturnsTypedAccessDeniedAsync()
    {
        using WslToWindowsTransferFixture fixture = Tree();
        VerifiedCopyReceipt receipt = await ReceiptAsync(fixture);
        bool denied = false;
        WindowsWslFileSystem fileSystem = new(path =>
        {
            if (path == WslToWindowsTransferFixture.Source("directory\\a.txt"))
            {
                denied = true;
                throw new UnauthorizedAccessException("Owned child-name observation denied.");
            }
            return OwnedWslNamespace.Resolve(fixture.Root.ResolveVerbatim("wsl"), path);
        }, OwnedWslNamespace.ReadFacts);

        ProviderStepOutcome outcome = fileSystem.DeleteVerifiedSource(receipt.Source, receipt);

        Assert.IsTrue(denied);
        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\a.txt")));
        Assert.AreEqual("second", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\b.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\a.txt")));
    }

    /// <summary>Refuses a different target before observing it when the gateway has frozen the exact destination.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task VerifyCopyWhenTargetDiffersFromFrozenBindingReturnsTypedVerificationAsync()
    {
        using WslToWindowsTransferFixture fixture = Tree();
        VerifiedCopyReceipt receipt = await ReceiptAsync(fixture);
        FileSystemPath other = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(fixture.Root.Resolve("windows\\other"))).Path;

        CopyVerificationOutcome outcome = fixture.FileSystem.VerifyCopy(receipt.Source, other);

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        Assert.IsFalse(Directory.Exists(fixture.Root.Resolve("windows\\other")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\a.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\a.txt")));
    }

    private static WslHandleFacts ParentFacts(WslHandleFacts current, WslHandleFacts baseline, string change, int observations)
    {
        return change switch
        {
            "object" => ChangedObject(current),
            "transient-object" when observations == 1 => ChangedObject(current),
            "transient-object" => baseline,
            "capture-pair" when observations == 2 => new WslHandleFacts(current.Inode, current.Attributes,
                current.ReparseTag, current.EndOfFile, current.NumberOfLinks, current.LastWriteFileTimeUtc, current.ChangeFileTimeUtc + 1),
            "capture-pair" when observations > 2 => baseline,
            _ => current,
        };
    }

    private static WslHandleFacts ChangedObject(WslHandleFacts current)
    {
        return new WslHandleFacts(current.Inode + 1, current.Attributes, current.ReparseTag, current.EndOfFile,
            current.NumberOfLinks, current.LastWriteFileTimeUtc, current.ChangeFileTimeUtc);
    }

    private static WslToWindowsTransferFixture Tree(Func<string, WslHandleFacts>? read = null)
    {
        WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(read);
        _ = fixture.Root.CreateDirectory("wsl\\directory");
        _ = fixture.Root.WriteFile("wsl\\directory\\a.txt", "first");
        _ = fixture.Root.WriteFile("wsl\\directory\\b.txt", "second");
        return fixture;
    }

    private static async Task<VerifiedCopyReceipt> ReceiptAsync(WslToWindowsTransferFixture fixture)
    {
        FileEntrySnapshot? frozen = null;
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = source =>
            {
                frozen = source;
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome copied = await gateway.ExecuteAsync(fixture.Copy(WslToWindowsTransferFixture.Source("directory")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, copied.Completion);
        FileEntrySnapshot source = frozen ?? throw new AssertFailedException("No frozen source was observed.");
        return Assert.IsInstanceOfType<CopyVerified>(fixture.FileSystem.VerifyCopy(source,
            source.TransferTarget ?? throw new AssertFailedException("The exact transfer target was not frozen."))).Receipt;
    }

    private sealed class FrozenSourcePort : IFileOperationPort
    {
        private readonly FileEntrySnapshot _source;
        private readonly FileSystemPath _target;

        internal FrozenSourcePort(FileEntrySnapshot source, FileSystemPath target)
        {
            _source = source;
            _target = target;
        }

        internal FileEntrySnapshot? Frozen { get; private set; }

        public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
        {
            return Task.FromResult(FileInspectionOutcome.Succeeded(_source));
        }

        public Task<TransferPreflightOutcome> PreflightTransferAsync(IReadOnlyList<FileEntrySnapshot> sources,
            FileSystemPath destination, CancellationToken cancellationToken)
        {
            return Task.FromResult(TransferPreflightOutcome.Succeeded([TransferPlanEntry.Transfer(sources[0], _target)]));
        }

        public Task<ProviderStepOutcome> CopyAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
        {
            Frozen = source;
            return Task.FromResult(ProviderStepOutcome.Failed(FileOperationFailureKind.Copy));
        }

        public Task<CopyVerificationOutcome> VerifyCopyAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The frozen-source capture stops before verification.");
        }

        public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The frozen-source capture is a copy.");
        }

        public Task<ProviderStepOutcome> MoveAsync(FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The frozen-source capture is a copy.");
        }

        public Task<ProviderStepOutcome> DeleteAsync(FileEntrySnapshot source, DeletionExecutionMode mode, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The frozen-source capture has no deletion.");
        }

        public Task<ProviderStepOutcome> CreateDirectoryAsync(FileEntrySnapshot location, FileSystemPath target, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The frozen-source capture has no directory creation.");
        }

        public Task<ProviderStepOutcome> RenameAsync(FileEntrySnapshot source, FileSystemPath target, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The frozen-source capture has no rename.");
        }
    }
}
