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
        Assert.AreSame(leftListing.Entries, presentation.Left.Entries);
        Assert.AreSame(PaneStatus.Cancelled, presentation.Right.Status);
        Assert.AreEqual("C:\\right", presentation.Right.AddressText);
        Assert.AreSame(OperationStatus.Idle, presentation.OperationStatus);
    }

    /// <summary>Proves activation swaps the frames without changing either pane's rows.</summary>
    [TestMethod]
    public async Task PresentWhenRightIsActiveFramesRightActiveAndLeftPassive()
    {
        DualPaneSession panes = CreatePanes(out ScriptedDirectoryReadPort _, out ScriptedDirectoryReadPort _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.ActivateOtherPane, CancellationToken.None);

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

        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.Move, CancellationToken.None);

        Assert.AreSame(OperationStatus.MoveRequestRejected, DualPanePresenter.Present(snapshot).OperationStatus);
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
        left = ScriptedDirectoryReadPort.Create();
        right = ScriptedDirectoryReadPort.Create();
        gateway = new FileOperationGateway(new UnusedFileOperationPort());
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
