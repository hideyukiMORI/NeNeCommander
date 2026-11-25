using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves reveal uses one external-handoff lifecycle without navigation or selection changes.</summary>
[TestClass]
public sealed class PaneSessionRevealTests
{
    /// <summary>Proves files and directories reveal the focused path without listing their contents.</summary>
    [TestMethod]
    [DataRow("file")]
    [DataRow("directory")]
    public async Task HandleAsyncWhenRevealFocusedHandsExactFocusWithoutReadOrStateChangeAsync(string kind)
    {
        DirectoryEntryKind entryKind = kind == "file" ? DirectoryEntryKind.File : DirectoryEntryKind.Directory;
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("target", entryKind));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneSnapshot before = await session.HandleAsync(UserIntent.ToggleSelection, CancellationToken.None);
        using CancellationTokenSource cancellation = new();

        PaneSnapshot after = await session.HandleAsync(UserIntent.RevealFocused, cancellation.Token);

        Assert.AreSame(before.Content, after.Content);
        Assert.AreSame(PaneActivity.Idle, after.Activity);
        Assert.AreSame(listing.Entries[0].Path, launcher.Targets.Single());
        Assert.AreSame(FileLaunchPurpose.Reveal, launcher.Purposes.Single());
        Assert.AreEqual(cancellation.Token, launcher.CancellationTokens.Single());
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves empty provider roots reveal the exact listed folder rather than an invented parent.</summary>
    [TestMethod]
    [DataRow("C:\\")]
    [DataRow("\\\\server\\share\\")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\")]
    public async Task HandleAsyncWhenRevealLocationOnEmptyRootHandsExactRootAsync(string path)
    {
        DirectoryListing listing = PaneSessionTestContext.Listing(path);
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot before = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PaneSnapshot after = await session.HandleAsync(UserIntent.RevealLocation, CancellationToken.None);

        Assert.AreSame(listing.Location, launcher.Targets.Single());
        Assert.AreSame(FileLaunchPurpose.Reveal, launcher.Purposes.Single());
        Assert.AreSame(before.Content, after.Content);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves an absent focused item never falls back to revealing the directory.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRevealFocusedHasNoFocusDoesNotHandoffAsync()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root");
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot before = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PaneSnapshot after = await session.HandleAsync(UserIntent.RevealFocused, CancellationToken.None);

        Assert.AreSame(before, after);
        Assert.IsEmpty(launcher.Targets);
    }

    /// <summary>Proves pending reveal freezes all pane routes and Escape is not a new cancellation owner.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRevealIsPendingRetainsExistingLaunchFreezeAsync()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("item.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        TaskCompletionSource<FileLaunchOutcome> release = launcher.EnqueuePending();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot before = await session.NavigateAsync(listing.Location, CancellationToken.None);
        Task<PaneSnapshot> pending = session.HandleAsync(UserIntent.RevealFocused, CancellationToken.None);
        PaneSnapshot launching = session.Current;
        PaneLaunching activity = Assert.IsInstanceOfType<PaneLaunching>(launching.Activity);

        UserIntent[] blocked = [UserIntent.RevealFocused, UserIntent.RevealLocation, UserIntent.OpenFocused,
            UserIntent.Refresh, UserIntent.MoveNext, UserIntent.ToggleSelection, UserIntent.Escape];
        foreach (UserIntent intent in blocked)
        {
            Assert.AreSame(launching, await session.HandleAsync(intent, CancellationToken.None));
        }
        Assert.AreSame(launching, await session.NavigateAsync(listing.Location, CancellationToken.None));
        Assert.AreSame(launching, await session.RefreshAsync(CancellationToken.None));
        Assert.AreSame(launching, await session.RefreshFocusingAsync(listing.Entries[0].Path, CancellationToken.None));
        Assert.AreSame(FileLaunchPurpose.Reveal, activity.Purpose);
        Assert.AreSame(listing.Entries[0].Path, activity.Target);
        Assert.HasCount(1, launcher.Targets);
        Assert.HasCount(1, port.Requests);
        release.SetResult(FileLaunchOutcome.Accepted());
        Assert.AreSame(before.Content, (await pending).Content);
    }

    /// <summary>Proves failure and cancellation preserve reveal purpose and exact captured content.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRevealFailsOrCancelsPreservesPurposeAndContentAsync()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("item.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Failed(FileLaunchFailureKind.AccessDenied));
        launcher.Enqueue(FileLaunchOutcome.Cancelled());
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot before = await session.NavigateAsync(listing.Location, CancellationToken.None);

        PaneSnapshot failed = await session.HandleAsync(UserIntent.RevealLocation, CancellationToken.None);
        PaneSnapshot cancelled = await session.HandleAsync(UserIntent.RevealFocused, CancellationToken.None);

        PaneLaunchFailed failure = Assert.IsInstanceOfType<PaneLaunchFailed>(failed.Activity);
        PaneLaunchCancelled cancellation = Assert.IsInstanceOfType<PaneLaunchCancelled>(cancelled.Activity);
        Assert.AreSame(FileLaunchPurpose.Reveal, failure.Purpose);
        Assert.AreSame(FileLaunchPurpose.Reveal, cancellation.Purpose);
        Assert.AreSame(listing.Location, failure.Target);
        Assert.AreSame(listing.Entries[0].Path, cancellation.Target);
        Assert.AreSame(FileLaunchFailureKind.AccessDenied, failure.Failure);
        Assert.AreSame(before.Content, failed.Content);
        Assert.AreSame(before.Content, cancelled.Content);
    }

    /// <summary>Proves a later normal activation uses its original purpose rather than retained reveal state.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenActivationFollowsRevealKeepsDistinctPortActionsAsync()
    {
        DirectoryListing listing = PaneSessionTestContext.Listing("C:\\root", ("item.txt", DirectoryEntryKind.File));
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        launcher.Enqueue(FileLaunchOutcome.Accepted());
        launcher.Enqueue(FileLaunchOutcome.Cancelled());
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        _ = await session.NavigateAsync(listing.Location, CancellationToken.None);

        _ = await session.HandleAsync(UserIntent.RevealFocused, CancellationToken.None);
        PaneSnapshot activated = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { FileLaunchPurpose.Reveal, FileLaunchPurpose.Activation }, launcher.Purposes);
        Assert.AreSame(FileLaunchPurpose.Activation,
            Assert.IsInstanceOfType<PaneLaunchCancelled>(activated.Activity).Purpose);
    }
}
