using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Locations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves the Locations picker scope owner: admission, the concurrent read of both sections, every
/// combination of section outcomes, focus boundaries, and qualified selection and cancellation.
/// </summary>
[TestClass]
public sealed class LocationsSessionTests
{
    /// <summary>Proves both catalogs start before either completes and the picker is loading meanwhile.</summary>
    [TestMethod]
    public async Task OpenAsyncWhenAdmittedStartsBothReadsConcurrentlyAndLoadsAsync()
    {
        ScriptedDriveCatalog drives = new();
        ScriptedWslDistributionCatalog wslRoots = new();
        LocationsSession session = new(drives, wslRoots);
        using CancellationTokenSource cancellation = new();

        Task<LocationsState> opening = session.OpenAsync(
            await PanesAsync(PaneSide.Right),
            InteractionOwnership.ScopeOwnsInput,
            cancellation.Token);

        Assert.IsFalse(opening.IsCompleted);
        Assert.AreEqual(1, drives.CallCount);
        Assert.AreEqual(1, wslRoots.CallCount);
        Assert.AreEqual(cancellation.Token, drives.ObservedToken);
        Assert.AreEqual(cancellation.Token, wslRoots.ObservedToken);
        LocationsLoading loading = Assert.IsInstanceOfType<LocationsLoading>(session.Current);
        Assert.AreSame(PaneSide.Right, loading.ActiveSide);
        drives.Complete(DrivesOf(Drive("C:\\", DriveKind.Fixed, "System")));
        Assert.IsFalse(opening.IsCompleted);
        Assert.AreSame(loading, session.Current);
        wslRoots.Complete(RootsOf("Ubuntu"));
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(await opening);
        Assert.AreSame(open, session.Current);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
    }

    /// <summary>Proves the picker refuses to open when another scope owns input, reading nothing.</summary>
    [TestMethod]
    public async Task OpenAsyncWhenAnotherScopeOwnsInputStaysClosedAsync()
    {
        ScriptedDriveCatalog drives = new();
        ScriptedWslDistributionCatalog wslRoots = new();
        LocationsSession session = new(drives, wslRoots);

        LocationsState state = await session.OpenAsync(
            await PanesAsync(PaneSide.Left),
            InteractionOwnership.AnotherScopeOwnsInput,
            CancellationToken.None);

        Assert.AreSame(LocationsState.Closed, state);
        Assert.AreEqual(0, drives.CallCount);
        Assert.AreEqual(0, wslRoots.CallCount);
    }

    /// <summary>Proves opening an open or loading picker changes nothing and reads nothing again.</summary>
    [TestMethod]
    public async Task OpenAsyncWhenAlreadyLoadingOrOpenChangesNothingAsync()
    {
        ScriptedDriveCatalog drives = new();
        ScriptedWslDistributionCatalog wslRoots = new();
        LocationsSession session = new(drives, wslRoots);
        DualPaneSnapshot panes = await PanesAsync(PaneSide.Left);
        Task<LocationsState> opening = session.OpenAsync(panes, InteractionOwnership.ScopeOwnsInput, CancellationToken.None);
        LocationsState loading = session.Current;

        LocationsState whileLoading = await session.OpenAsync(panes, InteractionOwnership.ScopeOwnsInput, CancellationToken.None);
        drives.Complete(DrivesOf());
        wslRoots.Complete(RootsOf());
        LocationsState open = await opening;
        LocationsState whileOpen = await session.OpenAsync(panes, InteractionOwnership.ScopeOwnsInput, CancellationToken.None);

        Assert.AreSame(loading, whileLoading);
        Assert.AreSame(open, whileOpen);
        Assert.AreEqual(1, drives.CallCount);
        Assert.AreEqual(1, wslRoots.CallCount);
    }

