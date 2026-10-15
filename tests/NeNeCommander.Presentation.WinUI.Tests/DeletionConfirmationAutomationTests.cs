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
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves deletion confirmation identifiers follow the existing operation lifecycle.</summary>
[TestClass]
public sealed class DeletionConfirmationAutomationTests
{
    /// <summary>Proves accepting permanent deletion clears the confirmation identifier after the gateway completes.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenDeletionIsConfirmedClearsConfirmationIdentifier()
    {
        DualPaneSession panes = CreatePanes(
            out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right,
            out FileOperationGateway gateway,
            out QueuedFileOperationPort port);
        using FileOperationGateway owned = gateway;
        DirectoryListing listing = CreateListing("C:\\left", ["a.txt"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing));
        left.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\left", [])));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\right", [])));
        right.Enqueue(DirectoryReadOutcome.Succeeded(CreateListing("C:\\right", [])));
        _ = await panes.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        FileIdentity identity = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("identity")).Identity;
        FileInspectionOutcome inspection = FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(listing.Entries[0].Path, identity, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(inspection);
        port.EnqueueInspection(inspection);
        port.EnqueueStep(ProviderStepOutcome.Succeeded());

        DualPanePresentation pending = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Delete, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);
        DualPanePresentation completed = DualPanePresenter.Present(
            await panes.HandleAsync(UserIntent.Confirm, RecordingDualPaneObserver.Create(), CancellationToken.None), TestMetadataFormats.Utc);

        Assert.AreEqual("DeletionConfirmation", pending.DeletionConfirmationAutomationId);
        Assert.AreSame(OperationStatus.DeleteSucceeded, completed.OperationStatus);
        Assert.AreEqual(string.Empty, completed.DeletionConfirmationAutomationId);
        Assert.AreSame(KeyboardContext.FileList, completed.InputContext);
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
        return new DualPaneSession(CreatePane(left), CreatePane(right), gateway);
    }

    private static PaneSession CreatePane(IDirectoryReadPort port)
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        return new PaneSession(port, new AcceptedFileLauncher(), PaneSessionConfiguration.Create(
            capacity, DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
    }

    private static DirectoryListing CreateListing(string location, string[] names)
    {
        FileSystemPath root = ParsePath(location);
        DirectoryEntry[] entries = [.. names.Select(name => DirectoryEntry.Create(
            Assert.IsInstanceOfType<PathParseSuccess>(root.Child(name)).Path,
            name,
            DirectoryEntryKind.File,
            EntryMetadata.Unmeasured(EntryVisibility.Normal)))];
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            root, entries, DirectoryListingCompleteness.Complete, 0)).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
