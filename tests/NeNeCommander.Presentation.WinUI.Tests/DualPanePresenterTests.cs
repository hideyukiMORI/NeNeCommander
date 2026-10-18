using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPanePresenter behavior and contracts.</summary>
[TestClass]
public sealed class DualPanePresenterTests
{
    /// <summary>Proves either pane reports its own observed network fact regardless of active side.</summary>
    [TestMethod]
    public async Task PresentWhenNetworkDriveIsPassiveStillShowsLocationIndication()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("Z:\\share", ["a.txt"]);
        DirectoryListing rightListing = DualPaneTestContext.CreateListing("C:\\right", ["b.txt"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, DriveKind.Network));
        right.Enqueue(DirectoryReadOutcome.Succeeded(rightListing, DriveKind.Fixed));
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        DualPaneSnapshot snapshot = await panes.NavigateAsync(PaneSide.Right, rightListing.Location, CancellationToken.None);

        DualPanePresentation first = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc);
        DualPaneSnapshot activated = await panes.HandleAsync(
            UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
        DualPanePresentation second = DualPanePresenter.Present(activated, TestMetadataFormats.Utc);

        Assert.AreSame(PaneLocationStatus.NetworkDrive, first.Left.LocationStatus);
        Assert.IsNull(first.Right.LocationStatus);
        Assert.AreSame(PaneLocationStatus.NetworkDrive, second.Left.LocationStatus);
        Assert.IsNull(second.Right.LocationStatus);
    }

    /// <summary>Proves the active side gets the active frame and both panes are projected.</summary>
    [TestMethod]
    public async Task PresentWhenLeftIsActiveFramesLeftActiveAndRightPassive()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DirectoryListing leftListing = DualPaneTestContext.CreateListing("C:\\left", ["a.txt"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(leftListing, null));
        right.Enqueue(DirectoryReadOutcome.Cancelled());
        _ = await panes.NavigateAsync(PaneSide.Left, leftListing.Location, CancellationToken.None);
        DualPaneSnapshot snapshot = await panes.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath("C:\\right"), CancellationToken.None);

        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc);

        Assert.AreSame(PaneFrame.Active, presentation.LeftFrame);
        Assert.AreSame(PaneFrame.Passive, presentation.RightFrame);
        Assert.AreSame(PaneSide.Left, presentation.ActiveSide);
        Assert.AreSame(leftListing.Entries[0], presentation.Left.Rows[0].Entry);
        Assert.AreSame(PaneStatus.Cancelled, presentation.Right.Status);
        Assert.AreEqual("C:\\right", presentation.Right.AddressText);
        Assert.AreSame(OperationStatus.Idle, presentation.OperationStatus);
        Assert.AreEqual(string.Empty, presentation.DeletionConfirmationAutomationId);
        Assert.AreSame(OperationDetail.None, presentation.Detail);
        Assert.AreSame(KeyboardContext.FileList, presentation.InputContext);
    }

    /// <summary>Proves activation swaps the frames without changing either pane's rows.</summary>
    [TestMethod]
    public async Task PresentWhenRightIsActiveFramesRightActiveAndLeftPassive()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort _, out ScriptedDirectoryReadPort _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DualPaneSnapshot snapshot = await panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);

        DualPanePresentation presentation = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc);

        Assert.AreSame(PaneFrame.Passive, presentation.LeftFrame);
        Assert.AreSame(PaneFrame.Active, presentation.RightFrame);
        Assert.AreSame(PaneSide.Right, presentation.ActiveSide);
        Assert.AreSame(PaneStatus.NoListing, presentation.Left.Status);
        Assert.AreSame(PaneStatus.NoListing, presentation.Right.Status);
    }

    /// <summary>Proves the dual-pane projection rejects an absent metadata format.</summary>
    [TestMethod]
    public void PresentWhenFormatIsNullThrowsArgumentNullException()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out _, out _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => DualPanePresenter.Present(panes.Current, null!));
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
            () => method.Invoke(null, [null, TestMetadataFormats.Utc]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }
}
