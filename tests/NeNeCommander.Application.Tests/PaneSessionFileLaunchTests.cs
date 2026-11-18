using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves focused-file launch owns input and preserves pane content across every typed outcome.</summary>
[TestClass]
public sealed class PaneSessionFileLaunchTests
{
    /// <summary>Proves opening a focused local, UNC or WSL file performs one handoff without another read.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    [DataRow("\\\\server\\share\\folder")]
    public async Task HandleAsyncWhenOpenFocusedOnSupportedFileLaunchesOnceWithoutRead(string location)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneSessionTestContext.Listing(location, ("docs", DirectoryEntryKind.Directory), ("a.txt", DirectoryEntryKind.File)), null));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Accepted());
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        using CancellationTokenSource cancellation = new();
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath(location), CancellationToken.None);
        PaneSnapshot onFile = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot afterOpen = await session.HandleAsync(UserIntent.OpenFocused, cancellation.Token);

        Assert.HasCount(1, launcher.Targets);
        Assert.AreSame(
            Assert.IsInstanceOfType<PaneContentListed>(onFile.Content).State.FocusItem,
            launcher.Targets[0]);
        Assert.AreEqual(cancellation.Token, launcher.CancellationTokens[0]);
        Assert.AreSame(onFile.Content, afterOpen.Content);
        Assert.AreSame(PaneActivity.Idle, afterOpen.Activity);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves launch activity freezes repeat dispatch until the handoff outcome arrives.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    [DataRow("\\\\server\\share\\folder")]
    public async Task HandleAsyncWhenFileLaunchIsPendingFreezesRepeatedIntent(string location)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneSessionTestContext.Listing(location, ("a.txt", DirectoryEntryKind.File)), null));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(PaneSessionTestContext.ParsePath(location), CancellationToken.None);

        Task<PaneSnapshot> opening = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot repeated = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        PaneLaunching activity = Assert.IsInstanceOfType<PaneLaunching>(repeated.Activity);
        Assert.AreSame(launcher.Targets[0], activity.Target);
        Assert.AreSame(listed.Content, repeated.Content);
        Assert.HasCount(1, launcher.Targets);
        release.SetResult(FileLaunchOutcome.Accepted());
        PaneSnapshot completed = await opening;
        Assert.AreSame(PaneActivity.Idle, completed.Activity);
        Assert.AreSame(listed.Content, completed.Content);
    }

    /// <summary>Proves every direct pane-read entry point is frozen while a file handoff is pending.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    [DataRow("\\\\server\\share\\folder")]
    public async Task PaneReadEntryPointsWhenFileLaunchIsPendingDoNotStartReadOrChangeState(string location)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.Listing(location, ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> opening = session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot launching = session.Current;

        PaneSnapshot navigated = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\other"), CancellationToken.None);
        PaneSnapshot refreshed = await session.RefreshAsync(CancellationToken.None);
        PaneSnapshot focusRefreshed = await session.RefreshFocusingAsync(
            listing.Entries[0].Path,
            CancellationToken.None);

        Assert.AreSame(launching, navigated);
        Assert.AreSame(launching, refreshed);
        Assert.AreSame(launching, focusRefreshed);
        Assert.AreSame(launching, session.Current);
        Assert.HasCount(1, port.Requests);
        release.SetResult(FileLaunchOutcome.Accepted());
        PaneSnapshot completed = await opening;
        Assert.AreSame(listed.Content, completed.Content);
        Assert.AreSame(PaneActivity.Idle, completed.Activity);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves normalized launch failure and cancellation preserve all listed pane state.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home")]
    [DataRow("\\\\server\\share\\folder")]
    public async Task HandleAsyncWhenFileLaunchDoesNotHandoffReportsTypedActivity(string location)
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneSessionTestContext.Listing(location, ("a.txt", DirectoryEntryKind.File)), null));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AccessDenied));
        launcher.Enqueue(FileLaunchOutcome.Cancelled());
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(PaneSessionTestContext.ParsePath(location), CancellationToken.None);

        PaneSnapshot failed = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);
        PaneSnapshot cancelled = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        PaneLaunchFailed failure = Assert.IsInstanceOfType<PaneLaunchFailed>(failed.Activity);
        Assert.AreSame(FileLaunchFailureKind.AccessDenied, failure.Failure);
        Assert.AreSame(listed.Content, failed.Content);
        Assert.AreSame(failure.Target, Assert.IsInstanceOfType<PaneLaunchCancelled>(cancelled.Activity).Target);
        Assert.AreSame(listed.Content, cancelled.Content);
        Assert.HasCount(2, launcher.Targets);
    }

    /// <summary>Proves UNC admission failures remain owned by the launcher and preserve pane content.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-019")]
    [TestCategory("Adversarial")]
    public async Task HandleAsyncWhenUncLauncherRefusesPreservesPaneContentAsync()
    {
        const string Location = "\\\\server\\share\\folder";
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        DirectoryListing listing = PaneSessionTestContext.Listing(Location, ("a.txt", DirectoryEntryKind.File));
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.ProviderUnavailable));
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot listed = await session.NavigateAsync(PaneSessionTestContext.ParsePath(Location), CancellationToken.None);

        PaneSnapshot snapshot = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        PaneLaunchFailed failed = Assert.IsInstanceOfType<PaneLaunchFailed>(snapshot.Activity);
        Assert.AreSame(FileLaunchFailureKind.ProviderUnavailable, failed.Failure);
        Assert.AreSame(listed.Content, snapshot.Content);
        Assert.HasCount(1, launcher.Targets);
        Assert.AreSame(listing.Entries[0].Path, launcher.Targets[0]);
        Assert.AreSame(launcher.Targets[0], failed.Target);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves an unregistered launch outcome variant is a defect instead of implicit success.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenFileLaunchOutcomeVariantIsUnsupportedThrows()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(
            PaneSessionTestContext.Listing("C:\\root", ("a.txt", DirectoryEntryKind.File)), null));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(new UnsupportedFileLaunchOutcome());
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(PaneSessionTestContext.ParsePath("C:\\root"), CancellationToken.None);

        InvalidOperationException failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None));

        Assert.AreEqual("The file launch outcome variant is not supported.", failure.Message);
    }
}
