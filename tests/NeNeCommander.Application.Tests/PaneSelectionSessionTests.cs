using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves selection storage preserves session publication and empty consumer behavior.</summary>
[TestClass]
public sealed class PaneSelectionSessionTests
{
    /// <summary>Proves reference-based publication continues when an unchanged focus target compares equal.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFocusTargetIsUnchangedPublishesNewSnapshot()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\selection",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot selected = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);

        PaneSnapshot sameFocus = await session.HandleAsync(UserIntent.FocusFirst, CancellationToken.None);

        PaneState originalState = Assert.IsInstanceOfType<PaneContentListed>(selected.Content).State;
        PaneState sameFocusState = Assert.IsInstanceOfType<PaneContentListed>(sameFocus.Content).State;
        Assert.AreNotSame(selected, sameFocus);
        Assert.AreNotSame(originalState, sameFocusState);
        Assert.AreEqual(originalState, sameFocusState);
        Assert.AreSame(originalState.Selection, sameFocusState.Selection);
        Assert.AreSame(sameFocus, session.Current);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves deletion freezes and executes the selected paths after focus moves elsewhere.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenDeleteHasSelectionConfirmsAndDeletesOnlySelectedPaths()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing listing = DualPaneSessionTestContext.Listing("C:\\left",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File), ("c.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(listing, DualPaneSessionTestContext.Listing("C:\\right"));
        RecordingDualPaneObserver observer = RecordingDualPaneObserver.Create();
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.MoveNext, observer, CancellationToken.None);
        _ = await fixture.Panes.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        DualPaneSnapshot selected = await fixture.Panes.HandleAsync(UserIntent.MoveNext, observer, CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[0].Path, DeletionCapability.PermanentOnly));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[1].Path, DeletionCapability.PermanentOnly));

        DualPaneSnapshot pending = await fixture.Panes.HandleAsync(UserIntent.Delete, observer, CancellationToken.None);
        OperationAwaitingConfirmation awaiting = Assert.IsInstanceOfType<OperationAwaitingConfirmation>(pending.Operation);
        Assert.HasCount(2, fixture.Port.Calls);
        Assert.HasCount(2, awaiting.Request.Sources);
        Assert.AreSame(listing.Entries[0].Path, awaiting.Request.Sources[0]);
        Assert.AreSame(listing.Entries[1].Path, awaiting.Request.Sources[1]);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[0].Path, DeletionCapability.PermanentOnly));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(listing.Entries[1].Path, DeletionCapability.PermanentOnly));
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        fixture.Port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left", ("c.txt", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\right"), null));
        DualPaneSnapshot confirmed = await fixture.Panes.HandleAsync(UserIntent.Confirm, observer, CancellationToken.None);

        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(confirmed.Operation);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.HasCount(6, fixture.Port.Calls);
        Assert.AreEqual("Delete:C:\\left\\a.txt", fixture.Port.Calls[4]);
        Assert.AreEqual("Delete:C:\\left\\b.txt", fixture.Port.Calls[5]);
        PaneState oldState = Assert.IsInstanceOfType<PaneContentListed>(selected.Left.Content).State;
        Assert.AreSame(listing.Entries[2].Path, oldState.FocusItem);
        Assert.HasCount(2, oldState.Selection);
        Assert.IsEmpty(Assert.IsInstanceOfType<PaneContentListed>(confirmed.Left.Content).State.Selection);
    }

    /// <summary>Proves initial and cleared empty selections support both collection enumerators.</summary>
    [TestMethod]
    public void SelectionWhenEmptyHasNoElementsAndCopiesNothing()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\empty", ("a.txt", DirectoryEntryKind.File));
        PaneState initial = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        PaneState selected = PaneReducer.Apply(initial, UserIntent.ToggleSelection);
        PaneState cleared = PaneReducer.Apply(selected, UserIntent.Escape);

        foreach (IReadOnlyList<FileSystemPath> selection in new[] { initial, cleared }.Select(state => state.Selection))
        {
            ICollection<FileSystemPath> collection = Assert.IsInstanceOfType<ICollection<FileSystemPath>>(selection);
            FileSystemPath[] destination = [listing.Entries[0].Path];
            collection.CopyTo(destination, 0);
            using IEnumerator<FileSystemPath> generic = selection.GetEnumerator();
            IEnumerable untyped = selection;

            Assert.AreEqual(0, collection.Count);
            Assert.IsFalse(generic.MoveNext());
            Assert.IsFalse(untyped.GetEnumerator().MoveNext());
            Assert.IsFalse(collection.Contains(listing.Entries[0].Path));
            Assert.AreSame(listing.Entries[0].Path, destination[0]);
        }
    }
}
