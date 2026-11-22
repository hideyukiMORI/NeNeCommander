using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPaneNameEntryPresenter behavior and contracts.</summary>
[TestClass]
public sealed class TextFileNameEntryPresenterTests
{
    /// <summary>Proves the name-entry state is projected as modal input with an active editor and no detail.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public async Task PresentWhenCreateTextFileAwaitsNameReportsModalContextAndActiveEditor()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]), null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing("C:\\right", []), null));
        _ = await panes.NavigateAsync(PaneSide.Left, DualPaneTestContext.ParsePath("C:\\left"), CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\right"), CancellationToken.None);

        DualPanePresentation awaiting = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.CreateTextFile, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc, KeyHintVisibility.Shown);
        DualPanePresentation escaped = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc, KeyHintVisibility.Shown);

        Assert.AreSame(OperationStatus.CreateTextFileAwaitingName, awaiting.OperationStatus);
        Assert.AreSame(KeyboardContext.Modal, awaiting.InputContext);
        Assert.AreSame(OperationBarTone.AwaitingName, awaiting.Tone);
        Assert.HasCount(2, awaiting.KeyHints);
        Assert.AreSame(OperationBarTone.Idle, escaped.Tone);
        NameEntryInitialText initial = Assert.IsInstanceOfType<ActiveNameEntry>(awaiting.NameEntry).InitialText;
        Assert.AreEqual("New text file.txt", initial.Resolve("New text file.txt"));
        Assert.AreEqual("新しいテキスト.txt", initial.Resolve("新しいテキスト.txt"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => initial.Resolve(null!));
        Assert.AreSame(OperationDetail.None, awaiting.Detail);
        Assert.AreSame(NameEntryPresentation.Hidden, escaped.NameEntry);
        Assert.AreSame(KeyboardContext.FileList, escaped.InputContext);
    }

    /// <summary>Proves each text-file creation completion, the invalid-name rejection, and the running state map to their statuses.</summary>
    [TestMethod]
    public async Task PresentWhenTextFileCreationCompletesTranslatesEachCompletionKind()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Assert.AreSame(OperationStatus.TextFileCreated, await CreateTextFileStatusAsync("new", port =>
            port.EnqueueStep(ProviderStepOutcome.Succeeded()), CancellationToken.None));
        Assert.AreSame(OperationStatus.CreateTextFileRejected, await CreateTextFileStatusAsync("new", port =>
            port.EnqueueStep(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)), CancellationToken.None));
        Assert.AreSame(OperationStatus.CreateTextFileCancelled, await CreateTextFileStatusAsync("new", port => { }, cancellation.Token));
        Assert.AreSame(OperationStatus.CreateTextFileRequestRejected, await CreateTextFileStatusAsync("..", port => { }, CancellationToken.None));
    }

    /// <summary>Proves the running text-file creation is projected with its own status and zero progress.</summary>
    [TestMethod]
    public async Task PresentWhenTextFileCreationIsRunningNamesTheRunningKind()
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
        _ = await panes.HandleAsync(UserIntent.CreateTextFile, RecordingDualPaneObserver.Create(), CancellationToken.None);
        TaskCompletionSource<FileInspectionOutcome> pending = port.EnqueuePendingInspection();

        Task<DualPaneSnapshot> running = panes.HandleAsync(UserIntent.SubmitName("new"), RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation presentation = DualPanePresenter.Present(panes.Current, TestMetadataFormats.Utc, KeyHintVisibility.Shown);
        pending.SetResult(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        _ = await running;

        Assert.AreSame(OperationStatus.CreatingTextFile, presentation.OperationStatus);
        Assert.AreSame(NameEntryPresentation.Hidden, presentation.NameEntry);
        Assert.AreEqual(0, Assert.IsInstanceOfType<OperationProgressDetail>(presentation.Detail).Completed);
    }

    /// <summary>Proves literal seeds retain F7/F2 text and reject absent factory or localization inputs.</summary>
    [TestMethod]
    public void ResolveWhenInitialTextIsLiteralPreservesExactSeedAndGuardsNull()
    {
        NameEntryInitialText empty = NameEntryInitialText.Create(string.Empty);
        NameEntryInitialText literal = NameEntryInitialText.Create("existing.txt");

        Assert.AreEqual(string.Empty, empty.Resolve("新しいテキスト.txt"));
        Assert.AreEqual("existing.txt", literal.Resolve("New text file.txt"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => NameEntryInitialText.Create(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => literal.Resolve(null!));
    }

    private static async Task<OperationStatus> CreateTextFileStatusAsync(
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
        _ = await panes.HandleAsync(UserIntent.CreateTextFile, RecordingDualPaneObserver.Create(), CancellationToken.None);
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity"));
        port.EnqueueInspection(FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(leftListing.Location, identity.Identity, DeletionCapability.PermanentOnly)));
        script(port);

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.SubmitName(name), RecordingDualPaneObserver.Create(), cancellationToken);

        return DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).OperationStatus;
    }

}
