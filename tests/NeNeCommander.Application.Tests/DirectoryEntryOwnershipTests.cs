using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves published directory and ordering snapshots retain their validated entries.</summary>
[TestClass]
public sealed class DirectoryEntryOwnershipTests
{
    /// <summary>Proves provider-neutral listings and record copies resist public collection mutation.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\server\\share\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\root")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public void CreateWhenPublishedEntriesAreInspectedRetainsFrozenContents(string location)
    {
        DirectoryListing listing = CreateListing(location);
        DirectoryListing copied = listing with { };
        DirectoryEntry[] expected = [.. listing.Entries];

        TryClearThroughSyncRoot(copied.Entries);

        CollectionAssert.AreEqual(expected, listing.Entries.ToArray());
        CollectionAssert.AreEqual(expected, copied.Entries.ToArray());
        Assert.AreSame(listing.Entries, copied.Entries);
        Assert.AreEqual(listing, copied);
        Assert.AreEqual(listing.GetHashCode(), copied.GetHashCode());
    }

    /// <summary>Proves the natural-order result cannot be changed independently of its declared snapshot.</summary>
    [TestMethod]
    public void ApplyWhenPublishedOrderIsInspectedRetainsNaturalOrder()
    {
        DirectoryListing listing = CreateListing("C:\\root");
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(listing.Entries, PaneSortOrder.Default);
        DirectoryEntry[] expected = [listing.Entries[1], listing.Entries[0]];

        TryClearThroughSyncRoot(ordered);

        CollectionAssert.AreEqual(expected, ordered.ToArray());
        Assert.AreEqual("file10.txt", listing.Entries[0].Name);
        Assert.AreEqual("file2.txt", listing.Entries[1].Name);
        Assert.AreEqual("file2.txt", ordered[0].Name);
        Assert.AreEqual("file10.txt", ordered[1].Name);
    }

    /// <summary>Proves listing inspection cannot detach the visible focus from its single file handoff.</summary>
    [TestMethod]
    [DataRow("C:\\root")]
    [DataRow("\\\\server\\share\\root")]
    [DataRow("\\\\wsl.localhost\\Ubuntu\\home\\root")]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public async Task HandleAsyncWhenPublishedListingIsInspectedLaunchesVisibleFocus(string location)
    {
        DirectoryListing listing = CreateListing(location);
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        port.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        ScriptedFileLauncher launcher = new();
        PaneSession session = PaneSessionTestContext.CreateSession(port, launcher);
        PaneSnapshot original = await session.NavigateAsync(listing.Location, CancellationToken.None);
        PaneContentListed content = Assert.IsInstanceOfType<PaneContentListed>(original.Content);

        TryClearThroughSyncRoot(content.Listing.Entries);
        PaneSnapshot afterOpen = await session.HandleAsync(UserIntent.OpenFocused, CancellationToken.None);

        Assert.HasCount(1, launcher.Targets);
        Assert.AreSame(content.State.FocusItem, launcher.Targets[0]);
        Assert.AreSame(content.State.VisibleEntries[0].Path, launcher.Targets[0]);
        Assert.AreSame(original.Content, afterOpen.Content);
        Assert.HasCount(2, content.Listing.Entries);
        Assert.HasCount(1, port.Requests);
    }

    /// <summary>Proves each public owner preserves collection reads and rejects generic mutation.</summary>
    [TestMethod]
    public void CollectionsWhenCopiedOrMutatedRetainTheirOwnedEntries()
    {
        DirectoryListing listing = CreateListing("C:\\root");
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(listing.Entries, PaneSortOrder.Default);
        PaneState pane = PaneReducer.Navigate(listing, PaneSessionTestContext.Capacity(4), null, HiddenItemVisibility.Hidden);
        IReadOnlyList<DirectoryEntry>[] snapshots = [listing.Entries, ordered, pane.VisibleEntries];

        foreach (IReadOnlyList<DirectoryEntry> snapshot in snapshots)
        {
            ICollection<DirectoryEntry> collection = Assert.IsInstanceOfType<ICollection<DirectoryEntry>>(snapshot);
            DirectoryEntry first = snapshot[0];
            DirectoryEntry[] copy = new DirectoryEntry[2];
            collection.CopyTo(copy, 0);
            copy[0] = snapshot[1];
            _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Add(first));
            _ = Assert.ThrowsExactly<NotSupportedException>(collection.Clear);
            _ = Assert.ThrowsExactly<NotSupportedException>(() => collection.Remove(first));
            Assert.IsTrue(collection.IsReadOnly);
            Assert.IsTrue(collection.Contains(first));
            Assert.HasCount(2, snapshot);
            Assert.AreSame(first, snapshot[0]);
        }
    }

    private static DirectoryListing CreateListing(string location)
    {
        return PaneSessionTestContext.Listing(location,
            ("file2.txt", DirectoryEntryKind.File), ("file10.txt", DirectoryEntryKind.File));
    }

    private static void TryClearThroughSyncRoot(IReadOnlyList<DirectoryEntry> entries)
    {
        if (entries is ICollection collection &&
            collection.SyncRoot is ICollection<DirectoryEntry> mutable && !mutable.IsReadOnly)
        {
            mutable.Clear();
        }
    }
}
