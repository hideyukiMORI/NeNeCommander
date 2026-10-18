using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneCopySession behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneCopySessionTests
{
    /// <summary>Proves a copy takes the focus item to the passive location, deletes nothing, and refreshes both panes.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCopyHasNoSelectionCopiesFocusItemWithoutDeletingSource()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing leftListing = DualPaneSessionTestContext.Listing("C:\\left", ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(leftListing, DualPaneSessionTestContext.Listing("C:\\right"));
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(leftListing.Entries[0].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        DirectoryListing rightAfter = DualPaneSessionTestContext.Listing("C:\\right", ("a.txt", DirectoryEntryKind.File));
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(rightAfter, null));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.HasCount(4, fixture.Port.Calls);
        Assert.AreEqual("Preflight:C:\\right", fixture.Port.Calls[1]);
        Assert.AreEqual("Copy:C:\\left\\a.txt", fixture.Port.Calls[2]);
        Assert.AreEqual("Verify:C:\\left\\a.txt", fixture.Port.Calls[3]);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(snapshot.Operation);
        Assert.AreSame(OperationKind.Copy, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
        Assert.AreSame(rightAfter, Assert.IsInstanceOfType<PaneContentListed>(snapshot.Right.Content).Listing);
        Assert.AreSame(leftListing.Entries[0].Path, DualPaneSessionTestContext.Focus(snapshot.Left));
        Assert.HasCount(2, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a copy whose destination is one of its sources is rejected before the gateway.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCopyTargetsItsOwnSourceRecordsRequestRejection()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await fixture.ListBothAsync(DualPaneSessionTestContext.Listing("C:\\", ("Users", DirectoryEntryKind.Directory)), DualPaneSessionTestContext.Listing("C:\\Users"));

        DualPaneSnapshot snapshot = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        OperationRequestRejected rejected = Assert.IsInstanceOfType<OperationRequestRejected>(snapshot.Operation);
        Assert.AreSame(FileOperationRequestFailureKind.DestinationIsSource, rejected.Failure);
        Assert.AreSame(OperationKind.Copy, rejected.Kind);
        Assert.IsEmpty(fixture.Port.Calls);
    }
}
