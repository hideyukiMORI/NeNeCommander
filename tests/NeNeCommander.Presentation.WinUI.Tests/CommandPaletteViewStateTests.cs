using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the CommandPaletteViewState contract.</summary>
[TestClass]
public sealed class CommandPaletteViewStateTests
{
    /// <summary>Proves empty, title, shortcut, and zero-result queries preserve the specified behavior.</summary>
    [TestMethod]
    public async Task PresentWhenQueryChangesFiltersLocalizedRowsAndSelectsFirstAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.Localize);

        Assert.HasCount(22, view.Rows);
        Assert.AreSame(open, view.SourceState);
        Assert.AreEqual(string.Empty, view.Query);
        Assert.AreSame(UserIntent.OpenFocused, view.SelectedRow!.Intent);
        Assert.AreEqual("Open item", view.SelectedRow.Title);
        Assert.AreEqual("L", view.SelectedRow.Shortcut);

        view.MoveNext();
        Assert.AreSame(UserIntent.NavigateParent, view.SelectedRow!.Intent);
        view.UpdateQuery("cOpY");
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(UserIntent.Copy, view.SelectedRow!.Intent);
        view.UpdateQuery("F5");
        Assert.HasCount(2, view.Rows);
        Assert.AreSame(UserIntent.Copy, view.SelectedRow!.Intent);
        Assert.AreSame(UserIntent.SortBySize, view.Rows[1].Intent);
        view.UpdateQuery("no-match");
        Assert.AreEqual("no-match", view.Query);
        Assert.IsEmpty(view.Rows);
        Assert.IsNull(view.SelectedRow);
        view.MoveNext();
        view.MovePrevious();
        Assert.IsNull(view.SelectedRow);
        view.UpdateQuery(string.Empty);
        Assert.AreSame(UserIntent.OpenFocused, view.SelectedRow!.Intent);
    }

    /// <summary>Proves both new scope rows are searchable by their localized title and canonical shortcut.</summary>
    [TestMethod]
    [DataRow("en-US", 0, "Bookmarks", "Ctrl+B")]
    [DataRow("en-US", 1, "Window adjustment", "Ctrl+W")]
    [DataRow("ja-JP", 0, "ブックマーク", "Ctrl+B")]
    [DataRow("ja-JP", 1, "ウィンドウ調整", "Ctrl+W")]
    public async Task PresentWhenScopeEntryIsSearchedUsesLocalizedTitleShortcutAndAutomationAsync(
        string language,
        int command,
        string title,
        string shortcut)
    {
        Dictionary<string, string> labels = new()
        {
            ["IntentLabelOpenBookmarks"] = language == "en-US" ? "Bookmarks" : "ブックマーク",
            ["IntentLabelOpenWindowAdjustment"] = language == "en-US" ? "Window adjustment" : "ウィンドウ調整",
            ["KeyLabelCtrlB"] = "Ctrl+B",
            ["KeyLabelCtrlW"] = "Ctrl+W",
        };
        CommandPaletteViewState view = CommandPalettePresenter.Present(await CommandPaletteTestContext.OpenPaletteAsync(null),
            key => labels.TryGetValue(key, out string? value) ? value : CommandPaletteTestContext.Localize(key));
        UserIntent expected = command == 0 ? UserIntent.OpenBookmarks : UserIntent.OpenWindowAdjustment;
        string labelKey = command == 0 ? "IntentLabelOpenBookmarks" : "IntentLabelOpenWindowAdjustment";

        view.UpdateQuery(title);

        Assert.HasCount(1, view.Rows);
        CommandPaletteRow row = view.Rows[0];
        Assert.AreSame(expected, row.Intent);
        Assert.AreEqual(title, row.Title);
        Assert.AreEqual(shortcut, row.Shortcut);
        Assert.IsTrue(row.IsAvailable);
        Assert.AreEqual("CommandPaletteCandidate_" + labelKey, row.AutomationId);
        StringAssert.Contains(row.AutomationName, title);
        StringAssert.Contains(row.AutomationName, shortcut);
        view.UpdateQuery(shortcut);
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(expected, view.Rows[0].Intent);
    }

    /// <summary>Proves unavailable rows remain selected and expose complete localized detail and UIA text.</summary>
    [TestMethod]
    public async Task PresentWhenCandidateIsUnavailableKeepsReadableSelectableRowAsync()
    {
        CommandPaletteViewState view = CommandPalettePresenter.Present(
            await CommandPaletteTestContext.OpenPaletteAsync(null),
            CommandPaletteTestContext.Localize);

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
            await CommandPaletteTestContext.OpenPaletteAsync(DirectoryReadOutcome.Succeeded(CommandPaletteTestContext.Listing("C:\\right", "other.txt"), null)),
            CommandPaletteTestContext.Localize);

        view.MovePrevious();
        Assert.AreSame(UserIntent.OpenFocused, view.SelectedRow!.Intent);
        for (int index = 0; index < 22; index++)
        {
            view.MoveNext();
        }
        Assert.AreSame(UserIntent.OpenWindowAdjustment, view.SelectedRow!.Intent);
        view.UpdateQuery("IntentLabelNavigate");
        Assert.HasCount(3, view.Rows);
        Assert.AreSame(UserIntent.NavigateParent, view.SelectedRow!.Intent);
    }

    /// <summary>Proves zero-, one-, and many-row projections preserve bounded selection movement.</summary>
    [TestMethod]
    public async Task SelectionBoundariesRemainClosedForZeroOneAndManyRowsAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteViewState many = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);
        CommandPaletteViewState zero = new(open, Array.Empty<CommandPaletteRow>());

        zero.MovePrevious();
        zero.MoveNext();
        Assert.IsNull(zero.SelectedRow);
        Assert.IsEmpty(zero.Rows);

        many.UpdateQuery("<IntentLabelOpenFocused>");
        CommandPaletteRow only = Assert.IsInstanceOfType<CommandPaletteRow>(many.SelectedRow);
        many.MovePrevious();
        many.MoveNext();
        Assert.AreSame(only, many.SelectedRow);

        many.UpdateQuery(string.Empty);
        CommandPaletteRow last = many.Rows[^1];
        Assert.IsTrue(many.Select(last));
        many.MoveNext();
        Assert.AreSame(last, many.SelectedRow);
        many.MovePrevious();
        Assert.AreSame(many.Rows[^2], many.SelectedRow);
    }

    /// <summary>Proves pointer selection accepts only a row owned by the current projection.</summary>
    [TestMethod]
    public async Task SelectWhenPointerChoosesRowRejectsAStaleProjectionAsync()
    {
        CommandPaletteViewState current = CommandPalettePresenter.Present(
            await CommandPaletteTestContext.OpenPaletteAsync(null),
            CommandPaletteTestContext.Localize);
        CommandPaletteViewState stale = CommandPalettePresenter.Present(
            await CommandPaletteTestContext.OpenPaletteAsync(null),
            CommandPaletteTestContext.Localize);

        bool selected = current.Select(current.Rows[1]);
        bool rejected = current.Select(stale.Rows[0]);

        Assert.IsTrue(selected);
        Assert.IsFalse(rejected);
        Assert.AreSame(UserIntent.NavigateParent, current.SelectedRow!.Intent);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => current.Select(null!));
    }

    /// <summary>
    /// Proves a query is rejected for its own absence and not merely by the row predicate, so an
    /// empty projection cannot silently accept an absent query and keep stale selection state.
    /// </summary>
    [TestMethod]
    public async Task UpdateQueryWhenProjectionIsEmptyStillRejectsAnAbsentQueryAsync()
    {
        CommandPaletteViewState empty = new(
            await CommandPaletteTestContext.OpenPaletteAsync(null),
            Array.Empty<CommandPaletteRow>());

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => empty.UpdateQuery(null!));

        Assert.AreEqual(string.Empty, empty.Query);
        Assert.IsEmpty(empty.Rows);
        Assert.IsNull(empty.SelectedRow);
    }
}
