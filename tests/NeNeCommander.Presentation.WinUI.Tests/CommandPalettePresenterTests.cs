using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves localized command filtering and Presentation-owned selection.</summary>
[TestClass]
public sealed class CommandPalettePresenterTests
{
    /// <summary>Proves empty, title, shortcut, and zero-result queries preserve the specified behavior.</summary>
    [TestMethod]
    public async Task PresentWhenQueryChangesFiltersLocalizedRowsAndSelectsFirstAsync()
    {
        CommandPaletteOpen open = await OpenPaletteAsync(null);

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, Localize);

        Assert.HasCount(15, view.Rows);
        Assert.AreSame(UserIntent.OpenFocused, view.SelectedRow!.Intent);
        Assert.AreEqual("Open item", view.SelectedRow.Title);
        Assert.AreEqual("L", view.SelectedRow.Shortcut);

        view.MoveNext();
        Assert.AreSame(UserIntent.NavigateParent, view.SelectedRow!.Intent);
        view.UpdateQuery("cOpY");
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(UserIntent.Copy, view.SelectedRow!.Intent);
        view.UpdateQuery("F5");
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(UserIntent.Copy, view.SelectedRow!.Intent);
        view.UpdateQuery("no-match");
        Assert.IsEmpty(view.Rows);
        Assert.IsNull(view.SelectedRow);
        view.MoveNext();
        view.MovePrevious();
        Assert.IsNull(view.SelectedRow);
        view.UpdateQuery(string.Empty);
        Assert.AreSame(UserIntent.OpenFocused, view.SelectedRow!.Intent);
    }

    /// <summary>Proves unavailable rows remain selected and expose complete localized detail and UIA text.</summary>
    [TestMethod]
    public async Task PresentWhenCandidateIsUnavailableKeepsReadableSelectableRowAsync()
    {
        CommandPaletteViewState view = CommandPalettePresenter.Present(
            await OpenPaletteAsync(null),
            Localize);

        view.UpdateQuery("Copy");

        CommandPaletteRow selected = view.SelectedRow!;
        Assert.IsFalse(selected.IsAvailable);
        Assert.AreEqual("Opposite pane is unavailable", selected.Reason);
        Assert.AreEqual("!", selected.StateMarker);
        StringAssert.Contains(selected.Detail, "Opposite pane is unavailable");
        StringAssert.Contains(selected.AutomationName, "Unavailable");
        StringAssert.Contains(selected.AutomationName, "F5");
        StringAssert.Contains(selected.AutomationName, "Target: left pane");
    }

    /// <summary>Proves selection clamps at both ends and starts over after every query change.</summary>
    [TestMethod]
    public async Task SelectionWhenMovedClampsAndQueryResetsToFirstAsync()
    {
        CommandPaletteViewState view = CommandPalettePresenter.Present(
            await OpenPaletteAsync(DirectoryReadOutcome.Succeeded(Listing("C:\\right", "other.txt"))),
            Localize);

        view.MovePrevious();
        Assert.AreSame(UserIntent.OpenFocused, view.SelectedRow!.Intent);
        for (int index = 0; index < 20; index++)
        {
            view.MoveNext();
        }
        Assert.AreSame(UserIntent.OpenSettings, view.SelectedRow!.Intent);
        view.UpdateQuery("IntentLabelNavigate");
        Assert.HasCount(3, view.Rows);
        Assert.AreSame(UserIntent.NavigateParent, view.SelectedRow!.Intent);
    }

    /// <summary>Proves absence at either presenter boundary is rejected.</summary>
    [TestMethod]
    public async Task PresentWhenRequiredInputIsNullThrowsAsync()
    {
        CommandPaletteOpen open = await OpenPaletteAsync(null);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CommandPalettePresenter.Present(null!, Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CommandPalettePresenter.Present(open, null!));
        CommandPaletteViewState view = CommandPalettePresenter.Present(open, Localize);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => view.UpdateQuery(null!));
    }

    private static async Task<CommandPaletteOpen> OpenPaletteAsync(
        DirectoryReadOutcome? passiveListing)
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(Listing("C:\\left", "item.txt")));
        if (passiveListing is not null)
        {
            right.Enqueue(passiveListing);
        }
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(4)).Capacity;
        PaneSession leftPane = new(
            left,
            new AcceptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        PaneSession rightPane = new(
            right,
            new AcceptedFileLauncher(),
            capacity,
            DirectoryListing.EntryBoundaryLimit,
            HiddenItemVisibility.Hidden);
        using FileOperationGateway gateway = new(QueuedFileOperationPort.Create());
        CommanderSession session = new(
            new DualPaneSession(leftPane, rightPane, gateway),
            new SettingsSession(
                new SuccessfulSettingsStore(),
                SettingsReadOutcome.Absent(),
                static _ => { }));
        _ = await session.NavigateAsync(PaneSide.Left, ParsePath("C:\\left"), CancellationToken.None);
        if (passiveListing is not null)
        {
            _ = await session.NavigateAsync(PaneSide.Right, ParsePath("C:\\right"), CancellationToken.None);
        }
        CommanderSnapshot snapshot = await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            new SilentCommanderObserver(),
            CancellationToken.None);
        return Assert.IsInstanceOfType<CommandPaletteOpen>(snapshot.CommandPalette);
    }

    private static DirectoryListing Listing(string location, string name)
    {
        FileSystemPath parsedLocation = ParsePath(location);
        DirectoryEntry entry = DirectoryEntry.Create(
            ParsePath(location + "\\" + name),
            name,
            DirectoryEntryKind.File,
            EntryVisibility.Normal);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(DirectoryListing.Create(
            parsedLocation,
            [entry],
            DirectoryListingCompleteness.Complete,
            0)).Listing;
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }

    private static string Localize(string key)
    {
        Dictionary<string, string> values = new()
        {
            ["IntentLabelOpenFocused"] = "Open item",
            ["IntentLabelCopy"] = "Copy Files",
            ["KeyLabelL"] = "L",
            ["KeyLabelF5"] = "F5",
            ["CommandPaletteTargetLeft"] = "Target: left pane",
            ["CommandPaletteTargetRight"] = "Target: right pane",
            ["CommandPaletteOppositeLeft"] = "Opposite: left pane",
            ["CommandPaletteOppositeRight"] = "Opposite: right pane",
            ["CommandPaletteAvailableDetailFormat"] = "{0}; {1}",
            ["CommandPaletteUnavailableDetailFormat"] = "{0}; {1}; Unavailable: {2}",
            ["CommandPaletteAvailableAutomationNameFormat"] = "{0}; {1}; {2}; {3}",
            ["CommandPaletteUnavailableAutomationNameFormat"] =
                "{0}; {1}; {2}; {3}; Unavailable; {4}",
            ["CommandPaletteUnavailableMarker"] = "!",
            ["CommandUnavailablePassivePane"] = "Opposite pane is unavailable",
        };
        return values.TryGetValue(key, out string? value) ? value : key;
    }

    private sealed class SuccessfulSettingsStore : ISettingsStore
    {
        public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(SettingsReadOutcome.Absent());
        }

        public Task<SettingsWriteOutcome> WriteAsync(
            UserSettings settings,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(SettingsWriteOutcome.Succeeded());
        }
    }

    private sealed class SilentCommanderObserver : ICommanderProgressObserver
    {
        public void OperationProgressed(DualPaneSnapshot snapshot)
        {
        }

        public void SettingsProgressed(SettingsSnapshot snapshot)
        {
        }
    }
}
