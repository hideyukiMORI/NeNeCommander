using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves changes after preflight never bypass name admission or source-delete guards.</summary>
[TestClass]
public sealed class WslToWindowsTransferRaceTests
{
    /// <summary>Proves an invalid descendant introduced after scanning leaves a truthful partial target and its source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task MoveWhenInvalidChildAppearsAfterPreflightKeepsSourceAndReportsPartialTarget()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\tree\\nested");
        StepHookPort port = new(fixture.Router)
        {
            AfterPreflight = () =>
            {
                _ = fixture.Root.WriteVerbatimFile("wsl\\tree\\nested\\late.", "original");
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.CopyTargetCreated);
        Assert.AreEqual("capability,copy", string.Join(',', port.Calls));
        Assert.IsTrue(Directory.Exists(fixture.Root.Resolve("windows\\tree")));
        Assert.IsFalse(File.Exists(fixture.Root.Resolve("windows\\tree\\nested\\late")));
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.ResolveVerbatim("wsl\\tree\\nested\\late.")));
    }

    /// <summary>Proves rewriting the source after preflight stops copying before any destination is created.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    public async Task CopyWhenSourceChangesAfterPreflightRejectsIdentity()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\file.txt", "original");
        StepHookPort port = new(fixture.Router)
        {
            AfterPreflight = () =>
            {
                _ = fixture.Root.WriteFile("wsl\\file.txt", "changed-length");
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Copy(WslToWindowsTransferFixture.Source("file.txt")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.IsFalse(File.Exists(fixture.Root.Resolve("windows\\file.txt")));
        Assert.AreEqual("changed-length", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
    }

    /// <summary>Proves a denied nested mount or copy error after scanning retains the root effect and source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("access")]
    [DataRow("io")]
    public async Task MoveWhenNestedInspectionFailsDuringCopyKeepsSource(string failureKind)
    {
        bool failInspection = false;
        Exception failure = failureKind == "access"
            ? new UnauthorizedAccessException("Synthetic nested refusal.")
            : new IOException("Synthetic nested I/O failure.");
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(path =>
            failInspection && path.EndsWith("\\file.txt", StringComparison.Ordinal)
                ? throw failure : WindowsFileIdentifier.ReadHandleFacts(path));
        _ = fixture.Root.CreateDirectory("wsl\\tree");
        _ = fixture.Root.WriteFile("wsl\\tree\\file.txt", "original");
        StepHookPort port = new(fixture.Router)
        {
            AfterPreflight = () =>
            {
                failInspection = true;
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(failureKind == "access" ? FileOperationFailureKind.AccessDenied : FileOperationFailureKind.Copy,
            outcome.Failure);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.CopyTargetCreated);
        Assert.AreEqual("capability,copy", string.Join(',', port.Calls));
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("wsl\\tree\\file.txt")));
    }

    /// <summary>Proves a failed source deletion reports the completed copy and verification without claiming deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task MoveWhenDeletionIsDeniedKeepsVerifiedEffects()
    {
        bool denyDeletion = false;
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(path =>
            denyDeletion ? throw new UnauthorizedAccessException("Synthetic deletion refusal.")
                : WindowsFileIdentifier.ReadHandleFacts(path));
        _ = fixture.Root.WriteFile("wsl\\file.txt", "original");
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = _ =>
            {
                denyDeletion = true;
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("file.txt")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        WslToWindowsTransferFixture.AssertEffects(outcome, FileOperationEffectKind.Copied, FileOperationEffectKind.Verified);
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("wsl\\file.txt")));
        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("windows\\file.txt")));
    }
}
