using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the created-file receipt and captured pane survive boundary events.</summary>
[TestClass]
public sealed class TextFileSessionCompletionTests
{
    /// <summary>Proves right-side creation ignores selected files, preserves its parent and cannot submit twice.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRightPaneCreatesTextFileFocusesExactChildOnlyOnce()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = DualPaneSessionTestContext.Listing(@"C:\left", ("keep.txt", DirectoryEntryKind.File));
        DirectoryListing right = DualPaneSessionTestContext.Listing(@"C:\right", ("old.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(left, right);
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, observer, CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.CreateTextFile, observer, CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(right.Location));
        fixture.Port.EnqueueFileCreation(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"C:\right",
            ("old.txt", DirectoryEntryKind.File), ("new.log", DirectoryEntryKind.File)), null));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(left, null));

        DualPaneSnapshot created = await fixture.Panes.HandleAsync(UserIntent.SubmitName("new.log"), observer, CancellationToken.None);
        DualPaneSnapshot duplicate = await fixture.Panes.HandleAsync(UserIntent.SubmitName("second.txt"), observer, CancellationToken.None);

        Assert.AreSame(PaneSide.Right, created.ActiveSide);
        Assert.AreEqual(@"C:\right\new.log", DualPaneSessionTestContext.Focus(created.Right)?.CanonicalText);
        Assert.AreSame(left.Entries[0].Path, DualPaneSessionTestContext.Focus(created.Left));
        Assert.AreEqual(created, duplicate);
        Assert.HasCount(2, fixture.Port.Calls);
        Assert.AreEqual(@"Inspect:C:\right", fixture.Port.Calls[0]);
        Assert.AreEqual(@"CreateEmptyFile:C:\right\new.log", fixture.Port.Calls[1]);
        Assert.HasCount(1, Assert.IsInstanceOfType<OperationCompleted>(created.Operation).Outcome.Effects);
    }

    /// <summary>Proves a failed listing cannot erase the successful atomic creation receipt.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRefreshFailsRetainsSuccessfulCreationReceipt()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = DualPaneSessionTestContext.Listing(@"C:\left");
        await fixture.ListBothAsync(left, DualPaneSessionTestContext.Listing(@"C:\right"));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        _ = await fixture.Panes.HandleAsync(UserIntent.CreateTextFile, observer, CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(left.Location));
        fixture.Port.EnqueueFileCreation(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        fixture.Left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        fixture.Right.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.NotFound));

        DualPaneSnapshot result = await fixture.Panes.HandleAsync(UserIntent.SubmitName("new.txt"), observer, CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(result.Operation);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.HasCount(1, completed.Outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.FileCreated, completed.Outcome.Effects[0].Kind);
        Assert.AreEqual(@"C:\left\new.txt", completed.Outcome.Effects[0].Source.CanonicalText);
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves cancellation during a completed atomic step cannot relabel it or trigger rollback.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task HandleAsyncWhenCreationCancelsAfterEffectRetainsSuccessfulReceipt()
    {
        using CancellationTokenSource cancellation = new();
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(ScriptedCallbackPoint.AfterFileCreation, cancellation.Cancel);
        DirectoryListing left = DualPaneSessionTestContext.Listing(@"C:\left");
        await fixture.ListBothAsync(left, DualPaneSessionTestContext.Listing(@"C:\right"));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        _ = await fixture.Panes.HandleAsync(UserIntent.CreateTextFile, observer, CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(left.Location));
        fixture.Port.EnqueueFileCreation(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        fixture.Left.Enqueue(DirectoryReadOutcome.Cancelled());
        fixture.Right.Enqueue(DirectoryReadOutcome.Cancelled());

        DualPaneSnapshot result = await fixture.Panes.HandleAsync(UserIntent.SubmitName("new.txt"), observer, cancellation.Token);

        Assert.IsTrue(cancellation.IsCancellationRequested);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(result.Operation);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.HasCount(1, completed.Outcome.Effects);
        Assert.HasCount(2, fixture.Port.Calls);
        Assert.IsEmpty(fixture.Port.DeletionModes);
    }
}
