using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves cancellation ownership and late-completion boundaries of opposite-pane navigation.</summary>
[TestClass]
public sealed class OtherPaneNavigationLifetimeTests
{
    /// <summary>Proves Escape releases the command without waiting for its provider and preserves both contents.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeArrivesAbandonsOnlyOwnedPassiveReadAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        _ = await HandleAsync(fixture, UserIntent.ToggleSelection);
        _ = await fixture.RightSession.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        DualPaneSnapshot before = fixture.Panes.Current;
        TaskCompletionSource<DirectoryReadOutcome> provider = fixture.Right.EnqueuePendingInline();
        Task<DualPaneSnapshot> navigation = HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);
        Assert.IsFalse(navigation.IsCompleted);
        Assert.IsFalse(fixture.Right.Tokens[1].IsCancellationRequested);

        DualPaneSnapshot escaped = await HandleAsync(fixture, UserIntent.Escape);
        DualPaneSnapshot returned = await navigation.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(provider.Task.IsCompleted);
        Assert.IsTrue(fixture.Right.Tokens[1].IsCancellationRequested);
        Assert.AreSame(before.Left, escaped.Left);
        Assert.AreSame(before.Right.Content, escaped.Right.Content);
        Assert.AreSame(escaped.Right, returned.Right);
        Assert.AreSame(PaneSide.Left, returned.ActiveSide);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(returned.Right.Activity);
        provider.SetResult(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"C:\source", ("late.txt", DirectoryEntryKind.File)), null));
        Assert.AreSame(returned.Right, fixture.Panes.Current.Right);
        Assert.IsEmpty(fixture.Port.Calls);
    }

    /// <summary>Proves an ordinary passive read has no Escape route from the active pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPassiveReadIsUnrelatedDoesNotAbandonItAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        _ = await HandleAsync(fixture, UserIntent.ToggleSelection);
        TaskCompletionSource<DirectoryReadOutcome> provider = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> navigation = fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath(@"D:\unrelated"), CancellationToken.None);
        PaneSnapshot passive = fixture.Panes.Current.Right;

        DualPaneSnapshot escaped = await HandleAsync(fixture, UserIntent.Escape);

        Assert.AreSame(passive, escaped.Right);
        Assert.IsFalse(fixture.Right.Tokens[1].IsCancellationRequested);
        Assert.IsFalse(navigation.IsCompleted);
        Assert.IsEmpty(Assert.IsInstanceOfType<PaneContentListed>(escaped.Left.Content).State.Selection);
        provider.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }

    /// <summary>Proves an active read receives Escape first, and a later Escape can abandon the owned passive read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActiveReadAlsoRunsKeepsActiveReadPriorityAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        TaskCompletionSource<DirectoryReadOutcome> passiveProvider = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> passiveNavigation = HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);
        TaskCompletionSource<DirectoryReadOutcome> activeProvider = fixture.Left.EnqueuePending();
        Task<DualPaneSnapshot> activeNavigation = fixture.Panes.NavigateAsync(PaneSide.Left, DualPaneSessionTestContext.ParsePath(@"C:\later"), CancellationToken.None);
        PaneSnapshot passiveLoading = fixture.Panes.Current.Right;

        DualPaneSnapshot first = await HandleAsync(fixture, UserIntent.Escape);
        _ = await activeNavigation.WaitAsync(TimeSpan.FromSeconds(5));

        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(first.Left.Activity);
        Assert.AreSame(passiveLoading, first.Right);
        Assert.IsTrue(fixture.Left.Tokens[1].IsCancellationRequested);
        Assert.IsFalse(fixture.Right.Tokens[1].IsCancellationRequested);
        DualPaneSnapshot second = await HandleAsync(fixture, UserIntent.Escape);
        _ = await passiveNavigation.WaitAsync(TimeSpan.FromSeconds(5));
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(second.Right.Activity);
        Assert.IsTrue(fixture.Right.Tokens[1].IsCancellationRequested);
        passiveProvider.SetResult(DirectoryReadOutcome.Cancelled());
        activeProvider.SetResult(DirectoryReadOutcome.Cancelled());
    }

    /// <summary>Proves a captured passive read cannot authorize cancellation of a newer unrelated target read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenTargetReadWasSupersededLeavesNewReadRunningAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        _ = await HandleAsync(fixture, UserIntent.ToggleSelection);
        TaskCompletionSource<DirectoryReadOutcome> oldProvider = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> oldNavigation = HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);
        TaskCompletionSource<DirectoryReadOutcome> newProvider = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> newNavigation = fixture.Panes.NavigateAsync(PaneSide.Right, DualPaneSessionTestContext.ParsePath(@"D:\newer"), CancellationToken.None);
        PaneSnapshot newer = fixture.Panes.Current.Right;

        DualPaneSnapshot escaped = await HandleAsync(fixture, UserIntent.Escape);

        Assert.AreSame(newer, escaped.Right);
        Assert.IsEmpty(Assert.IsInstanceOfType<PaneContentListed>(escaped.Left.Content).State.Selection);
        Assert.IsFalse(fixture.Right.Tokens[1].IsCancellationRequested);
        Assert.IsFalse(fixture.Right.Tokens[2].IsCancellationRequested);
        oldProvider.SetResult(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"C:\source"), null));
        _ = await oldNavigation;
        Assert.AreSame(newer, fixture.Panes.Current.Right);
        newProvider.SetResult(DirectoryReadOutcome.Succeeded(DualPaneSessionTestContext.Listing(@"D:\newer"), null));
        _ = await newNavigation;
        Assert.AreEqual(@"D:\newer", Assert.IsInstanceOfType<PaneContentListed>(fixture.Panes.Current.Right.Content).State.Location.CanonicalText);
    }

    /// <summary>Proves direct activation during a read cannot retarget that already captured destination.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActiveSideChangesKeepsCapturedDestinationAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        DualPaneSnapshot before = fixture.Panes.Current;
        TaskCompletionSource<DirectoryReadOutcome> provider = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> navigation = HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);
        _ = await HandleAsync(fixture, UserIntent.ActivateOtherPane);
        DirectoryListing arrived = DualPaneSessionTestContext.Listing(@"C:\source", ("arrived.txt", DirectoryEntryKind.File));

        provider.SetResult(DirectoryReadOutcome.Succeeded(arrived, null));
        DualPaneSnapshot after = await navigation;

        Assert.AreSame(PaneSide.Right, after.ActiveSide);
        Assert.AreSame(before.Left, after.Left);
        Assert.AreSame(arrived, Assert.IsInstanceOfType<PaneContentListed>(after.Right.Content).Listing);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(2, fixture.Right.Requests);
    }

    /// <summary>Proves a fault releases the captured route without converting a defect into a read outcome.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOwnedProviderFaultsClearsEscapeRouteAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        _ = await HandleAsync(fixture, UserIntent.ToggleSelection);
        TaskCompletionSource<DirectoryReadOutcome> provider = fixture.Right.EnqueuePending();
        Task<DualPaneSnapshot> navigation = HandleAsync(fixture, UserIntent.OpenCurrentLocationInOtherPane);
        PaneSnapshot faultedRead = fixture.Panes.Current.Right;
        InvalidOperationException defect = new("Owned provider defect.");

        provider.SetException(defect);
        InvalidOperationException actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => navigation);
        DualPaneSnapshot escaped = await HandleAsync(fixture, UserIntent.Escape);

        Assert.AreSame(defect, actual);
        Assert.AreSame(faultedRead, escaped.Right);
        Assert.IsEmpty(Assert.IsInstanceOfType<PaneContentListed>(escaped.Left.Content).State.Selection);
    }

    /// <summary>Proves the initiating caller's token remains linked to the existing destination read.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenCallerCancelsSignalsOnlyDestinationReadAsync()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();
        await ListAsync(fixture);
        DualPaneSnapshot before = fixture.Panes.Current;
        TaskCompletionSource<DirectoryReadOutcome> provider = fixture.Right.EnqueuePending();
        using CancellationTokenSource caller = new();
        Task<DualPaneSnapshot> navigation = fixture.Panes.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, RecordingDualPaneObserver.Create(), caller.Token);

        await caller.CancelAsync();

        Assert.AreNotEqual(caller.Token, fixture.Right.Tokens[1]);
        Assert.IsTrue(fixture.Right.Tokens[1].IsCancellationRequested);
        Assert.AreSame(before.Left, fixture.Panes.Current.Left);
        provider.SetResult(DirectoryReadOutcome.Cancelled());
        DualPaneSnapshot cancelled = await navigation;
        Assert.AreSame(before.Right.Content, cancelled.Right.Content);
        _ = Assert.IsInstanceOfType<PaneReadCancelled>(cancelled.Right.Activity);
    }

    private static Task ListAsync(DualPaneSessionTestContext fixture)
    {
        return fixture.ListBothAsync(
            DualPaneSessionTestContext.Listing(@"C:\source", ("source.txt", DirectoryEntryKind.File)),
            DualPaneSessionTestContext.Listing(@"D:\target", ("target.txt", DirectoryEntryKind.File)));
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSessionTestContext fixture, UserIntent intent)
    {
        return fixture.Panes.HandleAsync(intent, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }
}