    /// <summary>
    /// Proves every combination of section success and failure: each section is decided from its
    /// own outcome, a failure keeps its reason, and the listed entries are drives first.
    /// </summary>
    [TestMethod]
    [DataRow("listed", "listed")]
    [DataRow("listed", "failed")]
    [DataRow("failed", "listed")]
    [DataRow("failed", "failed")]
    public async Task OpenAsyncWhenSectionsSettleDecidesEachIndependentlyAsync(string driveResult, string wslResult)
    {
        DriveLocation fixedDrive = Drive("C:\\", DriveKind.Fixed, "System");
        DriveLocation removable = Drive("E:\\", DriveKind.Removable, null);
        DriveCatalogOutcome driveOutcome = driveResult == "listed"
            ? DriveCatalogOutcome.Succeeded([fixedDrive, removable], 1)
            : DriveCatalogOutcome.Failed(DriveCatalogFailureKind.AccessDenied);
        WslDistributionCatalogOutcome wslOutcome = wslResult == "listed"
            ? RootsOf("Ubuntu")
            : WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.MalformedOutput);

        LocationsOpen open = await OpenAsync(driveOutcome, wslOutcome);

        int expectedCount = (driveResult == "listed" ? 2 : 0) + (wslResult == "listed" ? 1 : 0);
        Assert.HasCount(expectedCount, open.Items);
        if (driveResult == "listed")
        {
            DriveSectionListed listed = Assert.IsInstanceOfType<DriveSectionListed>(open.Drives);
            Assert.AreEqual(1, listed.UnrepresentableRootCount);
            Assert.AreSame(fixedDrive, listed.Items[0].Drive);
            Assert.AreSame(removable, listed.Items[1].Drive);
            Assert.AreSame(listed.Items[0], open.Items[0]);
            Assert.AreSame(listed.Items[1], open.Items[1]);
        }
        else
        {
            Assert.AreSame(
                DriveCatalogFailureKind.AccessDenied,
                Assert.IsInstanceOfType<DriveSectionFailed>(open.Drives).Failure);
        }
        if (wslResult == "listed")
        {
            WslRootSectionListed listed = Assert.IsInstanceOfType<WslRootSectionListed>(open.WslRoots);
            Assert.AreEqual("Ubuntu", listed.Items[0].Root.DistributionName);
            Assert.AreSame(listed.Items[0], open.Items[^1]);
        }
        else
        {
            Assert.AreSame(
                WslDistributionCatalogFailureKind.MalformedOutput,
                Assert.IsInstanceOfType<WslRootSectionFailed>(open.WslRoots).Failure);
        }
    }

    /// <summary>Proves cancellation of either read, alone or with any other outcome, closes the picker.</summary>
    [TestMethod]
    [DataRow("cancelled", "listed")]
    [DataRow("cancelled", "failed")]
    [DataRow("cancelled", "cancelled")]
    [DataRow("listed", "cancelled")]
    [DataRow("failed", "cancelled")]
    public async Task OpenAsyncWhenEitherReadIsCancelledClosesAsync(string driveResult, string wslResult)
    {
        DriveCatalogOutcome driveOutcome = driveResult switch
        {
            "cancelled" => DriveCatalogOutcome.Cancelled(),
            "failed" => DriveCatalogOutcome.Failed(DriveCatalogFailureKind.ProviderUnavailable),
            _ => DrivesOf(Drive("C:\\", DriveKind.Fixed, null)),
        };
        WslDistributionCatalogOutcome wslOutcome = wslResult switch
        {
            "cancelled" => WslDistributionCatalogOutcome.Cancelled(),
            "failed" => WslDistributionCatalogOutcome.Failed(WslDistributionCatalogFailureKind.ProviderUnavailable),
            _ => RootsOf("Ubuntu"),
        };
        LocationsSession session = new(
            ScriptedDriveCatalog.Completed(driveOutcome),
            ScriptedWslDistributionCatalog.Completed(wslOutcome));

        LocationsState state = await session.OpenAsync(
            await PanesAsync(PaneSide.Left),
            InteractionOwnership.ScopeOwnsInput,
            CancellationToken.None);

        Assert.AreSame(LocationsState.Closed, state);
        Assert.AreSame(LocationsState.Closed, session.Current);
    }

    /// <summary>Proves a picker with nothing listed has no focus item and focus moves change nothing.</summary>
    [TestMethod]
    public async Task ValidateWhenNothingIsListedKeepsTheStateOnEveryMoveAsync()
    {
        LocationsSession session = await OpenedAsync(DrivesOf(), RootsOf());
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(session.Current);

        _ = session.Validate(UserIntent.MoveNext, InteractionOwnership.ScopeOwnsInput);
        _ = session.Validate(UserIntent.MovePrevious, InteractionOwnership.ScopeOwnsInput);
        LocationsValidation confirm = session.Validate(UserIntent.Confirm, InteractionOwnership.ScopeOwnsInput);

        Assert.IsEmpty(open.Items);
        Assert.IsNull(open.FocusItem);
        Assert.AreSame(open, session.Current);
        Assert.AreSame(LocationsValidation.NothingToRoute, confirm);
    }

    /// <summary>Proves focus starts on the first entry, stops at both ends, and crosses sections.</summary>
    [TestMethod]
    public async Task ValidateWhenFocusMovesStopsAtBothBoundariesAsync()
    {
        LocationsSession session = await OpenedAsync(
            DrivesOf(Drive("C:\\", DriveKind.Fixed, null)),
            RootsOf("Ubuntu"));
        LocationsOpen first = Assert.IsInstanceOfType<LocationsOpen>(session.Current);

        _ = session.Validate(UserIntent.MovePrevious, InteractionOwnership.ScopeOwnsInput);
        Assert.AreSame(first, session.Current);
        Assert.AreSame(first.Items[0], first.FocusItem);
        LocationsValidation moved = session.Validate(UserIntent.MoveNext, InteractionOwnership.ScopeOwnsInput);
        LocationsOpen second = Assert.IsInstanceOfType<LocationsOpen>(session.Current);
        _ = session.Validate(UserIntent.MoveNext, InteractionOwnership.ScopeOwnsInput);
        Assert.AreSame(second, session.Current);
        _ = session.Validate(UserIntent.MovePrevious, InteractionOwnership.ScopeOwnsInput);
        LocationsOpen back = Assert.IsInstanceOfType<LocationsOpen>(session.Current);

        Assert.AreSame(LocationsValidation.NothingToRoute, moved);
        Assert.AreNotSame(first, second);
        _ = Assert.IsInstanceOfType<WslLocationItem>(second.FocusItem);
        Assert.AreSame(first.Items[1], second.FocusItem);
        Assert.AreSame(first.Drives, second.Drives);
        Assert.AreSame(first.WslRoots, second.WslRoots);
        Assert.AreSame(first.ActiveSide, second.ActiveSide);
        Assert.AreSame(first.Items[0], back.FocusItem);
    }

    /// <summary>Proves an accepted selection closes the picker first and names the pane and root once.</summary>
    [TestMethod]
    public async Task ValidateWhenSelectionIsQualifiedClosesAndNamesTheTargetAsync()
    {
        LocationsSession session = await OpenedAsync(
            DrivesOf(Drive("C:\\", DriveKind.Fixed, null)),
            RootsOf("Ubuntu"),
            PaneSide.Right);
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(session.Current);
        LocationItem wsl = open.Items[1];

        LocationsValidation validation = session.Validate(
            UserIntent.SelectLocation(open, wsl),
            InteractionOwnership.ScopeOwnsInput);

        LocationTargetAccepted accepted = Assert.IsInstanceOfType<LocationTargetAccepted>(validation);
        Assert.AreSame(PaneSide.Right, accepted.Side);
        Assert.AreSame(wsl.Location, accepted.Target);
        Assert.AreSame(LocationsState.Closed, session.Current);
        Assert.AreSame(
            LocationsValidation.NothingToRoute,
            session.Validate(UserIntent.SelectLocation(open, wsl), InteractionOwnership.ScopeOwnsInput));
    }

    /// <summary>Proves a selection qualified by a stale state or naming a foreign entry changes nothing.</summary>
    [TestMethod]
    public async Task ValidateWhenSelectionIsStaleOrForeignChangesNothingAsync()
    {
        LocationsSession session = await OpenedAsync(
            DrivesOf(Drive("C:\\", DriveKind.Fixed, null), Drive("D:\\", DriveKind.Fixed, null)),
            RootsOf());
        LocationsOpen stale = Assert.IsInstanceOfType<LocationsOpen>(session.Current);
        _ = session.Validate(UserIntent.MoveNext, InteractionOwnership.ScopeOwnsInput);
        LocationsOpen current = Assert.IsInstanceOfType<LocationsOpen>(session.Current);
        LocationsOpen other = Assert.IsInstanceOfType<LocationsOpen>(
            (await OpenedAsync(DrivesOf(Drive("Z:\\", DriveKind.Network, null)), RootsOf())).Current);

        LocationsValidation staleSelection = session.Validate(
            UserIntent.SelectLocation(stale, stale.Items[0]),
            InteractionOwnership.ScopeOwnsInput);
        LocationsValidation foreignSelection = session.Validate(
            UserIntent.SelectLocation(current, other.Items[0]),
            InteractionOwnership.ScopeOwnsInput);
        LocationsValidation staleCancellation = session.Validate(
            UserIntent.CancelLocations(stale),
            InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(LocationsValidation.NothingToRoute, staleSelection);
        Assert.AreSame(LocationsValidation.NothingToRoute, foreignSelection);
        Assert.AreSame(LocationsValidation.NothingToRoute, staleCancellation);
        Assert.AreSame(current, session.Current);
    }

    /// <summary>Proves a selection while another scope owns input closes the picker without a route.</summary>
    [TestMethod]
    public async Task ValidateWhenOwnershipIsLostClosesWithoutRouteAsync()
    {
        LocationsSession session = await OpenedAsync(DrivesOf(Drive("C:\\", DriveKind.Fixed, null)), RootsOf());
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(session.Current);

        LocationsValidation validation = session.Validate(
            UserIntent.SelectLocation(open, open.Items[0]),
            InteractionOwnership.AnotherScopeOwnsInput);

        Assert.AreSame(LocationsValidation.NothingToRoute, validation);
        Assert.AreSame(LocationsState.Closed, session.Current);
    }

    /// <summary>Proves a qualified cancellation closes the picker without a route.</summary>
    [TestMethod]
    public async Task ValidateWhenCancellationIsQualifiedClosesAsync()
    {
        LocationsSession session = await OpenedAsync(DrivesOf(Drive("C:\\", DriveKind.Fixed, null)), RootsOf());
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(session.Current);

        LocationsValidation validation = session.Validate(
            UserIntent.CancelLocations(open),
            InteractionOwnership.AnotherScopeOwnsInput);

        Assert.AreSame(LocationsValidation.NothingToRoute, validation);
        Assert.AreSame(LocationsState.Closed, session.Current);
    }

    /// <summary>Proves unqualified intents leave the open picker unchanged.</summary>
    [TestMethod]
    public async Task ValidateWhenIntentIsNotAPickerIntentChangesNothingAsync()
    {
        LocationsSession session = await OpenedAsync(
            DrivesOf(Drive("C:\\", DriveKind.Fixed, null), Drive("D:\\", DriveKind.Fixed, null)),
            RootsOf());
        LocationsOpen open = Assert.IsInstanceOfType<LocationsOpen>(session.Current);

        foreach (UserIntent intent in new[] { UserIntent.Escape, UserIntent.Confirm, UserIntent.FocusLast, UserIntent.OpenLocations })
        {
            Assert.AreSame(LocationsValidation.NothingToRoute, session.Validate(intent, InteractionOwnership.ScopeOwnsInput));
        }

        Assert.AreSame(open, session.Current);
    }

    /// <summary>Proves a closed or loading picker routes nothing and moves nothing.</summary>
    [TestMethod]
    public async Task ValidateWhenClosedOrLoadingRoutesNothingAsync()
    {
        ScriptedDriveCatalog drives = new();
        ScriptedWslDistributionCatalog wslRoots = new();
        LocationsSession session = new(drives, wslRoots);
        LocationsOpen foreign = Assert.IsInstanceOfType<LocationsOpen>(
            (await OpenedAsync(DrivesOf(Drive("C:\\", DriveKind.Fixed, null)), RootsOf())).Current);

        LocationsValidation whileClosed = session.Validate(
            UserIntent.SelectLocation(foreign, foreign.Items[0]),
            InteractionOwnership.ScopeOwnsInput);
        Task<LocationsState> opening = session.OpenAsync(
            await PanesAsync(PaneSide.Left),
            InteractionOwnership.ScopeOwnsInput,
            CancellationToken.None);
        LocationsState loading = session.Current;
        LocationsValidation whileLoading = session.Validate(UserIntent.MoveNext, InteractionOwnership.ScopeOwnsInput);
        LocationsValidation cancelWhileLoading = session.Validate(
            UserIntent.CancelLocations(foreign),
            InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(LocationsValidation.NothingToRoute, whileClosed);
        Assert.AreSame(LocationsValidation.NothingToRoute, whileLoading);
        Assert.AreSame(LocationsValidation.NothingToRoute, cancelWhileLoading);
        Assert.AreSame(loading, session.Current);
        drives.Complete(DrivesOf());
        wslRoots.Complete(RootsOf());
        _ = await opening;
    }

    /// <summary>Proves every public operation rejects absent arguments.</summary>
    [TestMethod]
    public async Task OperationsWhenArgumentIsNullThrowArgumentNullExceptionAsync()
    {
        LocationsSession session = new(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog());
        DualPaneSnapshot panes = await PanesAsync(PaneSide.Left);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsSession(null!, new ScriptedWslDistributionCatalog()));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsSession(new ScriptedDriveCatalog(), null!));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => session.OpenAsync(null!, InteractionOwnership.ScopeOwnsInput, CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => session.OpenAsync(panes, null!, CancellationToken.None));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Validate(null!, InteractionOwnership.ScopeOwnsInput));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Validate(UserIntent.Escape, null!));
        Assert.AreSame(LocationsState.Closed, session.Current);
    }

    internal static DriveLocation Drive(string root, DriveKind kind, string? label)
    {
        return DriveLocation.Create(Assert.IsInstanceOfType<WindowsLocalPath>(Parse(root)), kind, label);
    }

    internal static DriveCatalogOutcome DrivesOf(params DriveLocation[] drives)
    {
        return DriveCatalogOutcome.Succeeded(drives, 0);
    }

    internal static WslDistributionCatalogOutcome RootsOf(params string[] names)
    {
        WslPath[] roots = new WslPath[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            roots[index] = Assert.IsInstanceOfType<WslPath>(Parse("\\\\wsl.localhost\\" + names[index]));
        }
        return WslDistributionCatalogOutcome.Succeeded(roots);
    }

    private static async Task<LocationsOpen> OpenAsync(
        DriveCatalogOutcome drives,
        WslDistributionCatalogOutcome wslRoots)
    {
        return Assert.IsInstanceOfType<LocationsOpen>((await OpenedAsync(drives, wslRoots)).Current);
    }

    private static Task<LocationsSession> OpenedAsync(
        DriveCatalogOutcome drives,
        WslDistributionCatalogOutcome wslRoots)
    {
        return OpenedAsync(drives, wslRoots, PaneSide.Left);
    }

    private static async Task<LocationsSession> OpenedAsync(
        DriveCatalogOutcome drives,
        WslDistributionCatalogOutcome wslRoots,
        PaneSide active)
    {
        LocationsSession session = new(
            ScriptedDriveCatalog.Completed(drives),
            ScriptedWslDistributionCatalog.Completed(wslRoots));
        _ = await session.OpenAsync(await PanesAsync(active), InteractionOwnership.ScopeOwnsInput, CancellationToken.None);
        return session;
    }

    private static async Task<DualPaneSnapshot> PanesAsync(PaneSide active)
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));
        DualPaneSession panes = new(
            new PaneSession(
                ScriptedDirectoryReadPort.Create(),
                new ScriptedFileLauncher(),
                capacity,
                DirectoryListing.EntryBoundaryLimit,
                HiddenItemVisibility.Hidden),
            new PaneSession(
                ScriptedDirectoryReadPort.Create(),
                new ScriptedFileLauncher(),
                capacity,
                DirectoryListing.EntryBoundaryLimit,
                HiddenItemVisibility.Hidden),
            gateway);
        return active == PaneSide.Left
            ? panes.Current
            : await panes.HandleAsync(UserIntent.ActivateOtherPane, RecordingDualPaneObserver.Create(), CancellationToken.None);
    }

    private static FileSystemPath Parse(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
