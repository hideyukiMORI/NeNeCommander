using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves reverse copy and composite move against real owned file trees.</summary>
[TestClass]
public sealed class WslToWindowsTransferGatewayTests
{
    /// <summary>Proves files, nested directories and API-visible private-use names preserve their bytes.</summary>
    [TestMethod]
    public async Task CopyWhenTreeIsRepresentableKeepsNamesBytesAndSources()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\tree\\nested");
        _ = fixture.Root.WriteFile("wsl\\tree\\nested\\a\uF03Ab.txt", "payload");
        _ = fixture.Root.WriteFile("wsl\\single.txt", "single");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Copy(WslToWindowsTransferFixture.Source("tree"), WslToWindowsTransferFixture.Source("single.txt")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.Copied,
            FileOperationEffectKind.Verified, FileOperationEffectKind.Copied, FileOperationEffectKind.Verified);
        Assert.AreEqual("payload", File.ReadAllText(fixture.Root.Resolve("windows\\tree\\nested\\a\uF03Ab.txt")));
        Assert.AreEqual("payload", File.ReadAllText(fixture.Root.Resolve("wsl\\tree\\nested\\a\uF03Ab.txt")));
        Assert.AreEqual("single", File.ReadAllText(fixture.Root.Resolve("windows\\single.txt")));
        Assert.AreEqual("single", File.ReadAllText(fixture.Root.Resolve("wsl\\single.txt")));
    }

    /// <summary>Proves a move uses only copy, verification and the source provider's permanent deletion.</summary>
    [TestMethod]
    public async Task MoveWhenCopyAndVerificationSucceedDeletesOnlyTheWslSource()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\tree\\nested");
        _ = fixture.Root.WriteFile("wsl\\tree\\nested\\payload.txt", "payload");
        StepHookPort port = new(fixture.Router);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.Copied,
            FileOperationEffectKind.Verified, FileOperationEffectKind.SourceDeleted);
        Assert.AreEqual("capability,copy,verify,delete", string.Join(',', port.Calls));
        Assert.HasCount(1, port.DeletionModes);
        VerifiedCopyReceipt receipt = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(port.DeletionModes[0]).Receipt;
        Assert.AreEqual(WslToWindowsTransferFixture.Source("tree"), receipt.Source.Path);
        Assert.AreEqual(fixture.Destination, receipt.Root.Target.Path.Parent);
        Assert.IsFalse(Directory.Exists(fixture.Root.Resolve("wsl\\tree")));
        Assert.AreEqual("payload", File.ReadAllText(fixture.Root.Resolve("windows\\tree\\nested\\payload.txt")));
    }

    /// <summary>Proves verification failure preserves the source and the truthful copy effect.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task MoveWhenTargetChangesBeforeVerificationKeepsSource()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\file.txt", "original");
        StepHookPort port = new(fixture.Router)
        {
            AfterCopy = snapshot =>
            {
                _ = fixture.Root.WriteFile("windows\\file.txt", "damaged-target");
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("file.txt")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Verification, outcome.Failure);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.Copied);
        Assert.AreEqual("capability,copy,verify", string.Join(',', port.Calls));
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
    }

    /// <summary>Proves cancellation after verification leaves both the source and verified copy.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task MoveWhenCancelledBeforeDeletionKeepsBothEntries()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\file.txt", "original");
        using CancellationTokenSource cancellation = new();
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = _ =>
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("file.txt")),
            IgnoredFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.Copied, FileOperationEffectKind.Verified);
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("windows\\file.txt")));
        Assert.DoesNotContain("delete", port.Calls);
    }
}
