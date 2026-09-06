using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves Windows-local conflict resolution through the sole gateway and adapter path.</summary>
[TestClass]
public sealed class TransferConflictWindowsTests
{
    /// <summary>Proves KeepBoth copies and verifies the exact visible candidate.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenFileConflictsKeepsBothAtVisibleCandidate()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.WriteFile("report.txt", "source"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination\\report.txt", "existing");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());

        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            Copy([source], destination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        TransferConflict conflict = awaiting.Conflicts!.Conflicts[0];
        FileOperationOutcome completed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.Current,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.AreEqual(root.Resolve("destination\\report (2).txt"), conflict.KeepBothCandidate.CanonicalText, ignoreCase: true);
        Assert.AreEqual("existing", File.ReadAllText(root.Resolve("destination\\report.txt")));
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("destination\\report (2).txt")));
        Assert.HasCount(2, completed.Effects);
    }

    /// <summary>Proves a directory KeepBoth target is newly named and never merged.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenDirectoryConflictsCreatesNewNamedDirectoryWithoutMerge()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.CreateDirectory("tree"));
        _ = root.WriteFile("tree\\source.txt", "source");
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.CreateDirectory("destination\\tree");
        _ = root.WriteFile("destination\\tree\\existing.txt", "existing");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());

        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            Copy([source], destination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome completed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.Current,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.IsTrue(File.Exists(root.Resolve("destination\\tree\\existing.txt")));
        Assert.IsFalse(File.Exists(root.Resolve("destination\\tree\\source.txt")));
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("destination\\tree (2)\\source.txt")));
    }

    /// <summary>Proves KeepBoth move copies and verifies before deleting its original source.</summary>
    [TestMethod]
    public async Task ResumeAsyncWhenMoveKeepsBothUsesCompositeVerifiedMove()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.WriteFile("move.txt", "source"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination\\move.txt", "existing");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());
        MoveRequest request = (MoveRequest)((FileOperationRequestAccepted)MoveRequest.Create([source], destination)).Request;
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome completed = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.Current,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Completion);
        Assert.HasCount(3, completed.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, completed.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, completed.Effects[1].Kind);
        Assert.AreSame(FileOperationEffectKind.SourceDeleted, completed.Effects[2].Kind);
        Assert.IsFalse(File.Exists(root.Resolve("move.txt")));
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("destination\\move (2).txt")));
    }

    /// <summary>Proves a candidate race returns a fresh conflict even after an Apply-to-all choice.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ResumeAsyncWhenKeepBothCandidateIsTakenReturnsFreshConflict()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.WriteFile("report.txt", "source"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination\\report.txt", "existing");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());
        FileOperationOutcome awaiting = await gateway.ExecuteAsync(
            Copy([source], destination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        _ = root.WriteFile("destination\\report (2).txt", "racer");

        FileOperationOutcome raced = await gateway.ResumeAsync(
            awaiting.Continuation!,
            awaiting.Conflicts!,
            TransferConflictDecision.KeepBoth,
            TransferConflictScope.All,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.IsNotNull(raced.Conflicts);
        Assert.IsEmpty(raced.Effects);
        Assert.AreEqual(root.Resolve("destination\\report (3).txt"), raced.Conflicts.Conflicts[0].KeepBothCandidate.CanonicalText, ignoreCase: true);
        Assert.AreEqual("racer", File.ReadAllText(root.Resolve("destination\\report (2).txt")));
        Assert.AreEqual("source", File.ReadAllText(root.Resolve("report.txt")));
    }

    /// <summary>Proves Apply-to-all choices expire with their owning operation.</summary>
    [TestMethod]
    public async Task ExecuteAsyncAfterApplyToAllSkipStartsWithNoPriorResolution()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemPath source = PathOf(root.WriteFile("report.txt", "source"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        _ = root.WriteFile("destination\\report.txt", "existing");
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());
        CopyRequest request = Copy([source], destination);
        FileOperationOutcome firstAwaiting = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome skipped = await gateway.ResumeAsync(
            firstAwaiting.Continuation!,
            firstAwaiting.Conflicts!,
            TransferConflictDecision.Skip,
            TransferConflictScope.All,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        FileOperationOutcome nextOperation = await gateway.ExecuteAsync(
            request,
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, skipped.Completion);
        Assert.HasCount(1, skipped.NotTransferred);
        Assert.IsNotNull(nextOperation.Conflicts);
        Assert.IsEmpty(nextOperation.Effects);
    }

    /// <summary>Proves batch reservations use Windows case-insensitive target identity.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenBatchNamesDifferOnlyByCaseReturnsConflictBeforeCopy()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("one");
        _ = root.CreateDirectory("two");
        FileSystemPath first = PathOf(root.WriteFile("one\\Report.txt", "first"));
        FileSystemPath second = PathOf(root.WriteFile("two\\report.txt", "second"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            Copy([first, second], destination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.IsNotNull(outcome.Conflicts);
        Assert.IsEmpty(outcome.Effects);
        Assert.IsFalse(File.Exists(root.Resolve("destination\\Report.txt")));
    }

    /// <summary>Proves linked source trees and linked destinations fail during complete preflight.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public async Task ExecuteAsyncWhenTransferTouchesReparsePointRejectsBeforeEffect()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory("tree");
        _ = root.CreateDirectory("link-target");
        _ = root.CreateJunction("tree\\nested-link", "link-target");
        FileSystemPath source = PathOf(root.Resolve("tree"));
        FileSystemPath destination = PathOf(root.CreateDirectory("destination"));
        FileSystemPath linkedDestination = PathOf(root.CreateJunction("destination-link", "destination"));
        using FileOperationGateway gateway = new(new WindowsLocalFileOperationAdapter());

        FileOperationOutcome linkedSource = await gateway.ExecuteAsync(
            Copy([source], destination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);
        FileOperationOutcome linkedTarget = await gateway.ExecuteAsync(
            Copy([PathOf(root.WriteFile("plain.txt", "plain"))], linkedDestination),
            IgnoredFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linkedSource.Failure);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, linkedTarget.Failure);
        Assert.IsEmpty(linkedSource.Effects);
        Assert.IsEmpty(linkedTarget.Effects);
    }

    private static CopyRequest Copy(FileSystemPath[] sources, FileSystemPath destination)
    {
        return (CopyRequest)((FileOperationRequestAccepted)CopyRequest.Create(sources, destination)).Request;
    }

    private static FileSystemPath PathOf(string text)
    {
        return ((PathParseSuccess)FileSystemPath.Parse(text)).Path;
    }
}
