using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves focus refers to the current visible entry after identity resolution and replacement.</summary>
[TestClass]
public sealed class PaneFocusReferenceTests
{
    /// <summary>Proves external preferred paths resolve by provider identity before copied-state movement.</summary>
    [TestMethod]
    [DataRow("C:\\root", "c:\\ROOT\\B.txt", 1)]
    [DataRow("\\\\server\\share\\root", "\\\\SERVER\\SHARE\\ROOT\\B.txt", 1)]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\root", "\\\\wsl$\\ubuntu\\home\\root\\b.txt", 1)]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\root", "\\\\wsl.localhost\\ubuntu\\home\\root\\B.txt", 0)]
    public void ApplyWhenPreferredPathWasResolvedMovesFromTheListedInstance(
        string location, string preferredText, int expectedIndex)
    {
        DirectoryListing listing = CreateListing(location);
        FileSystemPath preferred = PaneSessionTestContext.ParsePath(preferredText);
        PaneState original = PaneReducer.Navigate(
            listing, PaneSessionTestContext.Capacity(4), preferred, HiddenItemVisibility.Hidden);
        PaneState copy = original with { };
        PaneState selected = PaneReducer.Apply(copy, UserIntent.ToggleSelection);

        PaneState moved = PaneReducer.Apply(selected, UserIntent.MoveNext);

        Assert.AreNotSame(preferred, original.FocusItem);
        Assert.AreSame(listing.Entries[expectedIndex].Path, original.FocusItem);
        Assert.AreSame(original.FocusItem, copy.FocusItem);
        Assert.AreSame(listing.Entries[expectedIndex + 1].Path, moved.FocusItem);
        Assert.AreSame(original.VisibleEntries, moved.VisibleEntries);
        Assert.HasCount(1, moved.Selection);
        Assert.AreSame(original.FocusItem, moved.Selection[0]);
        Assert.IsEmpty(original.Selection);
    }

    /// <summary>Proves a refresh replaces path instances before the next movement.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenRefreshReplacesEntriesMovesWithinTheNewListing()
    {
        DirectoryListing first = CreateListing("C:\\root");
        DirectoryListing replacement = CreateListing("C:\\root");
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(first, null));
        port.Enqueue(DirectoryReadOutcome.Succeeded(replacement, null));
        PaneSession session = PaneSessionTestContext.CreateSession(port);
        _ = await session.NavigateAsync(first.Location, CancellationToken.None);
        PaneSnapshot before = await session.HandleAsync(UserIntent.MoveNext, CancellationToken.None);

        PaneSnapshot refreshed = await session.HandleAsync(UserIntent.Refresh, CancellationToken.None);
        PaneSnapshot moved = await session.HandleAsync(UserIntent.MovePrevious, CancellationToken.None);

        PaneState oldState = Assert.IsInstanceOfType<PaneContentListed>(before.Content).State;
        PaneState newState = Assert.IsInstanceOfType<PaneContentListed>(refreshed.Content).State;
        PaneState movedState = Assert.IsInstanceOfType<PaneContentListed>(moved.Content).State;
        Assert.AreSame(first.Entries[1].Path, oldState.FocusItem);
        Assert.AreNotSame(oldState.FocusItem, newState.FocusItem);
        Assert.AreSame(replacement.Entries[1].Path, newState.FocusItem);
        Assert.AreSame(replacement.Entries[0].Path, movedState.FocusItem);
        Assert.HasCount(2, port.Requests);
    }

    /// <summary>Proves a broken internal focus relationship is a defect, not an identity fallback.</summary>
    [TestMethod]
    public void ApplyWhenInternalFocusIsNotAListedInstanceThrowsInvalidOperationException()
    {
        DirectoryListing listing = CreateListing("C:\\root");
        PaneState state = PaneReducer.Navigate(
            listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        PaneState broken = state.Transition(PaneSessionTestContext.ParsePath("c:\\ROOT\\a.txt"), state.Selection, state.SelectionAnchor);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => PaneReducer.Apply(broken, UserIntent.MoveNext));
    }

    private static DirectoryListing CreateListing(string location)
    {
        return PaneSessionTestContext.Listing(
            location,
            ("a.txt", DirectoryEntryKind.File),
            ("b.txt", DirectoryEntryKind.File),
            ("c.txt", DirectoryEntryKind.File));
    }
}
