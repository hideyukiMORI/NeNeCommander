using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the CommandPalettePresenter contract.</summary>
[TestClass]
public sealed class CommandPalettePresenterTests
{
    /// <summary>Proves every catalog row preserves its candidate, label, shortcut, scope, and UIA identity.</summary>
    [TestMethod]
    public async Task PresentProjectsTheCompleteCatalogContractAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);

        IReadOnlyList<(UserIntent Intent, string ResourceKey, string ShortcutKey)> expected =
            ExpectedCatalogRows();
        Assert.AreSame(open, view.SourceState);
        Assert.AreEqual(string.Empty, view.Query);
        Assert.AreEqual("<CommandPaletteTargetLeft>", view.Target);
        Assert.AreEqual("<CommandPaletteOppositeRight>", view.Opposite);
        Assert.HasCount(expected.Count, view.Rows);
        for (int index = 0; index < expected.Count; index++)
        {
            (UserIntent intent, string resourceKey, string shortcutKey) = expected[index];
            CommandPaletteRow row = view.Rows[index];
            Assert.AreSame(open.Candidates[index], row.Candidate);
            Assert.AreSame(intent, row.Intent);
            Assert.AreEqual($"<{resourceKey}>", row.Title);
            Assert.AreEqual($"<{shortcutKey}>", row.Shortcut);
            Assert.AreEqual("<CommandPaletteTargetLeft>", row.Target);
            Assert.AreEqual("<CommandPaletteOppositeRight>", row.Opposite);
            Assert.AreEqual("CommandPaletteCandidate_" + resourceKey, row.AutomationId);
        }

        CommandPaletteRow available = view.Rows[0];
        Assert.IsTrue(available.IsAvailable);
        Assert.AreEqual(string.Empty, available.Reason);
        Assert.AreEqual(string.Empty, available.StateMarker);
        Assert.AreEqual(
            "<CommandPaletteTargetLeft> · <CommandPaletteOppositeRight>",
            available.Detail);
        Assert.AreEqual(
            "<IntentLabelOpenFocused>; <KeyLabelL>; <CommandPaletteTargetLeft>; " +
            "<CommandPaletteOppositeRight>",
            available.AutomationName);
        Assert.AreEqual(
            "<CommandPaletteTargetLeft> · <CommandPaletteOppositeRight>",
            view.Context);
    }

    /// <summary>Proves all closed unavailable reasons project exact right-target detail and UIA text.</summary>
    [TestMethod]
    public async Task PresentProjectsEveryUnavailableReasonWithoutChangingCandidateIdentityAsync()
    {
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        IReadOnlyList<(UserIntent Intent, CommandUnavailableReason Reason, string ResourceKey)> expected =
        [
            (UserIntent.OpenFocused, CommandUnavailableReason.FocusRequired, "CommandUnavailableFocusRequired"),
            (UserIntent.Copy, CommandUnavailableReason.SourceRequired, "CommandUnavailableSourceRequired"),
            (UserIntent.Move, CommandUnavailableReason.PassivePaneUnavailable, "CommandUnavailablePassivePane"),
            (UserIntent.NavigateParent, CommandUnavailableReason.ParentUnavailable, "CommandUnavailableParent"),
            (UserIntent.NavigateBack, CommandUnavailableReason.BackUnavailable, "CommandUnavailableBack"),
            (UserIntent.NavigateForward, CommandUnavailableReason.ForwardUnavailable, "CommandUnavailableForward"),
        ];
        List<CommandCandidate> candidates = [.. expected.Select(item =>
            CommandPaletteTestContext.CreateCandidate(item.Intent, CommandPaletteTestContext.CreateUnavailable(item.Reason)))];
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, PaneSide.Right, candidates.AsReadOnly());

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource);

        Assert.AreEqual("<CommandPaletteTargetRight>", view.Target);
        Assert.AreEqual("<CommandPaletteOppositeLeft>", view.Opposite);
        Assert.HasCount(expected.Count, view.Rows);
        for (int index = 0; index < expected.Count; index++)
        {
            (UserIntent intent, _, string resourceKey) = expected[index];
            CommandPaletteRow row = view.Rows[index];
            Assert.AreSame(candidates[index], row.Candidate);
            Assert.AreSame(intent, row.Intent);
            Assert.IsFalse(row.IsAvailable);
            Assert.AreEqual($"<{resourceKey}>", row.Reason);
            Assert.AreEqual("<CommandPaletteUnavailableMarker>", row.StateMarker);
            Assert.AreEqual(
                $"<CommandPaletteTargetRight>|<CommandPaletteOppositeLeft>|<{resourceKey}>",
                row.Detail);
            Assert.AreEqual(
                $"{row.Title};{row.Shortcut};<CommandPaletteTargetRight>;" +
                $"<CommandPaletteOppositeLeft>;<{resourceKey}>",
                row.AutomationName);
        }
    }

    /// <summary>Proves absence at either presenter boundary is rejected.</summary>
    [TestMethod]
    public async Task PresentWhenRequiredInputIsNullThrowsAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CommandPalettePresenter.Present(null!, CommandPaletteTestContext.Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CommandPalettePresenter.Present(open, null!));
        CommandPaletteViewState view = CommandPalettePresenter.Present(open, CommandPaletteTestContext.Localize);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => view.UpdateQuery(null!));
    }

    /// <summary>
    /// Proves every projected row reads its shortcut from the canonical file-list key map, so a
    /// candidate the map does not declare fails loudly instead of rendering an absent shortcut.
    /// </summary>
    [TestMethod]
    public async Task PresentWhenCandidateHasNoCanonicalFileListShortcutThrowsAsync()
    {
        CommandPaletteOpen template = await CommandPaletteTestContext.OpenPaletteAsync(null);
        List<CommandCandidate> unbound =
        [
            CommandPaletteTestContext.CreateCandidate(UserIntent.Confirm, CommandAvailability.Available),
        ];
        CommandPaletteOpen open = CommandPaletteTestContext.CreateOpen(template, PaneSide.Left, unbound.AsReadOnly());

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource));
    }

    /// <summary>
    /// Proves every Presentation-owned palette value rejects each absent part, so no key hint,
    /// row, or view state can reach the shell without its candidate, resource key, or scope.
    /// </summary>
    [TestMethod]
    public async Task ConstructPaletteProjectionWhenAnyPartIsNullThrowsAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);
        IReadOnlyList<CommandPaletteRow> rows = CommandPalettePresenter.Present(open, CommandPaletteTestContext.Localize).Rows;

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandPaletteKeyHint(null!, "CommandPaletteHintExecute"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandPaletteKeyHint("KeyLabelEnter", null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new CommandPaletteKeyHint(" ", "CommandPaletteHintExecute"));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new CommandPaletteKeyHint("KeyLabelEnter", " "));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandPaletteViewState(null!, rows));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandPaletteViewState(open, null!));
    }

    private static IReadOnlyList<(UserIntent Intent, string ResourceKey, string ShortcutKey)>
        ExpectedCatalogRows()
    {
        return
        [
            (UserIntent.OpenFocused, "IntentLabelOpenFocused", "KeyLabelL"),
            (UserIntent.NavigateParent, "IntentLabelNavigateParent", "KeyLabelH"),
            (UserIntent.NavigateBack, "IntentLabelNavigateBack", "KeyLabelAltLeft"),
            (UserIntent.NavigateForward, "IntentLabelNavigateForward", "KeyLabelAltRight"),
            (UserIntent.Refresh, "IntentLabelRefresh", "KeyLabelCtrlR"),
            (UserIntent.FocusAddress, "IntentLabelFocusAddress", "KeyLabelCtrlL"),
            (UserIntent.Rename, "IntentLabelRename", "KeyLabelF2"),
            (UserIntent.Copy, "IntentLabelCopy", "KeyLabelF5"),
            (UserIntent.Move, "IntentLabelMove", "KeyLabelF6"),
            (UserIntent.CreateDirectory, "IntentLabelCreateDirectory", "KeyLabelF7"),
            (UserIntent.Delete, "IntentLabelDelete", "KeyLabelF8"),
            (UserIntent.ToggleSelection, "IntentLabelToggleSelection", "KeyLabelSpace"),
            (UserIntent.ToggleHiddenItems, "IntentLabelToggleHiddenItems", "KeyLabelCtrlH"),
            (UserIntent.ActivateOtherPane, "IntentLabelActivateOtherPane", "KeyLabelTab"),
            (UserIntent.OpenSettings, "IntentLabelOpenSettings", "KeyLabelCtrlComma"),
            (UserIntent.SortByName, "IntentLabelSortByName", "KeyLabelCtrlF3"),
            (UserIntent.SortByExtension, "IntentLabelSortByExtension", "KeyLabelCtrlF4"),
            (UserIntent.OpenLocations, "IntentLabelOpenLocations", "KeyLabelCtrlG"),
            (UserIntent.SortBySize, "IntentLabelSortBySize", "KeyLabelCtrlF5"),
            (UserIntent.SortByModified, "IntentLabelSortByModified", "KeyLabelCtrlF6"),
            (UserIntent.OpenBookmarks, "IntentLabelOpenBookmarks", "KeyLabelCtrlB"),
            (UserIntent.OpenWindowAdjustment, "IntentLabelOpenWindowAdjustment", "KeyLabelCtrlW"),
        ];
    }
}
