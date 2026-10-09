using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves that Escape abandons a pane read in flight through the sole navigation path (ADR-0058).</summary>
[TestClass]
public sealed class PaneSessionAbandonmentTests
{
    /// <summary>Proves Escape during a read keeps the previous listing, focus, selection, and history and names the target.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeArrivesDuringReadKeepsPreviousContent()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\first", "x.txt")));
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt", "b.txt")));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\first"), CancellationToken.None);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot before = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        FileSystemPath target = ParsePath("C:\\unreachable");
        Task<PaneSnapshot> navigation = session.NavigateAsync(target, CancellationToken.None);

        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        Assert.AreSame(target, Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Activity).Target);
        Assert.AreSame(before.Content, abandoned.Content);
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(abandoned.Content);
        Assert.AreEqual("C:\\root", listed.Listing.Location.CanonicalText);
        Assert.AreSame(listed.Listing.Entries[1].Path, listed.State.FocusItem);
        Assert.HasCount(1, listed.State.Selection);
        Assert.AreEqual("C:\\first", listed.State.NavigationHistory.BackTarget?.CanonicalText);
        Assert.AreSame(abandoned, session.Current);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }

    /// <summary>Proves Escape cancels the token the abandoned read observes and nothing before it.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeArrivesDuringReadCancelsTheReadToken()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\unreachable"), CancellationToken.None);
        bool cancelledBeforeEscape = port.Tokens[0].IsCancellationRequested;

        _ = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        Assert.IsFalse(cancelledBeforeEscape);
        Assert.IsTrue(port.Tokens[0].IsCancellationRequested);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }

    /// <summary>Proves each read observes its own token, linked to the caller's token.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenCallerCancelsPropagatesToTheOwnedReadToken()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        using CancellationTokenSource caller = new();
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\root"), caller.Token);

        await caller.CancelAsync();

        Assert.AreNotEqual(caller.Token, port.Tokens[0]);
        Assert.IsTrue(port.Tokens[0].IsCancellationRequested);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        PaneSnapshot cancelled = await navigation;
        _ = Assert.IsInstanceOfType<PaneReadCancelled>(cancelled.Activity);
    }

    /// <summary>Proves a pane that had no listing shows the abandoned state alone.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeArrivesBeforeFirstListingLeavesContentAbsent()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\unreachable"), CancellationToken.None);

        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        Assert.AreSame(PaneContent.Absent, abandoned.Content);
        Assert.AreEqual("C:\\unreachable", Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Activity).Target.CanonicalText);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }

    /// <summary>Proves every outcome the abandoned read returns afterwards is discarded without a state write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    [DataRow("succeeded")]
    [DataRow("failed")]
    [DataRow("cancelled")]
    public async Task NavigateAsyncWhenAbandonedReadReturnsLateDiscardsResult(string outcome)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt")));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\late"), CancellationToken.None);
        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        pending.SetResult(outcome == "succeeded"
            ? DirectoryReadOutcome.Succeeded(Listing("C:\\late", "late.txt"))
            : outcome == "failed"
                ? DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
                : DirectoryReadOutcome.Cancelled());
        PaneSnapshot returned = await navigation;

        Assert.AreSame(abandoned, returned);
        Assert.AreSame(abandoned, session.Current);
    }

    /// <summary>Proves refresh, navigation, and movement are accepted immediately after abandonment.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenReadWasAbandonedAcceptsIntentsAgain()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt", "b.txt")));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt", "b.txt", "c.txt")));
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\next", "n.txt")));
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\unreachable"), CancellationToken.None);
        _ = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        PaneSnapshot moved = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);
        PaneSnapshot refreshed = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);
        PaneSnapshot navigated = await session.NavigateAsync(ParsePath("C:\\next"), CancellationToken.None);
        pending.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\unreachable", "u.txt")));
        PaneSnapshot afterLateResult = await navigation;

        Assert.AreSame(PaneActivity.Idle, moved.Activity);
        Assert.AreEqual("b.txt", Focus(moved));
        Assert.HasCount(3, Assert.IsInstanceOfType<PaneContentListed>(refreshed.Content).Listing.Entries);
        Assert.AreEqual("b.txt", Focus(refreshed));
        Assert.AreEqual("C:\\next", Assert.IsInstanceOfType<PaneContentListed>(navigated.Content).Listing.Location.CanonicalText);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(afterLateResult.Activity);
        Assert.AreSame(navigated, session.Current);
        Assert.HasCount(4, port.Requests);
    }

    /// <summary>Proves every intent other than Escape stays frozen and leaves the read token untouched.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    [DataRow("refresh")]
    [DataRow("open")]
    [DataRow("parent")]
    [DataRow("selection")]
    [DataRow("back")]
    public async Task HandleAsyncWhenReadIsInFlightFreezesEveryIntentButEscape(string intentName)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt")));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\other"), CancellationToken.None);
        PaneSnapshot loading = session.Current;

        PaneSnapshot frozen = await session.HandleAsync(IntentNamed(intentName), CancellationToken.None);

        Assert.AreSame(loading, frozen);
        Assert.IsFalse(port.Tokens[1].IsCancellationRequested);
        Assert.HasCount(2, port.Requests);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await navigation;
    }

    /// <summary>Proves Escape does not interrupt a focused-file handoff, which has no abandon path.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenEscapeArrivesDuringLaunchStaysFrozen()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt")));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> launch = launcher.EnqueuePending();
        PaneSession session = CreateSession(port, launcher);
        _ = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        Task<PaneSnapshot> handoff = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot launching = session.Current;

        PaneSnapshot frozen = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        Assert.AreSame(launching, frozen);
        _ = Assert.IsInstanceOfType<PaneLaunching>(frozen.Activity);
        launch.SetResult(FileLaunchOutcome.Accepted());
        _ = await handoff;
    }

    /// <summary>Proves a superseded read that returns late does not detach the cancellation of the read that replaced it.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-016")]
    public async Task HandleAsyncWhenSupersededReadReturnsFirstEscapeCancelsTheLatestRead()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> first = port.EnqueuePending();
        TaskCompletionSource<DirectoryReadOutcome> second = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> firstNavigation = session.NavigateAsync(ParsePath("C:\\first"), CancellationToken.None);
        Task<PaneSnapshot> secondNavigation = session.NavigateAsync(ParsePath("C:\\second"), CancellationToken.None);
        first.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\first", "f.txt")));
        _ = await firstNavigation;

        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        Assert.IsTrue(port.Tokens[1].IsCancellationRequested);
        Assert.AreEqual("C:\\second", Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Activity).Target.CanonicalText);
        second.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await secondNavigation;
    }

    /// <summary>Proves a read whose provider faults releases its token source, so a later Escape still abandons the pane.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenReadFaultedEscapeAbandonsWithoutTouchingTheDisposedToken()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        InvalidOperationException defect = new("provider defect");
        pending.SetException(defect);
        InvalidOperationException observed = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => navigation);

        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);

        Assert.AreSame(defect, observed);
        Assert.AreEqual("C:\\root", Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Activity).Target.CanonicalText);
    }

    /// <summary>Proves the waiting caller returns with the abandoned snapshot while the provider is still running.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenReadIsAbandonedReturnsBeforeProviderCompletes()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePending();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\unreachable"), CancellationToken.None);

        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        PaneSnapshot returned = await navigation;

        Assert.AreSame(abandoned, returned);
        Assert.IsFalse(pending.Task.IsCompleted);
        pending.SetResult(DirectoryReadOutcome.Cancelled());
    }

    /// <summary>Proves the fault of an abandoned read is rethrown once, at the next entry point, and then cleared.</summary>
    [TestMethod]
    [DataRow("handle")]
    [DataRow("navigate")]
    [DataRow("refresh")]
    [DataRow("refreshFocusing")]
    public async Task EntryPointWhenAbandonedReadFaultedRethrowsOrphanedFaultOnce(string entryPoint)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt")));
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePendingInline();
        PaneSession session = CreateSession(port);
        PaneSnapshot listed = await session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\unreachable"), CancellationToken.None);
        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        _ = await navigation;
        InvalidOperationException defect = new("orphaned provider defect");
        pending.SetException(defect);

        InvalidOperationException observed = Assert.ThrowsExactly<InvalidOperationException>(
            () => _ = EnterSession(session, entryPoint, Assert.IsInstanceOfType<PaneContentListed>(listed.Content)));
        PaneSnapshot afterFault = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        Assert.AreSame(defect, observed);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(abandoned.Activity);
        Assert.AreEqual("C:\\root", Assert.IsInstanceOfType<PaneContentListed>(afterFault.Content).Listing.Location.CanonicalText);
    }

    /// <summary>Proves an abandoned read that later succeeds or is cancelled writes nothing and raises nothing.</summary>
    [TestMethod]
    [DataRow("succeeded")]
    [DataRow("canceled")]
    public async Task HandleAsyncWhenAbandonedReadCompletesLaterDiscardsItSilently(string completion)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePendingInline();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\unreachable"), CancellationToken.None);
        PaneSnapshot abandoned = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        _ = await navigation;

        if (completion == "succeeded")
        {
            pending.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\unreachable", "u.txt")));
        }
        else
        {
            pending.SetCanceled();
        }
        PaneSnapshot after = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        Assert.AreSame(abandoned, after);
        Assert.AreSame(abandoned, session.Current);
    }

    /// <summary>Proves the read's token source is disposed when the provider completes, abandoned or not.</summary>
    [TestMethod]
    [DataRow("abandoned")]
    [DataRow("awaited")]
    public async Task NavigateAsyncWhenProviderCompletesDisposesTheReadTokenSource(string caller)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        TaskCompletionSource<DirectoryReadOutcome> pending = port.EnqueuePendingInline();
        PaneSession session = CreateSession(port);
        Task<PaneSnapshot> navigation = session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None);
        if (caller == "abandoned")
        {
            _ = await session.HandleAsync(UserIntent.Escape, CancellationToken.None);
        }
        WaitHandle beforeCompletion = port.Tokens[0].WaitHandle;

        pending.SetResult(DirectoryReadOutcome.Succeeded(Listing("C:\\root", "a.txt")));
        _ = await navigation;

        Assert.IsNotNull(beforeCompletion);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = port.Tokens[0].WaitHandle);
    }

    /// <summary>Proves a provider that throws synchronously leaves the pane unchanged and disposes the read's token source.</summary>
    [TestMethod]
    public async Task NavigateAsyncWhenProviderThrowsSynchronouslyDisposesTokenSourceAndKeepsState()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        PaneSession session = CreateSession(port);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => session.NavigateAsync(ParsePath("C:\\root"), CancellationToken.None));

        Assert.AreSame(PaneSnapshot.Initial, session.Current);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = port.Tokens[0].WaitHandle);
    }

    private static Task<PaneSnapshot> EnterSession(PaneSession session, string entryPoint, PaneContentListed listed)
    {
        return entryPoint switch
        {
            "handle" => session.HandleAsync(UserIntent.MoveNext, CancellationToken.None),
            "navigate" => session.NavigateAsync(ParsePath("C:\\next"), CancellationToken.None),
            "refresh" => session.RefreshAsync(CancellationToken.None),
            _ => session.RefreshFocusingAsync(listed.Listing.Entries[0].Path, CancellationToken.None),
        };
    }

    private static UserIntent IntentNamed(string name)
    {
        return name switch
        {
            "refresh" => UserIntent.Refresh,
            "open" => UserIntent.OpenFocused,
            "parent" => UserIntent.NavigateParent,
            "selection" => UserIntent.ToggleSelection,
            _ => UserIntent.NavigateBack,
        };
    }

    private static string? Focus(PaneSnapshot snapshot)
    {
        PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(snapshot.Content);
        return listed.FindFocusedEntry()?.Name;
    }

    private static PaneSession CreateSession(IDirectoryReadPort port)
    {
        return CreateSession(port, new ScriptedFileLauncher());
    }

    private static PaneSession CreateSession(IDirectoryReadPort port, IFileLauncher launcher)
    {
        return new PaneSession(
            port,
            launcher,
            Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
    }

    private static DirectoryListing Listing(string location, params string[] files)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry[] built = new DirectoryEntry[files.Length];
        for (int index = 0; index < files.Length; index++)
        {
            built[index] = DirectoryEntry.Create(
                ParsePath(parsedLocation.CanonicalText + "\\" + files[index]),
                files[index],
                DirectoryEntryKind.File,
                EntryMetadata.Unmeasured(EntryVisibility.Normal));
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            parsedLocation,
            built,
            DirectoryListingCompleteness.Complete,
            0);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
