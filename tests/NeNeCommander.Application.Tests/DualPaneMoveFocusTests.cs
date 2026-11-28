using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves move completion recovers focus from the remaining source listing on either side.</summary>
[TestClass]
public sealed class DualPaneMoveFocusTests
{
    /// <summary>Proves one moved middle item focuses its surviving successor in the active pane only.</summary>
    [TestMethod]
    [DataRow("left")]
    [DataRow("right")]
    public async Task HandleAsyncWhenMiddleItemMovesFocusesSourceNeighbour(string activeSide)
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = activeSide == "left" ? Listing("left", "a", "b", "c", "d") : Listing("left", "p", "q");
        DirectoryListing right = activeSide == "right" ? Listing("right", "a", "b", "c", "d") : Listing("right", "p", "q");
        await fixture.ListBothAsync(left, right);
        PaneSide active = activeSide == "left" ? PaneSide.Left : PaneSide.Right;
        if (active == PaneSide.Right)
        {
            _ = await fixture.Panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        }
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DirectoryListing source = active == PaneSide.Left ? left : right;
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(source.Entries[1].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        EnqueueSuccessfulMove(fixture.Port);
        DirectoryListing remaining = Listing(activeSide, "a", "c", "d");
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(active == PaneSide.Left ? remaining : Listing("left", "b", "p", "q"), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(active == PaneSide.Right ? remaining : Listing("right", "b", "p", "q"), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        PaneSnapshot sourcePane = active == PaneSide.Left ? snapshot.Left : snapshot.Right;
        PaneSnapshot passivePane = active == PaneSide.Left ? snapshot.Right : snapshot.Left;
        Assert.AreSame(remaining.Entries[1].Path, DualPaneSessionTestContext.Focus(sourcePane));
        Assert.AreEqual("p", Assert.IsInstanceOfType<PaneContentListed>(passivePane.Content).FindFocusedEntry()?.Name);
        Assert.AreSame(active, snapshot.ActiveSide);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome.Completion);
    }

    /// <summary>Proves a failed second source retains that source and focuses it after the first was removed.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMovePartlyFailsFocusesNextRemainingSelectedItem()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing before = Listing("left", "a", "b", "c", "d");
        await fixture.ListBothAsync(before, Listing("right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MovePrevious, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(before.Entries[1].Path));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(before.Entries[2].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        EnqueueSuccessfulMove(fixture.Port);
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.AccessDenied), ProviderStepReceipt.Empty));
        DirectoryListing remaining = Listing("left", "a", "c", "d");
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(remaining, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("right", "b"), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(remaining.Entries[1].Path, DualPaneSessionTestContext.Focus(snapshot.Left));
        PaneState state = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Left.Content).State;
        Assert.IsEmpty(state.Selection);
        FileOperationOutcome outcome = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome;
        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreEqual(1, outcome.Effects.Count(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
    }

    /// <summary>Proves cancellation after the first selected removal uses the actual remaining source listing.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenMoveCancelsAfterRemovalFocusesNextUnselectedSurvivor()
    {
        DualPaneSessionTestContext? running = null;
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create(
            ScriptedCallbackPoint.AfterDeletion,
            () => _ = running?.Panes.HandleAsync(UserIntent.Escape, RecordingDualPaneObserver.Create(), CancellationToken.None));
        running = fixture;
        DirectoryListing before = Listing("left", "a", "b", "c", "d", "e");
        await fixture.ListBothAsync(before, Listing("right"));
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MovePrevious, RecordingDualPaneObserver.Create(), CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MovePrevious, RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(before.Entries[1].Path));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(before.Entries[3].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        EnqueueSuccessfulMove(fixture.Port);
        DirectoryListing remaining = Listing("left", "a", "c", "d", "e");
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(remaining, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(Listing("right", "b"), null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Move, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(remaining.Entries[1].Path, DualPaneSessionTestContext.Focus(snapshot.Left));
        FileOperationOutcome outcome = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation).Outcome;
        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.AreEqual(1, outcome.Effects.Count(effect => effect.Kind == FileOperationEffectKind.SourceDeleted));
        Assert.IsFalse(fixture.Port.Calls.Any(call => call == "Copy:C:\\left\\d"));
    }

    private static void EnqueueSuccessfulMove(ScriptedFileOperationPort port)
    {
        port.EnqueueCopy(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
    }

    private static DirectoryListing Listing(string side, params string[] names)
    {
        return DualPaneSessionTestContext.Listing("C:\\" + side,
            [.. names.Select(name => (name, DirectoryEntryKind.File))]);
    }
}
