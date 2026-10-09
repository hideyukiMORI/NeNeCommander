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
using NeNeCommander.Presentation.WinUI.Locations;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the Locations picker projection: row text, loading, section failures, and key qualification.</summary>
[TestClass]
public sealed class LocationsPresenterTests
{
    /// <summary>Proves drive rows show letter, label, and kind, and WSL rows show the distribution name.</summary>
    [TestMethod]
    public async Task PresentWhenBothSectionsAreListedProjectsEveryRowAsync()
    {
        LocationsOpen open = await OpenAsync(
            DriveCatalogOutcome.Succeeded(
                [
                    Drive("C:\\", DriveKind.Fixed, "System"),
                    Drive("E:\\", DriveKind.Removable, null),
                    Drive("Z:\\", DriveKind.Network, "Share"),
                    Drive("F:\\", DriveKind.Optical, null),
                    Drive("G:\\", DriveKind.Unknown, null),
                ],
                0),
            Roots("Ubuntu"));

        LocationsPresentation presentation = LocationsPresenter.Present(open, Localize);

        Assert.IsTrue(presentation.IsShown);
        Assert.AreSame(open, presentation.SourceState);
        Assert.HasCount(6, presentation.Rows);
        AssertRow(presentation.Rows[0], open.Items[0], "C:", "System · Fixed");
        AssertRow(presentation.Rows[1], open.Items[1], "E:", "Removable");
        AssertRow(presentation.Rows[2], open.Items[2], "Z:", "Share · Network");
        AssertRow(presentation.Rows[3], open.Items[3], "F:", "Optical");
        AssertRow(presentation.Rows[4], open.Items[4], "G:", "Unknown");
        AssertRow(presentation.Rows[5], open.Items[5], "Ubuntu", "WSL");
        Assert.AreSame(presentation.Rows[0], presentation.FocusRow);
        Assert.AreEqual(string.Empty, presentation.Texts.Status);
        Assert.AreEqual(string.Empty, presentation.Texts.Drives);
        Assert.AreEqual(string.Empty, presentation.Texts.WslRoots);
        Assert.HasCount(4, presentation.KeyHints);
    }

    /// <summary>Proves the focus row follows the Application focus item.</summary>
    [TestMethod]
    public async Task PresentWhenFocusMovedSelectsTheMatchingRowAsync()
    {
        LocationsSession session = Session(
            DriveCatalogOutcome.Succeeded([Drive("C:\\", DriveKind.Fixed, null)], 0),
            Roots("Ubuntu"));
        _ = await session.OpenAsync(await PanesAsync(), InteractionOwnership.ScopeOwnsInput, CancellationToken.None);
        _ = session.Validate(UserIntent.MoveNext, InteractionOwnership.ScopeOwnsInput);

        LocationsPresentation presentation = LocationsPresenter.Present(session.Current, Localize);

        Assert.AreSame(presentation.Rows[1], presentation.FocusRow);
    }

    /// <summary>Proves loading is shown with its status and no rows, and closed is not shown.</summary>
    [TestMethod]
    public async Task PresentWhenLoadingOrClosedProjectsStatusOnlyAsync()
    {
        LocationsSession session = new(new PendingDriveCatalog(), new PendingWslDistributionCatalog());
        _ = session.OpenAsync(await PanesAsync(), InteractionOwnership.ScopeOwnsInput, CancellationToken.None);

        LocationsPresentation loading = LocationsPresenter.Present(session.Current, Localize);
        LocationsPresentation closed = LocationsPresenter.Present(LocationsState.Closed, Localize);

        _ = Assert.IsInstanceOfType<LocationsLoading>(loading.SourceState);
        Assert.IsTrue(loading.IsShown);
        Assert.AreEqual("Loading", loading.Texts.Status);
        Assert.IsEmpty(loading.Rows);
        Assert.IsNull(loading.FocusRow);
        Assert.IsFalse(closed.IsShown);
        Assert.AreEqual(string.Empty, closed.Texts.Status);
        Assert.AreEqual(string.Empty, closed.Texts.Drives);
        Assert.AreEqual(string.Empty, closed.Texts.WslRoots);
        Assert.IsEmpty(closed.Rows);
        Assert.IsNull(closed.FocusRow);
    }

