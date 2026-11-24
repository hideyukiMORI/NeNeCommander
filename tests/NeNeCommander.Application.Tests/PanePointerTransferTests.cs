using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves pointer-selected subjects feed the existing keyboard transfer gateway.</summary>
[TestClass]
public sealed class PanePointerTransferTests
{
    /// <summary>Proves clicking the inactive pane determines the following copy's exact source and destination.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCopyFollowsRightRowSelectionUsesThatEntryAndPassiveLeftAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        DirectoryListing left = DualPaneSessionTestContext.Listing("C:\\left");
        DirectoryListing right = DualPaneSessionTestContext.Listing("C:\\right",
            ("a.txt", DirectoryEntryKind.File), ("b.txt", DirectoryEntryKind.File));
        await fixture.ListBothAsync(left, right);
        _ = await fixture.Panes.HandleAsync(UserIntent.InteractWithPaneEntry(PaneSide.Right,
            fixture.Panes.Current.Right, right.Entries[1], PaneEntryAction.Select), RecordingDualPaneObserver.Create(), CancellationToken.None);
        fixture.Port.EnqueueInspection(DualPaneSessionTestContext.Inspection(right.Entries[1].Path));
        fixture.Port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        fixture.Port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing("C:\\left", ("b.txt", DirectoryEntryKind.File)), null));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(right, null));

        DualPaneSnapshot after = await fixture.Panes.HandleAsync(UserIntent.Copy, RecordingDualPaneObserver.Create(), CancellationToken.None);

        Assert.AreSame(PaneSide.Right, after.ActiveSide);
        Assert.HasCount(4, fixture.Port.Calls);
        Assert.AreEqual("Preflight:C:\\left", fixture.Port.Calls[1]);
        Assert.AreEqual("Copy:C:\\right\\b.txt", fixture.Port.Calls[2]);
        Assert.AreEqual("Verify:C:\\right\\b.txt", fixture.Port.Calls[3]);
        OperationCompleted completed = Assert.IsInstanceOfType<OperationCompleted>(after.Operation);
        Assert.AreSame(OperationKind.Copy, completed.Kind);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, completed.Outcome.Completion);
    }
}
