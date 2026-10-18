using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Preserves VerifiedMoveEvidenceTests evidence for VerifiedMovePartialDeletionTests.</summary>
[TestClass]
public sealed class VerifiedMovePartialDeletionTests
{
    /// <summary>Proves late changes stop after truthful partial effects, without sweeping unknown entries.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("sibling")]
    [DataRow("new-child")]
    [DataRow("reanchor-failure")]
    [DataRow("external-parent")]
    [DataRow("untouched-root")]
    [DataRow("untouched-sibling")]
    public async Task ExecuteAsyncWhenSourceChangesAfterFirstLeafDeletionReportsPartialSource(string change)
    {
        Action<string> inspect = _ => { };
        bool changed = false;
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree(path =>
        {
            inspect(path);
            WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
            return changed && VerifiedMoveTestContext.IsUntouchedDirectoryTarget(change, path)
                ? VerifiedMoveTestContext.ChangedTime(facts) : facts;
        });
        if (change == "untouched-sibling")
        {
            _ = fixture.Root.CreateDirectory("wsl\\directory\\sibling");
        }
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = snapshot =>
            {
                inspect = path =>
                {
                    if (!changed && !File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")) &&
                        path.EndsWith("\\nested", StringComparison.Ordinal))
                    {
                        changed = true;
                        VerifiedMoveTestContext.ApplyLateChange(fixture, change);
                    }
                };
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        Assert.IsTrue(changed);
        AssertPartialWslSource(fixture, outcome, change);
    }

    /// <summary>Proves a later ctime change is not accepted solely because a touched directory retains its inode.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenTouchedDirectoryChangesAfterReanchorRejectsStableObjectOnlyMatch()
    {
        Func<string, WslHandleFacts> inspect = OwnedWslNamespace.ReadFacts;
        using WslToWindowsTransferFixture fixture = VerifiedMoveTestContext.Tree(path => inspect(path));
        int observationsAfterRemoval = 0;
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = snapshot =>
            {
                inspect = path =>
                {
                    WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
                    return path.EndsWith("\\nested", StringComparison.Ordinal) &&
                        !File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")) && ++observationsAfterRemoval > 2
                        ? VerifiedMoveTestContext.ChangedTime(facts) : facts;
                };
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await VerifiedMoveTestContext.MoveAsync(gateway, fixture);

        Assert.IsTrue(observationsAfterRemoval >= 3);
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\b.txt")));
        Assert.IsTrue(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
    }

    /// <summary>Proves Windows-source deletion routes through its owner and retains partial effects on a late change or denied target.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    [DataRow("sibling")]
    [DataRow("denied")]
    [DataRow("late-directory-child")]
    public async Task ExecuteAsyncWhenWindowsSourceDeletionStopsAfterFirstLeafReportsPartialSource(string change)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        CreateWindowsSourceTree(root);
        bool deleting = false;
        bool changed = false;
        int emptyDirectoryTargetReads = 0;
        WindowsWslFileSystem fileSystem = new(path => OwnedWslNamespace.Resolve(root.ResolveVerbatim("wsl"), path), path =>
        {
            if (deleting && change == "late-directory-child" && !changed &&
                !File.Exists(root.Resolve("windows\\directory\\nested\\a.txt")) &&
                !File.Exists(root.Resolve("windows\\directory\\nested\\b.txt")) &&
                path.EndsWith("\\nested", StringComparison.Ordinal) && ++emptyDirectoryTargetReads == 3)
            {
                _ = root.WriteFile("windows\\directory\\nested\\late.txt", "unverified child");
                changed = true;
            }
            else if (deleting && change != "late-directory-child" && !changed &&
                !File.Exists(root.Resolve("windows\\directory\\nested\\a.txt")))
            {
                changed = true;
                if (change == "denied")
                {
                    throw new UnauthorizedAccessException("Synthetic target denial after source removal.");
                }
                _ = root.WriteFile("windows\\directory\\nested\\b.txt", "update");
            }
            return OwnedWslNamespace.ReadFacts(path);
        });
        StepHookPort port = new(WindowsToWslPort(fileSystem))
        {
            AfterVerify = _ =>
            {
                deleting = true;
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);
        FileSystemPath source = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root.Resolve("windows\\directory"))).Path;
        FileSystemPath target = Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("\\\\wsl.localhost\\Ubuntu\\owned\\destination")).Path;
        MoveRequest request = Assert.IsInstanceOfType<MoveRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(MoveRequest.Create([source], target)).Request);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);

        Assert.IsTrue(changed);
        AssertPartialWindowsSource(root, outcome, change);
    }

    private static ProviderFileOperationPort WindowsToWslPort(WindowsWslFileSystem fileSystem)
    {
        WindowsLocalIoExecutionBoundary execution = new();
        return new ProviderFileOperationPort(new WindowsLocalFileOperationAdapter(execution, fileSystem),
            new WslFileOperationAdapter(execution, fileSystem), new WindowsToWslTransfer(execution, fileSystem),
            new WslToWindowsTransfer(execution, fileSystem));
    }

    private static void AssertPartialWslSource(WslToWindowsTransferFixture fixture, FileOperationOutcome outcome, string change)
    {
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsFalse(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")));
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\b.txt")));
        Assert.AreEqual("first", File.ReadAllText(fixture.Root.Resolve("windows\\directory\\nested\\a.txt")));
        Assert.IsTrue(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
        if (change == "new-child")
        {
            Assert.AreEqual("new", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\nested\\late.txt")));
        }
    }

    private static void AssertPartialWindowsSource(TestOwnedTemporaryRoot root, FileOperationOutcome outcome, string change)
    {
        Assert.IsFalse(File.Exists(root.Resolve("windows\\directory\\nested\\a.txt")));
        Assert.AreEqual(change != "late-directory-child", File.Exists(root.Resolve("windows\\directory\\nested\\b.txt")));
        Assert.AreEqual("first", File.ReadAllText(root.Resolve("wsl\\destination\\directory\\nested\\a.txt")));
        Assert.IsTrue(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
        if (change == "denied")
        {
            Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        }
        if (change == "late-directory-child")
        {
            Assert.AreEqual("unverified child", File.ReadAllText(root.Resolve("windows\\directory\\nested\\late.txt")));
        }
    }

    private static void CreateWindowsSourceTree(TestOwnedTemporaryRoot root)
    {
        _ = root.CreateDirectory("windows\\directory\\nested");
        _ = root.CreateDirectory("wsl\\destination");
        _ = root.WriteFile("windows\\directory\\nested\\a.txt", "first");
        _ = root.WriteFile("windows\\directory\\nested\\b.txt", "second");
    }
}
