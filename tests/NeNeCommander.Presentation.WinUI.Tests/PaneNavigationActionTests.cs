using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the header projects the existing admission and canonical shortcut without owning navigation.</summary>
[TestClass]
public sealed class PaneNavigationActionTests
{
    /// <summary>Proves either active source exposes its opposite direction while the passive action stays absent.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenActivationChangesSwapsVisibilityAndDirectionWithoutChangingRows()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        await ListAsync(panes, left, right);
        _ = await HandleAsync(panes, UserIntent.ToggleSelection);
        DualPaneSnapshot before = panes.Current;
        PaneNavigationAction leftAction = PaneNavigationAction.Create(before, PaneSide.Left);
        PaneNavigationAction rightHidden = PaneNavigationAction.Create(before, PaneSide.Right);
        DualPaneSnapshot activated = await HandleAsync(panes, UserIntent.ActivateOtherPane);
        PaneNavigationAction rightAction = PaneNavigationAction.Create(activated, PaneSide.Right);
        PaneNavigationAction leftHidden = PaneNavigationAction.Create(activated, PaneSide.Left);

        Assert.IsTrue(leftAction.IsVisible && leftAction.IsEnabled);
        Assert.IsTrue(rightAction.IsVisible && rightAction.IsEnabled);
        Assert.IsFalse(rightHidden.IsVisible || rightHidden.IsEnabled);
        Assert.IsFalse(leftHidden.IsVisible || leftHidden.IsEnabled);
        Assert.AreEqual("Open this folder in the right pane (Ctrl+E)", leftAction.ComposeName(PresentationResourceCatalog.English.Get));
        Assert.AreEqual("Open this folder in the left pane (Ctrl+E)", rightAction.ComposeName(PresentationResourceCatalog.English.Get));
        Assert.AreSame(before.Left, activated.Left);
        Assert.AreSame(before.Right, activated.Right);
    }

    /// <summary>Proves no source listing disables the still-visible active action.</summary>
    [TestMethod]
    public void CreateWhenSourceIsAbsentShowsDisabledActiveAction()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out _, out _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        PaneNavigationAction action = PaneNavigationAction.Create(panes.Current, PaneSide.Left);

        Assert.IsTrue(action.IsVisible);
        Assert.IsFalse(action.IsEnabled);
    }

    /// <summary>Proves a pending destination disables the source action and retained content becomes available after cancellation.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenOtherPaneIsLoadingDisablesUntilReadIsAbandoned()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        await ListAsync(panes, left, right);
        TaskCompletionSource<DirectoryReadOutcome> provider = right.EnqueuePendingInline();
        Task<DualPaneSnapshot> work = HandleAsync(panes, UserIntent.OpenCurrentLocationInOtherPane);
        PaneNavigationAction pending = PaneNavigationAction.Create(panes.Current, PaneSide.Left);
        _ = await HandleAsync(panes, UserIntent.Escape);
        DualPaneSnapshot abandoned = await work;
        provider.SetResult(DirectoryReadOutcome.Cancelled());

        Assert.IsTrue(pending.IsVisible);
        Assert.IsFalse(pending.IsEnabled);
        Assert.IsTrue(PaneNavigationAction.Create(abandoned, PaneSide.Left).IsEnabled);
        Assert.AreSame(PaneSide.Left, abandoned.ActiveSide);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Right.Activity);
    }

    /// <summary>Proves a pending name modal disables the action and cancelling restores existing readiness.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenNameEntryOwnsOperationDisablesUntilCancelled()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        await ListAsync(panes, left, right);
        DualPaneSnapshot naming = await HandleAsync(panes, UserIntent.CreateDirectory);
        PaneNavigationAction blocked = PaneNavigationAction.Create(naming, PaneSide.Left);
        DualPaneSnapshot cancelled = await HandleAsync(panes, UserIntent.Escape);

        _ = Assert.IsInstanceOfType<OperationAwaitingName>(naming.Operation);
        Assert.IsTrue(blocked.IsVisible);
        Assert.IsFalse(blocked.IsEnabled);
        Assert.IsTrue(PaneNavigationAction.Create(cancelled, PaneSide.Left).IsEnabled);
    }

    /// <summary>Proves a retained source after read failure still offers its displayed location.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenSourceRefreshFailsKeepsRetainedListingAvailable()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        await ListAsync(panes, left, right);
        PaneContent content = panes.Current.Left.Content;
        left.Enqueue(DirectoryReadOutcome.Failed(FileOperationFailureKind.AccessDenied));
        DualPaneSnapshot failed = await HandleAsync(panes, UserIntent.Refresh);

        Assert.AreSame(content, failed.Left.Content);
        _ = Assert.IsInstanceOfType<PaneReadFailed>(failed.Left.Activity);
        Assert.IsTrue(PaneNavigationAction.Create(failed, PaneSide.Left).IsEnabled);
    }

    /// <summary>Proves equal locations keep an available idempotent command and preserve the merged count projection.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenLocationsMatchKeepsEnabledNoOpAndSelectionCounts()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        DirectoryListing listing = DualPaneTestContext.CreateListing(@"C:\same", ["selected"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await panes.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, listing.Location, CancellationToken.None);
        DualPaneSnapshot before = await HandleAsync(panes, UserIntent.ToggleSelection);
        PaneNavigationAction action = PaneNavigationAction.Create(before, PaneSide.Left);
        DualPaneSnapshot after = await HandleAsync(panes, UserIntent.OpenCurrentLocationInOtherPane);
        DualPanePresentation rendered = DualPanePresenter.Present(after, TestMetadataFormats.Utc, KeyHintVisibility.Shown);

        Assert.IsTrue(action.IsVisible && action.IsEnabled);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(before.Right, after.Right);
        Assert.AreSame(before.Operation, after.Operation);
        Assert.AreSame(before.ActiveSide, after.ActiveSide);
        Assert.HasCount(1, left.Tokens);
        Assert.HasCount(1, right.Tokens);
        Assert.AreEqual(1, Assert.IsInstanceOfType<PaneCounts>(rendered.Left.Counts).Selected);
        Assert.AreEqual(0, Assert.IsInstanceOfType<PaneCounts>(rendered.Right.Counts).Selected);
    }

    /// <summary>Proves the public factory rejects an absent snapshot or side.</summary>
    [TestMethod]
    public void CreateWhenRequiredArgumentIsMissingRejectsIt()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out _, out _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        MethodInfo factory = typeof(PaneNavigationAction).GetMethod(nameof(PaneNavigationAction.Create))
            ?? throw new AssertFailedException("Factory not found.");

        TargetInvocationException snapshot = Assert.ThrowsExactly<TargetInvocationException>(() => factory.Invoke(null, [null, PaneSide.Left]));
        TargetInvocationException side = Assert.ThrowsExactly<TargetInvocationException>(() => factory.Invoke(null, [panes.Current, null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(snapshot.InnerException);
        _ = Assert.IsInstanceOfType<ArgumentNullException>(side.InnerException);
    }

    /// <summary>Proves label projection rejects an absent localization boundary.</summary>
    [TestMethod]
    public void ComposeNameWhenResolverIsMissingRejectsIt()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out _, out _, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        PaneNavigationAction action = PaneNavigationAction.Create(panes.Current, PaneSide.Left);
        MethodInfo compose = typeof(PaneNavigationAction).GetMethod(nameof(PaneNavigationAction.ComposeName))
            ?? throw new AssertFailedException("Name projection not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(() => compose.Invoke(action, [null]));
        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static async Task ListAsync(DualPaneSession panes, ScriptedDirectoryReadPort left, ScriptedDirectoryReadPort right)
    {
        DirectoryListing source = DualPaneTestContext.CreateListing(@"C:\source", ["selected"]);
        DirectoryListing target = DualPaneTestContext.CreateListing(@"C:\target", []);
        left.Enqueue(DirectoryReadOutcome.Succeeded(source, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(target, null));
        _ = await panes.NavigateAsync(PaneSide.Left, source.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, target.Location, CancellationToken.None);
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSession panes, UserIntent intent)
    {
        return panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }
}
