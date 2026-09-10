using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the bounded immutable location-history invariants.</summary>
[TestClass]
public sealed class PaneNavigationHistoryTests
{
    /// <summary>Proves a history cannot be constructed without its required location sequence.</summary>
    [TestMethod]
    public void CreateWhenLocationsIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => PaneNavigationHistory.Create(null!, 0));
    }

    /// <summary>Proves a history always contains between one and 100 locations.</summary>
    [TestMethod]
    public void CreateWhenLocationCountIsOutsideBoundThrowsArgumentOutOfRangeException()
    {
        FileSystemPath[] tooMany = new FileSystemPath[PaneNavigationHistory.LocationLimit + 1];
        Array.Fill(tooMany, ParsePath("C:\\root"));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PaneNavigationHistory.Create([], 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PaneNavigationHistory.Create(tooMany, 0));
    }

    /// <summary>Proves the cursor must identify one retained location.</summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    public void CreateWhenCurrentIndexIsOutsideLocationsThrowsArgumentOutOfRangeException(int currentIndex)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PaneNavigationHistory.Create([ParsePath("C:\\root")], currentIndex));
    }

    /// <summary>Proves an absent location cannot enter the owned immutable copy.</summary>
    [TestMethod]
    public void CreateWhenLocationIsNullThrowsArgumentNullException()
    {
        FileSystemPath[] locations = new FileSystemPath[1];

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => PaneNavigationHistory.Create(locations, 0));
    }

    /// <summary>Proves later changes to an input list cannot mutate an accepted history.</summary>
    [TestMethod]
    public void CreateWhenInputListChangesKeepsOwnedLocationSequence()
    {
        FileSystemPath root = ParsePath("C:\\root");
        List<FileSystemPath> locations = [root];
        PaneNavigationHistory history = PaneNavigationHistory.Create(locations, 0);

        locations[0] = ParsePath("C:\\other");

        Assert.AreSame(root, history.Locations[0]);
    }

    /// <summary>Proves a pane rejects history whose cursor identifies another location.</summary>
    [TestMethod]
    public void WithNavigationHistoryWhenCurrentDiffersFromPaneThrowsArgumentException()
    {
        PaneState state = CreateState("C:\\root");
        PaneNavigationHistory history = PaneNavigationHistory.Create([ParsePath("C:\\other")], 0);

        _ = Assert.ThrowsExactly<ArgumentException>(() => state.WithNavigationHistory(history));
    }

    /// <summary>Proves a pane cannot replace its required navigation history with absence.</summary>
    [TestMethod]
    public void WithNavigationHistoryWhenHistoryIsNullThrowsArgumentNullException()
    {
        PaneState state = CreateState("C:\\root");

        _ = Assert.ThrowsExactly<ArgumentNullException>(
            () => state.WithNavigationHistory(null!));
    }

    private static PaneState CreateState(string location)
    {
        PaneStateCreation creation = PaneState.Create(
            ParsePath(location),
            [],
            Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(VisiblePageCapacity.Create(4)).Capacity,
            HiddenItemVisibility.Hidden);
        return Assert.IsInstanceOfType<PaneStateAccepted>(creation).State;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
