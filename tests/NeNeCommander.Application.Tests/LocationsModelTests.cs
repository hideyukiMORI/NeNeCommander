using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Locations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the Locations picker records reject absent components and project their entries.</summary>
[TestClass]
public sealed class LocationsModelTests
{
    /// <summary>Proves every internal Locations record rejects an absent component.</summary>
    [TestMethod]
    public void ConstructorsWhenComponentIsNullThrowArgumentNullException()
    {
        DriveSection drives = DriveSection.Of(LocationsSessionTests.DrivesOf(LocationsSessionTests.Drive("C:\\", DriveKind.Fixed, null)));
        WslRootSection wslRoots = WslRootSection.Of(LocationsSessionTests.RootsOf("Ubuntu"));
        LocationsOpen open = new(PaneSide.Left, drives, wslRoots, 0);
        FileSystemPath target = open.Items[0].Location;

        AssertNullGuard(() => _ = new LocationsLoading(null!));
        AssertNullGuard(() => _ = new LocationsOpen(null!, drives, wslRoots, 0));
        AssertNullGuard(() => _ = new LocationsOpen(PaneSide.Left, null!, wslRoots, 0));
        AssertNullGuard(() => _ = new LocationsOpen(PaneSide.Left, drives, null!, 0));
        AssertNullGuard(() => _ = new LocationTargetAccepted(null!, target));
        AssertNullGuard(() => _ = new LocationTargetAccepted(PaneSide.Left, null!));
        AssertNullGuard(() => _ = UserIntent.SelectLocation(null!, open.Items[0]));
        AssertNullGuard(() => _ = UserIntent.SelectLocation(open, null!));
        AssertNullGuard(() => _ = UserIntent.CancelLocations(null!));
        AssertNullGuard(() => _ = new DriveLocationItem(null!));
        AssertNullGuard(() => _ = new WslLocationItem(null!));
        AssertNullGuard(() => _ = new DriveSectionListed(null!));
        AssertNullGuard(() => _ = new DriveSectionFailed(null!));
        AssertNullGuard(() => _ = new WslRootSectionListed(null!));
        AssertNullGuard(() => _ = new WslRootSectionFailed(null!));
    }

    /// <summary>Proves each entry navigates to the root it lists, and qualified intents keep their state and entry.</summary>
    [TestMethod]
    public void EntriesWhenProjectedNameTheirRootAndQualifiedIntentsKeepTheirState()
    {
        DriveLocation drive = LocationsSessionTests.Drive("C:\\", DriveKind.Fixed, "System");
        LocationsOpen open = new(
            PaneSide.Right,
            DriveSection.Of(DriveCatalogOutcome.Succeeded([drive], 0)),
            WslRootSection.Of(LocationsSessionTests.RootsOf("Ubuntu")),
            0);

        LocationSelection selection = Assert.IsInstanceOfType<LocationSelection>(
            UserIntent.SelectLocation(open, open.Items[1]));
        LocationsCancellation cancellation = Assert.IsInstanceOfType<LocationsCancellation>(
            UserIntent.CancelLocations(open));

        Assert.AreSame(drive.Root, open.Items[0].Location);
        Assert.AreSame(drive, Assert.IsInstanceOfType<DriveLocationItem>(open.Items[0]).Drive);
        WslLocationItem wsl = Assert.IsInstanceOfType<WslLocationItem>(open.Items[1]);
        Assert.AreSame(wsl.Root, wsl.Location);
        LocationsOpen selectionState = selection.ExpectedState;
        Assert.AreSame(open, selectionState);
        Assert.AreSame(open.Items[1], selection.Item);
        LocationsOpen cancellationState = cancellation.ExpectedState;
        Assert.AreSame(open, cancellationState);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
    }

    /// <summary>Proves a cancelled outcome is never turned into a section.</summary>
    [TestMethod]
    public void SectionsWhenOutcomeIsCancelledRejectTheImpossibleState()
    {
        _ = Assert.ThrowsExactly<InvalidCastException>(() => DriveSection.Of(DriveCatalogOutcome.Cancelled()));
        _ = Assert.ThrowsExactly<InvalidCastException>(() => WslRootSection.Of(WslDistributionCatalogOutcome.Cancelled()));
    }

    private static void AssertNullGuard(Action action)
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(action);
    }
}