    /// <summary>Proves each section failure is named by its own text while the other section stays listed.</summary>
    [TestMethod]
    [DataRow("provider", "malformed")]
    [DataRow("access", "provider")]
    public async Task PresentWhenSectionFailsShowsItsOwnReasonAsync(string driveFailure, string wslFailure)
    {
        LocationsOpen drivesFailed = await OpenAsync(
            DriveCatalogOutcome.Failed(driveFailure == "provider"
                ? DriveCatalogFailureKind.ProviderUnavailable
                : DriveCatalogFailureKind.AccessDenied),
            Roots("Debian"));
        LocationsOpen wslFailed = await OpenAsync(
            DriveCatalogOutcome.Succeeded([Drive("C:\\", DriveKind.Fixed, null)], 0),
            WslDistributionCatalogOutcome.Failed(wslFailure == "provider"
                ? WslDistributionCatalogFailureKind.ProviderUnavailable
                : WslDistributionCatalogFailureKind.MalformedOutput));

        LocationsPresentation drives = LocationsPresenter.Present(drivesFailed, Localize);
        LocationsPresentation wsl = LocationsPresenter.Present(wslFailed, Localize);

        Assert.AreEqual(driveFailure == "provider" ? "Drives unavailable" : "Drives denied", drives.Texts.Drives);
        Assert.AreEqual(string.Empty, drives.Texts.WslRoots);
        Assert.HasCount(1, drives.Rows);
        Assert.AreEqual("Debian", drives.Rows[0].NameText);
        Assert.AreEqual(wslFailure == "provider" ? "WSL unavailable" : "WSL malformed", wsl.Texts.WslRoots);
        Assert.AreEqual(string.Empty, wsl.Texts.Drives);
        Assert.HasCount(1, wsl.Rows);
    }

    /// <summary>Proves an empty picker says so, and unshown roots are counted in the drives text.</summary>
    [TestMethod]
    public async Task PresentWhenNothingIsListedOrRootsAreUnshownSaysSoAsync()
    {
        LocationsOpen empty = await OpenAsync(DriveCatalogOutcome.Succeeded([], 0), Roots());
        LocationsOpen unshown = await OpenAsync(
            DriveCatalogOutcome.Succeeded([Drive("C:\\", DriveKind.Fixed, null)], 2),
            Roots());

        LocationsPresentation emptyPresentation = LocationsPresenter.Present(empty, Localize);
        LocationsPresentation unshownPresentation = LocationsPresenter.Present(unshown, Localize);

        Assert.AreEqual("Nothing", emptyPresentation.Texts.Status);
        Assert.IsNull(emptyPresentation.FocusRow);
        Assert.AreEqual(string.Empty, emptyPresentation.Texts.Drives);
        Assert.AreEqual("Unshown 2", unshownPresentation.Texts.Drives);
        Assert.AreEqual(string.Empty, unshownPresentation.Texts.Status);
    }

    /// <summary>Proves Enter selects the focused entry and Escape closes, both qualified by the rendered state.</summary>
    [TestMethod]
    public async Task QualifyWhenPickerIsOpenQualifiesEnterAndEscapeAsync()
    {
        LocationsOpen open = await OpenAsync(
            DriveCatalogOutcome.Succeeded([Drive("C:\\", DriveKind.Fixed, null)], 0),
            Roots());

        LocationSelection selection = Assert.IsInstanceOfType<LocationSelection>(
            LocationsPresenter.Qualify(UserIntent.Confirm, open));
        LocationsCancellation cancellation = Assert.IsInstanceOfType<LocationsCancellation>(
            LocationsPresenter.Qualify(UserIntent.Escape, open));

        LocationsOpen selectionState = selection.ExpectedState;
        Assert.AreSame(open, selectionState);
        Assert.AreSame(open.Items[0], selection.Item);
        LocationsOpen cancellationState = cancellation.ExpectedState;
        Assert.AreSame(open, cancellationState);
        Assert.AreSame(UserIntent.MoveNext, LocationsPresenter.Qualify(UserIntent.MoveNext, open));
    }

    /// <summary>Proves Enter with nothing listed, and every intent while not open, pass unchanged to the session.</summary>
    [TestMethod]
    public async Task QualifyWhenNothingIsFocusedOrPickerIsNotOpenReturnsTheIntentAsync()
    {
        LocationsOpen empty = await OpenAsync(DriveCatalogOutcome.Succeeded([], 0), Roots());

        Assert.AreSame(UserIntent.Confirm, LocationsPresenter.Qualify(UserIntent.Confirm, empty));
        Assert.AreSame(UserIntent.Escape, LocationsPresenter.Qualify(UserIntent.Escape, LocationsState.Closed));
        Assert.AreSame(UserIntent.Confirm, LocationsPresenter.Qualify(UserIntent.Confirm, LocationsState.Closed));
    }

