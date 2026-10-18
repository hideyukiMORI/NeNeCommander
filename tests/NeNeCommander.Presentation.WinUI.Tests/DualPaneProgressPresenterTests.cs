using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPaneProgressPresenter behavior and contracts.</summary>
[TestClass]
public sealed class DualPaneProgressPresenterTests
{
    /// <summary>Proves each running operation kind is projected as its own running status with zero progress while the gateway works.</summary>
    [TestMethod]
    public async Task PresentWhenOperationIsRunningNamesTheRunningKindAndProgress()
    {
        Assert.AreSame(OperationStatus.Moving, (await RunningAsync(UserIntent.Move)).OperationStatus);
        Assert.AreSame(OperationStatus.Copying, (await RunningAsync(UserIntent.Copy)).OperationStatus);
        DualPanePresentation deleting = await RunningAsync(UserIntent.Delete);

        Assert.AreSame(OperationStatus.Deleting, deleting.OperationStatus);
        OperationProgressDetail detail = Assert.IsInstanceOfType<OperationProgressDetail>(deleting.Detail);
        Assert.AreEqual(0, detail.Completed);
        Assert.AreEqual(1, detail.Total);
        Assert.AreSame(KeyboardContext.FileList, deleting.InputContext);
        Assert.AreSame(OperationBarTone.Idle, deleting.Tone);
        Assert.HasCount(OperationProgressDetail.SegmentCount, detail.Segments);
        Assert.AreSame(ProgressSegment.Empty, detail.Segments[0]);
        Assert.AreSame(ProgressSegment.Empty, detail.Segments[OperationProgressDetail.SegmentCount - 1]);
    }

    /// <summary>Proves the observer receives a presentation-ready snapshot whose detail counts the completed source.</summary>
    [TestMethod]
    public async Task PresentWhenProgressIsReportedProjectsCompletedAndTotal()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.Recycle)));
        port.EnqueueStep(ProviderStepOutcome.Succeeded());
        port.EnqueueStep(ProviderStepOutcome.Succeeded());
        port.EnqueueStep(ProviderStepOutcome.Succeeded());
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();

        _ = await panes.HandleAsync(UserIntent.Copy, observer, CancellationToken.None);

        Assert.HasCount(1, observer.Snapshots);
        DualPanePresentation presentation = DualPanePresenter.Present(observer.Snapshots[0], TestMetadataFormats.Utc);
        Assert.AreSame(OperationStatus.Copying, presentation.OperationStatus);
        OperationProgressDetail detail = Assert.IsInstanceOfType<OperationProgressDetail>(presentation.Detail);
        Assert.AreEqual(1, detail.Completed);
        Assert.AreEqual(1, detail.Total);
        Assert.AreSame(ProgressSegment.Filled, detail.Segments[0]);
        Assert.AreSame(ProgressSegment.Filled, detail.Segments[OperationProgressDetail.SegmentCount - 1]);
    }

    private static async Task<DualPanePresentation> RunningAsync(UserIntent intent)
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        TaskCompletionSource<FileInspectionOutcome> pending = port.EnqueuePendingInspection();

        Task<DualPaneSnapshot> running = panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc);

        pending.SetResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        _ = await running;
        return presentation;
    }

    /// <summary>Proves a partly completed transfer fills exactly the completed proportion of the bar.</summary>
    [TestMethod]
    public async Task PresentWhenHalfTheSourcesCompletedFillsHalfTheSegments()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt", "b.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        foreach (DirectoryEntry entry in leftListing.Entries)
        {
            port.EnqueueInspection(FileInspectionOutcome.Succeeded(
                FileEntrySnapshot.Create(entry.Path, identity.Identity, DeletionCapability.Recycle)));
        }
        port.EnqueueStep(ProviderStepOutcome.Succeeded());
        port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Delete));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        DualPanePresentation beforeProgress = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc);

        _ = await panes.HandleAsync(UserIntent.Delete, observer, CancellationToken.None);

        DualPanePresentation duringProgress = DualPanePresenter.Present(observer.Snapshots[0], TestMetadataFormats.Utc, beforeProgress);
        OperationProgressDetail detail = Assert.IsInstanceOfType<OperationProgressDetail>(
            duringProgress.Detail);
        Assert.AreSame(beforeProgress.Left.Rows, duringProgress.Left.Rows);
        Assert.AreSame(beforeProgress.Right.Rows, duringProgress.Right.Rows);
        Assert.AreEqual(1, detail.Completed);
        Assert.AreEqual(2, detail.Total);
        Assert.AreSame(ProgressSegment.Filled, detail.Segments[5]);
        Assert.AreSame(ProgressSegment.Empty, detail.Segments[6]);
    }
}
