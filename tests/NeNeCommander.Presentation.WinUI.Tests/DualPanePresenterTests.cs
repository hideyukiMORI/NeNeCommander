using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the projection of both panes, their activation frames, and the operation status.</summary>
[TestClass]
public sealed class DualPanePresenterTests
{
    /// <summary>Proves the active side gets the active frame and both panes are projected.</summary>
    [TestMethod]
    public async Task PresentWhenLeftIsActiveFramesLeftActiveAndRightPassive()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Cancelled());
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        DualPaneSnapshot snapshot = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        DualPanePresentation presentation = DualPanePresenter.Present(snapshot);

        Assert.AreSame(PaneFrame.Active, presentation.LeftFrame);
        Assert.AreSame(PaneFrame.Passive, presentation.RightFrame);
        Assert.AreSame(PaneSide.Left, presentation.ActiveSide);
        Assert.AreSame(leftListing.Entries[0], presentation.Left.Rows[0].Entry);
        Assert.AreSame(PaneStatus.Cancelled, presentation.Right.Status);
        Assert.AreEqual("C:\\right", presentation.Right.AddressText);
        Assert.AreSame(OperationStatus.Idle, presentation.OperationStatus);
        Assert.AreSame(OperationDetail.None, presentation.Detail);
        Assert.AreSame(KeyboardContext.FileList, presentation.InputContext);
    }

    /// <summary>Proves activation swaps the frames without changing either pane's rows.</summary>
    [TestMethod]
    public async Task PresentWhenRightIsActiveFramesRightActiveAndLeftPassive()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort _, out ScriptedDirectoryReadPort _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPanePresentation presentation = DualPanePresenter.Present(snapshot);

        Assert.AreSame(PaneFrame.Passive, presentation.LeftFrame);
        Assert.AreSame(PaneFrame.Active, presentation.RightFrame);
        Assert.AreSame(PaneSide.Right, presentation.ActiveSide);
        Assert.AreSame(PaneStatus.NoListing, presentation.Left.Status);
        Assert.AreSame(PaneStatus.NoListing, presentation.Right.Status);
    }

    /// <summary>Proves a request the gateway never receives is shown as a rejected request.</summary>
    [TestMethod]
    public async Task PresentWhenMoveRequestIsRejectedShowsRequestRejectedStatus()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\", ["Users"])));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\Users", [])));
        _ = await panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\Users"), CancellationToken.None);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(OperationStatus.MoveRequestRejected, DualPanePresenter.Present(snapshot).OperationStatus);
    }

    /// <summary>Proves each gateway completion and the running state map to one operation status.</summary>
    [TestMethod]
    public async Task PresentWhenMoveCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.MoveSucceeded, await MoveStatusAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.MoveRejected, await MoveStatusAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)), CancellationToken.None));
        Assert.AreSame(OperationStatus.MovePartiallyCompleted, await MoveStatusAsync(port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Verification));
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.MoveCancelled, await MoveStatusAsync(port => { }, cancellation.Token));
    }

    private static async Task<OperationStatus> MoveStatusAsync(
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.Recycle)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot).OperationStatus;
    }
    /// <summary>Proves each copy completion maps to one copy status and a rejected copy request is named.</summary>
    [TestMethod]
    public async Task PresentWhenCopyCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.CopySucceeded, await OperationStatusAsync(UserIntent.Copy, port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.CopyRejected, await OperationStatusAsync(UserIntent.Copy, port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)), CancellationToken.None));
        Assert.AreSame(OperationStatus.CopyPartiallyCompleted, await OperationStatusAsync(UserIntent.Copy, port =>
        {
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Succeeded());
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Verification));
        }, CancellationToken.None));
        Assert.AreSame(OperationStatus.CopyCancelled, await OperationStatusAsync(UserIntent.Copy, port => { }, cancellation.Token));
    }

    /// <summary>Proves a copy request the gateway never receives is shown as a rejected copy request.</summary>
    [TestMethod]
    public async Task PresentWhenCopyRequestIsRejectedShowsCopyRequestRejectedStatus()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\", ["Users"])));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\Users", [])));
        _ = await panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\Users"), CancellationToken.None);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(OperationStatus.CopyRequestRejected, DualPanePresenter.Present(snapshot).OperationStatus);
    }

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
    }

    /// <summary>Proves the observer receives a presentation-ready snapshot whose detail counts the completed source.</summary>
    [TestMethod]
    public async Task PresentWhenProgressIsReportedProjectsCompletedAndTotal()
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
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
        DualPanePresentation presentation = DualPanePresenter.Present(observer.Snapshots[0]);
        Assert.AreSame(OperationStatus.Copying, presentation.OperationStatus);
        OperationProgressDetail detail = Assert.IsInstanceOfType<OperationProgressDetail>(presentation.Detail);
        Assert.AreEqual(1, detail.Completed);
        Assert.AreEqual(1, detail.Total);
    }

    private static async Task<DualPanePresentation> RunningAsync(UserIntent intent)
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        TaskCompletionSource<FileInspectionOutcome> pending = port.EnqueuePendingInspection();

        Task<DualPaneSnapshot> running = panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(panes.Current);

        pending.SetResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        _ = await running;
        return presentation;
    }

    private static async Task<OperationStatus> OperationStatusAsync(
        UserIntent intent,
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.Recycle)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot).OperationStatus;
    }

    /// <summary>Proves the name-entry state is projected as modal input with an active editor and no detail.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public async Task PresentWhenCreateDirectoryAwaitsNameReportsModalContextAndActiveEditor()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\left", ["a.txt"])));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\right", [])));
        _ = await panes.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);

        DualPanePresentation awaiting = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None));
        DualPanePresentation escaped = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None));

        Assert.AreSame(OperationStatus.CreateDirectoryAwaitingName, awaiting.OperationStatus);
        Assert.AreSame(KeyboardContext.Modal, awaiting.InputContext);
        Assert.AreSame(NameEntryPresentation.Active, awaiting.NameEntry);
        Assert.AreSame(OperationDetail.None, awaiting.Detail);
        Assert.AreSame(NameEntryPresentation.Hidden, escaped.NameEntry);
        Assert.AreSame(KeyboardContext.FileList, escaped.InputContext);
    }

    /// <summary>Proves each directory-creation completion, the invalid-name rejection, and the running state map to their statuses.</summary>
    [TestMethod]
    public async Task PresentWhenDirectoryCreationCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.DirectoryCreated, await CreateDirectoryStatusAsync("new", port =>
            port.EnqueueStep(ProviderStepOutcome.Succeeded()), CancellationToken.None));
        Assert.AreSame(OperationStatus.CreateDirectoryRejected, await CreateDirectoryStatusAsync("new", port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)), CancellationToken.None));
        Assert.AreSame(OperationStatus.CreateDirectoryCancelled, await CreateDirectoryStatusAsync("new", port => { }, cancellation.Token));
        Assert.AreSame(OperationStatus.CreateDirectoryRequestRejected, await CreateDirectoryStatusAsync("..", port => { }, CancellationToken.None));
    }

    /// <summary>Proves the running directory creation is projected with its own status and zero progress.</summary>
    [TestMethod]
    public async Task PresentWhenDirectoryCreationIsRunningNamesTheRunningKind()
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        TaskCompletionSource<FileInspectionOutcome> pending = port.EnqueuePendingInspection();

        Task<DualPaneSnapshot> running = panes.HandleAsync(UserIntent.SubmitName("new"), RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(panes.Current);
        pending.SetResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        _ = await running;

        Assert.AreSame(OperationStatus.CreatingDirectory, presentation.OperationStatus);
        Assert.AreSame(NameEntryPresentation.Hidden, presentation.NameEntry);
        Assert.AreEqual(0, Assert.IsInstanceOfType<OperationProgressDetail>(presentation.Detail).Completed);
    }

    private static async Task<OperationStatus> CreateDirectoryStatusAsync(
        string name,
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Location, identity.Identity, DeletionCapability.PermanentOnly)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.SubmitName(name), RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot).OperationStatus;
    }

    /// <summary>Proves a pending confirmation is projected as modal input with its item count.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task PresentWhenDeleteAwaitsConfirmationReportsModalContextAndCount()
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt", "b.txt"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\right", [])));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        foreach (DirectoryEntry entry in leftListing.Entries)
        {
            port.EnqueueInspection(FileInspectionOutcome.Succeeded(
                FileEntrySnapshot.Create(entry.Path, identity.Identity, DeletionCapability.PermanentOnly)));
        }

        DualPanePresentation pending = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None));
        DualPanePresentation escaped = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None));

        Assert.AreSame(OperationStatus.DeleteAwaitingConfirmation, pending.OperationStatus);
        Assert.AreEqual(2, Assert.IsInstanceOfType<OperationItemCountDetail>(pending.Detail).Count);
        Assert.AreSame(KeyboardContext.Modal, pending.InputContext);
        Assert.AreSame(OperationStatus.Idle, escaped.OperationStatus);
        Assert.AreSame(OperationDetail.None, escaped.Detail);
        Assert.AreSame(KeyboardContext.FileList, escaped.InputContext);
    }

    /// <summary>Proves each delete completion and the running state map to one operation status.</summary>
    [TestMethod]
    public async Task PresentWhenDeleteCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.DeleteSucceeded, await DeleteStatusAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Succeeded()), CancellationToken.None));
        Assert.AreSame(OperationStatus.DeleteRejected, await DeleteStatusAsync(port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.AccessDenied)), CancellationToken.None));
        Assert.AreSame(OperationStatus.DeleteCancelled, await DeleteStatusAsync(port => { }, cancellation.Token));
    }

    /// <summary>Proves a partially completed deletion and the running state map to their statuses.</summary>
    [TestMethod]
    public async Task PresentWhenDeleteStopsAfterPartialCompletionReportsPartialStatus()
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt", "b.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
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

        DualPanePresentation presentation = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None));

        Assert.AreSame(OperationStatus.DeletePartiallyCompleted, presentation.OperationStatus);
    }

    private static async Task<OperationStatus> DeleteStatusAsync(
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = CreateListing("C:\\left", ["a.txt"]);
        DirectoryListing rightListing = CreateListing("C:\\right", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.Recycle)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot).OperationStatus;
    }
    /// <summary>Proves each frame names distinct semantic border resources.</summary>
    [TestMethod]
    public void ResourceKeysWhenFrameIsReadNameExactSemanticResources()
    {
        Assert.AreEqual("FocusRingBrush", PaneFrame.Active.BrushResourceKey);
        Assert.AreEqual("BorderActivePaneThickness", PaneFrame.Active.ThicknessResourceKey);
        Assert.AreEqual("BorderSubtleBrush", PaneFrame.Passive.BrushResourceKey);
        Assert.AreEqual("BorderPassivePaneThickness", PaneFrame.Passive.ThicknessResourceKey);
    }

    /// <summary>Proves every operation status names a distinct localization resource key.</summary>
    [TestMethod]
    public void ResourceKeyWhenOperationStatusIsReadNamesExactResource()
    {
        Assert.AreEqual("OperationStatusIdle", OperationStatus.Idle.ResourceKey);
        Assert.AreEqual("OperationStatusMoving", OperationStatus.Moving.ResourceKey);
        Assert.AreEqual("OperationStatusMoveSucceeded", OperationStatus.MoveSucceeded.ResourceKey);
        Assert.AreEqual("OperationStatusMoveCancelled", OperationStatus.MoveCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusMovePartiallyCompleted", OperationStatus.MovePartiallyCompleted.ResourceKey);
        Assert.AreEqual("OperationStatusMoveRejected", OperationStatus.MoveRejected.ResourceKey);
        Assert.AreEqual("OperationStatusMoveRequestRejected", OperationStatus.MoveRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCopying", OperationStatus.Copying.ResourceKey);
        Assert.AreEqual("OperationStatusCopySucceeded", OperationStatus.CopySucceeded.ResourceKey);
        Assert.AreEqual("OperationStatusCopyCancelled", OperationStatus.CopyCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusCopyPartiallyCompleted", OperationStatus.CopyPartiallyCompleted.ResourceKey);
        Assert.AreEqual("OperationStatusCopyRejected", OperationStatus.CopyRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCopyRequestRejected", OperationStatus.CopyRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCreatingDirectory", OperationStatus.CreatingDirectory.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryAwaitingName", OperationStatus.CreateDirectoryAwaitingName.ResourceKey);
        Assert.AreEqual("OperationStatusDirectoryCreated", OperationStatus.DirectoryCreated.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryCancelled", OperationStatus.CreateDirectoryCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryRejected", OperationStatus.CreateDirectoryRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryRequestRejected", OperationStatus.CreateDirectoryRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusDeleting", OperationStatus.Deleting.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteAwaitingConfirmation", OperationStatus.DeleteAwaitingConfirmation.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteSucceeded", OperationStatus.DeleteSucceeded.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteCancelled", OperationStatus.DeleteCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusDeletePartiallyCompleted", OperationStatus.DeletePartiallyCompleted.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteRejected", OperationStatus.DeleteRejected.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteRequestRejected", OperationStatus.DeleteRequestRejected.ResourceKey);
    }

    /// <summary>Proves the presenter rejects an absent snapshot.</summary>
    [TestMethod]
    public void PresentWhenSnapshotIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(DualPanePresenter).GetMethod(
            nameof(DualPanePresenter.Present),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The present method was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static DualPaneSession CreatePanes(
        out ScriptedDirectoryReadPort left,
        out ScriptedDirectoryReadPort right,
        out FileOperationGateway gateway)
    {
        return CreatePanes(out left, out right, out gateway, out QueuedFileOperationPort _);
    }

    private static DualPaneSession CreatePanes(
        out ScriptedDirectoryReadPort left,
        out ScriptedDirectoryReadPort right,
        out FileOperationGateway gateway,
        out QueuedFileOperationPort port)
    {
        left = ScriptedDirectoryReadPort.Create();
        right = ScriptedDirectoryReadPort.Create();
        port = QueuedFileOperationPort.Create();
        gateway = new FileOperationGateway(port);
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        return new DualPaneSession(
            new PaneSession(left, capacity, DirectoryListing.EntryBoundaryLimit),
            new PaneSession(right, capacity, DirectoryListing.EntryBoundaryLimit),
            gateway);
    }

    private static DirectoryListing CreateListing(string location, string[] names)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        string separator = parsedLocation.CanonicalText.EndsWith('\\') ? string.Empty : "\\";
        DirectoryEntry[] entries = new DirectoryEntry[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            entries[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + separator + names[index]),
                names[index],
                DirectoryEntryKind.Directory);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            entries,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