    /// <summary>Proves the presenter and its records reject absent arguments.</summary>
    [TestMethod]
    public async Task PresenterWhenArgumentIsNullThrowsArgumentNullExceptionAsync()
    {
        LocationsOpen open = await OpenAsync(
            DriveCatalogOutcome.Succeeded([Drive("C:\\", DriveKind.Fixed, null)], 0),
            Roots());
        LocationsPresentationTexts texts = new(string.Empty, string.Empty, string.Empty);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => LocationsPresenter.Present(null!, Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => LocationsPresenter.Present(open, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => LocationsPresenter.Qualify(null!, open));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => LocationsPresenter.Qualify(UserIntent.Escape, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentationTexts(null!, string.Empty, string.Empty));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentationTexts(string.Empty, null!, string.Empty));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentationTexts(string.Empty, string.Empty, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentation(null!, texts, [], []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentation(open, null!, [], []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentation(LocationsState.Closed, texts, null!, []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationsPresentation(LocationsState.Closed, texts, [], null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationRow(null!, "C:", "Fixed", "C:, Fixed", "LocationsRow_C:"));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new LocationRow(open.Items[0], " ", "Fixed", "C:, Fixed", "LocationsRow_C:"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationRow(open.Items[0], "C:", null!, "C:, Fixed", "LocationsRow_C:"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new LocationRow(open.Items[0], "C:", "Fixed", null!, "LocationsRow_C:"));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new LocationRow(open.Items[0], "C:", "Fixed", "C:, Fixed", " "));
    }

    private static void AssertRow(LocationRow row, LocationItem item, string name, string detail)
    {
        Assert.AreSame(item, row.Item);
        Assert.AreEqual(name, row.NameText);
        Assert.AreEqual(detail, row.DetailText);
        Assert.AreEqual(name + ", " + detail, row.AutomationName);
        Assert.AreEqual("LocationsRow_" + name, row.AutomationId);
    }

    private static string Localize(string key)
    {
        return key switch
        {
            "LocationsDriveKindFixed" => "Fixed",
            "LocationsDriveKindRemovable" => "Removable",
            "LocationsDriveKindNetwork" => "Network",
            "LocationsDriveKindOptical" => "Optical",
            "LocationsDriveKindUnknown" => "Unknown",
            "LocationsDriveDetailFormat" => "{0} · {1}",
            "LocationsWslDetail" => "WSL",
            "LocationsRowAutomationNameFormat" => "{0}, {1}",
            "LocationsLoading" => "Loading",
            "LocationsEmpty" => "Nothing",
            "LocationsDrivesProviderUnavailable" => "Drives unavailable",
            "LocationsDrivesAccessDenied" => "Drives denied",
            "LocationsDrivesUnrepresentableFormat" => "Unshown {0}",
            "LocationsWslProviderUnavailable" => "WSL unavailable",
            "LocationsWslMalformedOutput" => "WSL malformed",
            _ => throw new AssertFailedException("Unexpected resource " + key),
        };
    }

    private static async Task<LocationsOpen> OpenAsync(DriveCatalogOutcome drives, WslDistributionCatalogOutcome wslRoots)
    {
        LocationsSession session = Session(drives, wslRoots);
        return Assert.IsInstanceOfType<LocationsOpen>(
            await session.OpenAsync(await PanesAsync(), InteractionOwnership.ScopeOwnsInput, CancellationToken.None));
    }

    private static LocationsSession Session(DriveCatalogOutcome drives, WslDistributionCatalogOutcome wslRoots)
    {
        FixedLocationCatalogs catalogs = new(drives, wslRoots);
        return new LocationsSession(catalogs, catalogs);
    }

    private static Task<DualPaneSnapshot> PanesAsync()
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        using FileOperationGateway gateway = new(QueuedFileOperationPort.Create());
        DualPaneSession panes = new(
            new PaneSession(
                ScriptedDirectoryReadPort.Create(),
                new AcceptedFileLauncher(),
                capacity,
                DirectoryListing.EntryBoundaryLimit,
                HiddenItemVisibility.Hidden),
            new PaneSession(
                ScriptedDirectoryReadPort.Create(),
                new AcceptedFileLauncher(),
                capacity,
                DirectoryListing.EntryBoundaryLimit,
                HiddenItemVisibility.Hidden),
            gateway);
        return Task.FromResult(panes.Current);
    }

    private static DriveLocation Drive(string root, DriveKind kind, string? label)
    {
        return DriveLocation.Create(
            Assert.IsInstanceOfType<WindowsLocalPath>(
                Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(root)).Path),
            kind,
            label);
    }

    private static WslDistributionCatalogOutcome Roots(params string[] names)
    {
        WslPath[] roots = new WslPath[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            roots[index] = Assert.IsInstanceOfType<WslPath>(
                Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse("\\\\wsl.localhost\\" + names[index])).Path);
        }
        return WslDistributionCatalogOutcome.Succeeded(roots);
    }
}
