using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves composite moves retain descendants rewritten after copying or verification.</summary>
[TestClass]
public sealed class VerifiedMoveSourceRaceTests
{
    /// <summary>Proves a same-length descendant rewrite after copying cannot be discarded by source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenDescendantChangesAfterCopyKeepsUpdatedSource()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\directory\\nested");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\payload.txt", "original");
        StepHookPort port = new(fixture.Router)
        {
            AfterCopy = snapshot =>
            {
                _ = fixture.Root.WriteFile("wsl\\directory\\nested\\payload.txt", "modified");
                WslFileSystemEntry current = Assert.IsInstanceOfType<WslFileSystemEntry>(
                    fixture.FileSystem.Find(WslToWindowsTransferFixture.Source("directory")));
                Assert.AreEqual(snapshot.Identity, current.Identity,
                    "The requested directory identity must remain unchanged after its descendant rewrite.");
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("directory")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\payload.txt")));
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\payload.txt")),
            "The move must retain the rewritten source; completed steps: " + string.Join(',', port.Calls));
        Assert.AreEqual("modified", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\nested\\payload.txt")));
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
    }

    /// <summary>Proves a different-length descendant rewrite after verification cannot be discarded by source deletion.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenDescendantChangesAfterVerificationKeepsUpdatedSource()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\directory\\nested");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\payload.txt", "original");
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = snapshot =>
            {
                _ = fixture.Root.WriteFile("wsl\\directory\\nested\\payload.txt", "updated-after-verification");
                WslFileSystemEntry current = Assert.IsInstanceOfType<WslFileSystemEntry>(
                    fixture.FileSystem.Find(WslToWindowsTransferFixture.Source("directory")));
                Assert.AreEqual(snapshot.Identity, current.Identity,
                    "The requested directory identity must remain unchanged after its descendant rewrite.");
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("directory")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreEqual("original", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\payload.txt")));
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\payload.txt")),
            "The move must retain the rewritten source; completed steps: " + string.Join(',', port.Calls));
        Assert.AreEqual("updated-after-verification", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\nested\\payload.txt")));
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
    }
}
