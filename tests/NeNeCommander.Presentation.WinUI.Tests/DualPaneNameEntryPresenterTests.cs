using System;
using System.Reflection;
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

/// <summary>Proves DualPaneNameEntryPresenter behavior and contracts.</summary>
[TestClass]
public sealed class DualPaneNameEntryPresenterTests
{
    /// <summary>Proves the name-entry state is projected as modal input with an active editor and no detail.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public async Task PresentWhenCreateDirectoryAwaitsNameReportsModalContextAndActiveEditor()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\right", []), null));
        _ = await panes.NavigateAsync(PaneSide.Left, DualPaneTestContext.ParsePath("C:\\left"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\right"), CancellationToken.None);

        DualPanePresentation awaiting = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);
        DualPanePresentation escaped = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);

        Assert.AreSame(OperationStatus.CreateDirectoryAwaitingName, awaiting.OperationStatus);
        Assert.AreSame(KeyboardContext.Modal, awaiting.InputContext);
        Assert.AreSame(OperationBarTone.AwaitingName, awaiting.Tone);
        Assert.HasCount(2, awaiting.KeyHints);
        Assert.AreSame(OperationBarTone.Idle, escaped.Tone);
        Assert.AreEqual(string.Empty, Assert.IsInstanceOfType<ActiveNameEntry>(awaiting.NameEntry).InitialText);
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
        _ = await panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        TaskCompletionSource<FileInspectionOutcome> pending = port.EnqueuePendingInspection();

        Task<DualPaneSnapshot> running = panes.HandleAsync(UserIntent.SubmitName("new"), RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc);
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
        _ = await panes.HandleAsync(UserIntent.CreateDirectory, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Location, identity.Identity, DeletionCapability.PermanentOnly)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.SubmitName(name), RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc).OperationStatus;
    }

    /// <summary>Proves the rename name entry is projected as modal input with the focus item's current name.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public async Task PresentWhenRenameAwaitsNameReportsModalContextAndCurrentName()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\right", []), null));
        _ = await panes.NavigateAsync(PaneSide.Left, DualPaneTestContext.ParsePath("C:\\left"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\right"), CancellationToken.None);

        DualPanePresentation awaiting = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);
        DualPanePresentation escaped = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);

        Assert.AreSame(OperationStatus.RenameAwaitingName, awaiting.OperationStatus);
        Assert.AreEqual(string.Empty, awaiting.DeletionConfirmationAutomationId);
        Assert.AreSame(KeyboardContext.Modal, awaiting.InputContext);
        Assert.AreEqual("a.txt", Assert.IsInstanceOfType<ActiveNameEntry>(awaiting.NameEntry).InitialText);
        Assert.AreSame(OperationDetail.None, awaiting.Detail);
        Assert.AreSame(NameEntryPresentation.Hidden, escaped.NameEntry);
        Assert.AreSame(KeyboardContext.FileList, escaped.InputContext);
    }

    /// <summary>Proves each rename completion and the invalid-name rejection map to their own statuses.</summary>
    [TestMethod]
    public async Task PresentWhenRenameCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.Renamed, await RenameStatusAsync("b.txt", port =>
            port.EnqueueStep(ProviderStepOutcome.Succeeded()), CancellationToken.None));
        Assert.AreSame(OperationStatus.RenameRejected, await RenameStatusAsync("b.txt", port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)), CancellationToken.None));
        Assert.AreSame(OperationStatus.RenameCancelled, await RenameStatusAsync("b.txt", port => { }, cancellation.Token));
        Assert.AreSame(OperationStatus.RenameRequestRejected, await RenameStatusAsync("..", port => { }, CancellationToken.None));
    }

    /// <summary>Proves the running rename is projected with its own status and zero progress.</summary>
    [TestMethod]
    public async Task PresentWhenRenameIsRunningNamesTheRunningKind()
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
        _ = await panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        TaskCompletionSource<FileInspectionOutcome> pending = port.EnqueuePendingInspection();

        Task<DualPaneSnapshot> running = panes.HandleAsync(UserIntent.SubmitName("b.txt"), RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc);
        pending.SetResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        _ = await running;

        Assert.AreSame(OperationStatus.Renaming, presentation.OperationStatus);
        Assert.AreSame(NameEntryPresentation.Hidden, presentation.NameEntry);
        Assert.AreEqual(0, Assert.IsInstanceOfType<OperationProgressDetail>(presentation.Detail).Completed);
    }

    private static async Task<OperationStatus> RenameStatusAsync(
        string name,
        Action<QueuedFileOperationPort> script,
        CancellationToken cancellationToken)
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
        _ = await panes.HandleAsync(UserIntent.Rename, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Entries[0].Path, identity.Identity, DeletionCapability.PermanentOnly)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.SubmitName(name), RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc).OperationStatus;
    }

    /// <summary>Proves the active name entry rejects absent initial text.</summary>
    [TestMethod]
    public void ConstructActiveNameEntryWhenInitialTextIsNullThrowsArgumentNullException()
    {
        ConstructorInfo constructor = typeof(ActiveNameEntry).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [typeof(string)]) ??
            throw new AssertFailedException("The active name entry constructor was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => constructor.Invoke([null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
}
