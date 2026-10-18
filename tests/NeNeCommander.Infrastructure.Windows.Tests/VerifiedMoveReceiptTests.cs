using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves VerifiedMoveEvidenceTests evidence for VerifiedMoveReceiptTests.</summary>
[TestClass]
public sealed class VerifiedMoveReceiptTests
{
    /// <summary>Proves owned parent metadata changes do not prevent an unchanged nested move.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenNestedTreeIsUnchangedDeletesOnlyVerifiedEntries()
    {
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree();
        StepHookPort port = new(fixture.Router);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.IsFalse(Directory.Exists(fixture.Root.Resolve("wsl\\directory")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\a.txt")));
        Assert.AreEqual("second", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\b.txt")));
        _ = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(port.DeletionModes.Single());
        Assert.AreSame(FileOperationEffectKind.SourceDeleted, outcome.Effects[^1].Kind);
    }

    /// <summary>Proves target content changes with restored metadata never authorize source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenVerifiedTargetBytesChangeWithRestoredTimesRetainsSource()
    {
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree();
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = snapshot =>
            {
                string target = fixture.Root.Resolve("windows\\directory\\nested\\a.txt");
                DateTime modified = File.GetLastWriteTimeUtc(target);
                _ = fixture.Root.WriteFile("windows\\directory\\nested\\a.txt", "other");
                File.SetLastWriteTimeUtc(target, modified);
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        VerifiedMoveTestContext.AssertRetained(fixture, outcome);
        Assert.AreEqual("other", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\a.txt")));
    }

    /// <summary>Proves rewrite, replacement, membership and link changes after verification stop source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("rewrite-restored-time")]
    [DataRow("replacement")]
    [DataRow("added")]
    [DataRow("removed")]
    [DataRow("renamed")]
    [DataRow("link")]
    public async Task ExecuteAsyncWhenVerifiedSourceChangesRetainsRemainingSource(string change)
    {
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree();
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = snapshot =>
            {
                VerifiedMoveTestContext.ChangeSource(fixture, change);
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsTrue(Directory.Exists(fixture.Root.Resolve("wsl\\directory\\nested")));
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\b.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\a.txt")));
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted ||
            effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
    }

    /// <summary>Proves stale successful evidence cannot delete a recreated source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task DeleteAsyncWhenSuccessfulReceiptIsReplayedAgainstRecreatedSourceRejects()
    {
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree();
        VerifiedCopyReceipt? saved = null;
        StepHookPort port = new(fixture.Router)
        {
            TransformDeletion = (_, mode) =>
            {
                saved = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(mode).Receipt;
                return mode;
            },
        };
        using FileOperationGateway gateway = new(port);
        FileOperationOutcome moved = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, moved.Completion);
        VerifiedCopyReceipt receipt = saved ?? throw new AssertFailedException("Deletion evidence was not captured.");
        _ = fixture.Root.CreateDirectory("wsl\\directory\\nested");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\a.txt", "first");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\b.txt", "second");

        ProviderStepOutcome outcome = await fixture.Router.DeleteAsync(receipt.Source,
            DeletionExecutionMode.VerifiedCopy(receipt), CancellationToken.None);

        Assert.IsNotNull(outcome.Failure);
        Assert.IsNull(outcome.Effect);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")));
    }

    /// <summary>Proves an already scanned file is checked again after a later file is inspected.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("verification")]
    [DataRow("deletion-scan")]
    public async Task ExecuteAsyncWhenEarlierChildChangesDuringLaterVerificationRetainsSource(string phase)
    {
        Action<string> inspect = _ => { };
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree(path =>
        {
            inspect(path);
            return OwnedWslNamespace.ReadFacts(path);
        });
        bool changed = false;
        Task InstallMutationAsync(FileEntrySnapshot snapshot)
        {
            inspect = path =>
            {
                if (!changed && path.EndsWith("\\b.txt", StringComparison.Ordinal))
                {
                    _ = fixture.Root.WriteFile("wsl\\directory\\nested\\a.txt", "other");
                    changed = true;
                }
            };
            return Task.CompletedTask;
        }
        StepHookPort port = new(fixture.Router)
        {
            AfterCopy = phase == "verification" ? InstallMutationAsync : _ => Task.CompletedTask,
            AfterVerify = phase == "deletion-scan" ? InstallMutationAsync : _ => Task.CompletedTask,
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        Assert.IsTrue(changed);
        VerifiedMoveTestContext.AssertRetained(fixture, outcome);
        Assert.AreEqual("other", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")));
    }

    /// <summary>Proves malformed receipt coverage and mapping are refused before source effects.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("missing-child")]
    [DataRow("duplicate-directory")]
    [DataRow("foreign-target")]
    [DataRow("missing-source-ancestor")]
    [DataRow("missing-target-ancestor")]
    [DataRow("foreign-source-ancestor")]
    [DataRow("reordered-target-ancestors")]
    [DataRow("wrong-provider")]
    public async Task ExecuteAsyncWhenReceiptCoverageIsInvalidRetainsSource(string corruption)
    {
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree();
        StepHookPort port = new(fixture.Router)
        {
            TransformDeletion = (_, mode) => VerifiedMoveTestContext.CorruptReceipt(mode, corruption),
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        VerifiedMoveTestContext.AssertRetained(fixture, outcome);
        Assert.IsNotNull(outcome.Failure);
    }
}
