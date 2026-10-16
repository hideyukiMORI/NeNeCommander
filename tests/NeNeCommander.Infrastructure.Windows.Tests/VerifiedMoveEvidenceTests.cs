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

/// <summary>Proves descendant evidence survives verification, source-provider admission and partial deletion.</summary>
[TestClass]
public sealed class VerifiedMoveEvidenceTests
{
    /// <summary>Proves owned parent metadata changes do not prevent an unchanged nested move.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenNestedTreeIsUnchangedDeletesOnlyVerifiedEntries()
    {
        using WslToWindowsTransferFixture fixture = Tree();
        StepHookPort port = new(fixture.Router);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

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
        using WslToWindowsTransferFixture fixture = Tree();
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

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

        AssertRetained(fixture, outcome);
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
        using WslToWindowsTransferFixture fixture = Tree();
        StepHookPort port = new(fixture.Router)
        {
            AfterVerify = snapshot =>
            {
                ChangeSource(fixture, change);
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

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
        using WslToWindowsTransferFixture fixture = Tree();
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
        FileOperationOutcome moved = await MoveAsync(gateway, fixture);
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
        using WslToWindowsTransferFixture fixture = Tree(path =>
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

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

        Assert.IsTrue(changed);
        AssertRetained(fixture, outcome);
        Assert.AreEqual("other", File.ReadAllText(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")));
    }

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
        using WslToWindowsTransferFixture fixture = Tree(path =>
        {
            inspect(path);
            WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
            return changed && ((change == "untouched-root" && path.EndsWith("\\directory", StringComparison.Ordinal)) ||
                (change == "untouched-sibling" && path.EndsWith("\\sibling", StringComparison.Ordinal)))
                ? ChangedTime(facts) : facts;
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
                        ApplyLateChange(fixture, change);
                    }
                };
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

        Assert.IsTrue(changed);
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

    /// <summary>Proves a later ctime change is not accepted solely because a touched directory retains its inode.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenTouchedDirectoryChangesAfterReanchorRejectsStableObjectOnlyMatch()
    {
        Func<string, WslHandleFacts> inspect = OwnedWslNamespace.ReadFacts;
        using WslToWindowsTransferFixture fixture = Tree(path => inspect(path));
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
                        ? ChangedTime(facts) : facts;
                };
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

        Assert.IsTrue(observationsAfterRemoval >= 3);
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\b.txt")));
        Assert.IsTrue(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
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
        using WslToWindowsTransferFixture fixture = Tree();
        StepHookPort port = new(fixture.Router)
        {
            TransformDeletion = (_, mode) => CorruptReceipt(mode, corruption),
        };
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await MoveAsync(gateway, fixture);

        AssertRetained(fixture, outcome);
        Assert.IsNotNull(outcome.Failure);
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
        _ = root.CreateDirectory("windows\\directory\\nested");
        _ = root.CreateDirectory("wsl\\destination");
        _ = root.WriteFile("windows\\directory\\nested\\a.txt", "first");
        _ = root.WriteFile("windows\\directory\\nested\\b.txt", "second");
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
        WindowsLocalIoExecutionBoundary execution = new();
        StepHookPort port = new(new ProviderFileOperationPort(new WindowsLocalFileOperationAdapter(execution, fileSystem),
            new WslFileOperationAdapter(execution, fileSystem), new WindowsToWslTransfer(execution, fileSystem),
            new WslToWindowsTransfer(execution, fileSystem)))
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

    private static DeletionExecutionMode CorruptReceipt(DeletionExecutionMode mode, string corruption)
    {
        VerifiedCopyReceipt receipt = Assert.IsInstanceOfType<DeletionExecutionMode.VerifiedCopyMode>(mode).Receipt;
        VerifiedCopyEntry.VerifiedDirectory root = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedDirectory>(receipt.Root);
        if (corruption.Contains("ancestor", StringComparison.Ordinal))
        {
            VerifiedTreeBoundary boundary = receipt.Boundary;
            VerifiedTreeBoundary changedBoundary = corruption switch
            {
                "missing-source-ancestor" => VerifiedTreeBoundary.Create([], boundary.TargetAncestors),
                "missing-target-ancestor" => VerifiedTreeBoundary.Create(boundary.SourceAncestors, []),
                "foreign-source-ancestor" => VerifiedTreeBoundary.Create(boundary.TargetAncestors, boundary.TargetAncestors),
                _ => VerifiedTreeBoundary.Create(boundary.SourceAncestors, boundary.TargetAncestors.Reverse().ToArray()),
            };
            return DeletionExecutionMode.VerifiedCopy(VerifiedCopyReceipt.Create(receipt.Source, root, changedBoundary));
        }
        if (corruption == "wrong-provider")
        {
            FileEntrySnapshot foreign = FileEntrySnapshot.Create(root.Target.Path, root.Target.Identity, DeletionCapability.PermanentOnly);
            return DeletionExecutionMode.VerifiedCopy(VerifiedCopyReceipt.Create(foreign,
                VerifiedCopyEntry.Directory(root.Target, root.Target, root.Children), receipt.Boundary));
        }
        VerifiedCopyEntry changed = corruption switch
        {
            "missing-child" => VerifiedCopyEntry.Directory(root.Source, root.Target, []),
            "duplicate-directory" => VerifiedCopyEntry.Directory(root.Source, root.Target, [root.Children[0], root.Children[0]]),
            _ => VerifiedCopyEntry.Directory(root.Source, root.Target, [VerifiedCopyEntry.Directory(
                root.Children[0].Source, root.Target, [])]),
        };
        return DeletionExecutionMode.VerifiedCopy(VerifiedCopyReceipt.Create(receipt.Source, changed, receipt.Boundary));
    }

    private static WslHandleFacts ChangedTime(WslHandleFacts facts)
    {
        return new WslHandleFacts(facts.Inode, facts.Attributes, facts.ReparseTag, facts.EndOfFile,
            facts.NumberOfLinks, facts.LastWriteFileTimeUtc, facts.ChangeFileTimeUtc + 1);
    }

    private static void ChangeSource(WslToWindowsTransferFixture fixture, string change)
    {
        string file = fixture.Root.Resolve("wsl\\directory\\nested\\a.txt");
        DateTime modified = File.GetLastWriteTimeUtc(file);
        if (change == "rewrite-restored-time")
        {
            _ = fixture.Root.WriteFile("wsl\\directory\\nested\\a.txt", "other");
            File.SetLastWriteTimeUtc(file, modified);
        }
        else if (change == "replacement")
        {
            DateTime created = File.GetCreationTimeUtc(file);
            File.Move(file, fixture.Root.Resolve("wsl\\directory\\nested\\old.txt"));
            _ = fixture.Root.WriteFile("wsl\\directory\\nested\\a.txt", "first");
            File.SetCreationTimeUtc(file, created);
            File.SetLastWriteTimeUtc(file, modified);
        }
        else if (change == "added")
        {
            _ = fixture.Root.WriteFile("wsl\\directory\\nested\\late.txt", "late");
        }
        else if (change == "removed")
        {
            File.Delete(file);
        }
        else if (change == "renamed")
        {
            File.Move(file, fixture.Root.Resolve("wsl\\directory\\nested\\renamed.txt"));
        }
        else
        {
            _ = fixture.Root.CreateDirectory("link-target");
            _ = fixture.Root.CreateJunction("wsl\\directory\\nested\\link", "link-target");
        }
    }

    private static void ApplyLateChange(WslToWindowsTransferFixture fixture, string change)
    {
        if (change is "untouched-root" or "untouched-sibling")
        {
            return;
        }
        if (change == "reanchor-failure")
        {
            throw new IOException("Synthetic failure after the first owned removal.");
        }
        string target = change switch
        {
            "new-child" => "wsl\\directory\\nested\\late.txt",
            "external-parent" => "wsl\\outside.txt",
            _ => "wsl\\directory\\nested\\b.txt",
        };
        _ = fixture.Root.WriteFile(target, change == "new-child" ? "new" : "update");
    }

    private static WslToWindowsTransferFixture Tree(Func<string, WslHandleFacts>? readFacts = null)
    {
        WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(readFacts);
        _ = fixture.Root.CreateDirectory("wsl\\directory\\nested");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\a.txt", "first");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\b.txt", "second");
        return fixture;
    }

    private static Task<FileOperationOutcome> MoveAsync(FileOperationGateway gateway, WslToWindowsTransferFixture fixture)
    {
        return gateway.ExecuteAsync(fixture.Move(WslToWindowsTransferFixture.Source("directory")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);
    }

    private static void AssertRetained(WslToWindowsTransferFixture fixture, FileOperationOutcome outcome)
    {
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")));
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\b.txt")));
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted ||
            effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
    }
}
