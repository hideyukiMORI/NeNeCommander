using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves whole-batch rejection before the reverse transfer writes any destination.</summary>
[TestClass]
public sealed class WslToWindowsPreflightTests
{
    /// <summary>Proves Windows-invalid top-level names stop an otherwise valid earlier source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-015")]
    [DataRow("bad.")]
    [DataRow("bad ")]
    [DataRow("CON")]
    [DataRow("NUL.txt")]
    [DataRow("COM².txt")]
    [DataRow("lpt³.tar.gz")]
    public async Task CopyWhenLaterSourceNameIsInvalidRejectsEntireBatch(string name)
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\first.txt", "first");
        _ = fixture.Root.WriteVerbatimFile("wsl\\" + name, "invalid");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Copy(WslToWindowsTransferFixture.Source("first.txt"), WslToWindowsTransferFixture.Source(name)),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        AssertRejected(fixture, outcome, FileOperationFailureKind.ProviderUnavailable);
        Assert.AreEqual("invalid", File.ReadAllText(fixture.Root.ResolveVerbatim("wsl\\" + name)));
    }

    /// <summary>Proves invalid names deep in a later tree are rejected before the first source is copied.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    [DataRow("bad.")]
    [DataRow("bad ")]
    [DataRow("COM1.txt")]
    [DataRow("com³.txt")]
    [DataRow("LPT¹")]
    public async Task CopyWhenLaterTreeContainsInvalidNameRejectsBeforeEffects(string name)
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\first.txt", "first");
        _ = fixture.Root.CreateDirectory("wsl\\tree\\nested");
        _ = fixture.Root.WriteVerbatimFile("wsl\\tree\\nested\\" + name, "invalid");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("first.txt"), WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        AssertRejected(fixture, outcome, FileOperationFailureKind.ProviderUnavailable);
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("wsl\\first.txt")));
    }

    /// <summary>Proves two Linux names that become one Windows identity reject the entire batch.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task CopyWhenDifferentSourcesCollideWithoutCaseRejectsWithoutEffects()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\one");
        _ = fixture.Root.CreateDirectory("wsl\\two");
        _ = fixture.Root.WriteFile("wsl\\one\\Name.txt", "one");
        _ = fixture.Root.WriteFile("wsl\\two\\name.txt", "two");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Copy(WslToWindowsTransferFixture.Source("one\\Name.txt"), WslToWindowsTransferFixture.Source("two\\name.txt")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        AssertRejected(fixture, outcome, FileOperationFailureKind.Conflict);
    }

    /// <summary>Proves an existing directory is a collision rather than an invitation to merge trees.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-022")]
    public async Task CopyWhenLaterTargetDirectoryExistsPreservesEveryExistingEntry()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.WriteFile("wsl\\first.txt", "first");
        _ = fixture.Root.CreateDirectory("wsl\\tree");
        _ = fixture.Root.WriteFile("wsl\\tree\\new.txt", "new");
        _ = fixture.Root.CreateDirectory("windows\\tree");
        _ = fixture.Root.WriteFile("windows\\tree\\existing.txt", "existing");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Copy(WslToWindowsTransferFixture.Source("first.txt"), WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.IsFalse(File.Exists(fixture.Root.Resolve("windows\\first.txt")));
        Assert.IsFalse(File.Exists(fixture.Root.Resolve("windows\\tree\\new.txt")));
        Assert.AreEqual("existing", File.ReadAllText(fixture.Root.Resolve("windows\\tree\\existing.txt")));
    }

    /// <summary>Proves a nested source junction cannot be copied or followed into its target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-003")]
    public async Task CopyWhenSourceContainsJunctionRejectsWithoutEffects()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create();
        _ = fixture.Root.CreateDirectory("wsl\\tree");
        _ = fixture.Root.CreateDirectory("other");
        _ = fixture.Root.WriteFile("other\\sentinel.txt", "keep");
        _ = fixture.Root.CreateJunction("wsl\\tree\\link", "other");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Move(WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        AssertRejected(fixture, outcome, FileOperationFailureKind.ProviderUnavailable);
        Assert.AreEqual("keep", File.ReadAllText(fixture.Root.Resolve("other\\sentinel.txt")));
    }

    /// <summary>Proves each nested WSL entry receives the guarded handle inspection before writes.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-020")]
    public async Task CopyWhenNestedWslHandleIsDeniedRejectsBeforeEffects()
    {
        using WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(path =>
            path.EndsWith("\\denied", StringComparison.Ordinal)
                ? throw new UnauthorizedAccessException("Synthetic nested mount refusal.")
                : WindowsFileIdentifier.ReadHandleFacts(path));
        _ = fixture.Root.CreateDirectory("wsl\\tree\\denied");
        using FileOperationGateway gateway = new(fixture.Router);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            fixture.Copy(WslToWindowsTransferFixture.Source("tree")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);

        AssertRejected(fixture, outcome, FileOperationFailureKind.AccessDenied);
    }

    private static void AssertRejected(
        WslToWindowsTransferFixture fixture, FileOperationOutcome outcome, FileOperationFailureKind failure)
    {
        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(failure, outcome.Failure);
        Assert.HasCount(0, outcome.Effects);
        Assert.HasCount(0, Directory.GetFileSystemEntries(fixture.Destination.CanonicalText));
    }
}
