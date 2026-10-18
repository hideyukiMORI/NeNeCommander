using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Shares the existing deterministic setup and assertions for VerifiedMoveEvidenceTests.</summary>
internal static class VerifiedMoveTestContext
{
    internal static DeletionExecutionMode CorruptReceipt(DeletionExecutionMode mode, string corruption)
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

    internal static bool IsUntouchedDirectoryTarget(string change, string path)
    {
        return change switch
        {
            "untouched-root" => path.EndsWith("\\directory", StringComparison.Ordinal),
            "untouched-sibling" => path.EndsWith("\\sibling", StringComparison.Ordinal),
            _ => false,
        };
    }

    internal static WslHandleFacts ChangedTime(WslHandleFacts facts)
    {
        return new WslHandleFacts(facts.Inode, facts.Attributes, facts.ReparseTag, new WslRewriteFacts(facts.EndOfFile, facts.NumberOfLinks, facts.LastWriteFileTimeUtc, facts.ChangeFileTimeUtc + 1));
    }

    internal static void ChangeSource(WslToWindowsTransferFixture fixture, string change)
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

    internal static void ApplyLateChange(WslToWindowsTransferFixture fixture, string change)
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

    internal static WslToWindowsTransferFixture Tree(Func<string, WslHandleFacts>? readFacts = null)
    {
        WslToWindowsTransferFixture fixture = WslToWindowsTransferFixture.Create(readFacts);
        _ = fixture.Root.CreateDirectory("wsl\\directory\\nested");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\a.txt", "first");
        _ = fixture.Root.WriteFile("wsl\\directory\\nested\\b.txt", "second");
        return fixture;
    }

    internal static Task<FileOperationOutcome> MoveAsync(FileOperationGateway gateway, WslToWindowsTransferFixture fixture)
    {
        return gateway.ExecuteAsync(fixture.Move(WslToWindowsTransferFixture.Source("directory")),
            IgnoredFileOperationProgress.Create(), CancellationToken.None);
    }

    internal static void AssertRetained(WslToWindowsTransferFixture fixture, FileOperationOutcome outcome)
    {
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\a.txt")));
        Assert.IsTrue(File.Exists(fixture.Root.Resolve("wsl\\directory\\nested\\b.txt")));
        Assert.IsFalse(outcome.Effects.Any(effect => effect.Kind == FileOperationEffectKind.SourceDeleted ||
            effect.Kind == FileOperationEffectKind.SourcePartiallyDeleted));
    }
}
