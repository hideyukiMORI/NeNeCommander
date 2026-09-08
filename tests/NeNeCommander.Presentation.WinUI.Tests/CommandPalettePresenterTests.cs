using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
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
    /// <summary>Proves displayed palette hints project the canonical palette binding order.</summary>
    [TestMethod]
    public void KeyHintsComeFromCanonicalPaletteBindings()
    {
        IReadOnlyList<CommandPaletteKeyHint> hints = CommandPaletteKeyHintPresenter.Present();

        Assert.HasCount(5, hints);
        Assert.AreEqual("KeyLabelUp", hints[0].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintPrevious", hints[0].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelDown", hints[1].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintNext", hints[1].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelEnter", hints[2].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintExecute", hints[2].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelEscape", hints[3].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintCancel", hints[3].IntentLabelResourceKey);
        Assert.AreEqual("KeyLabelTab", hints[4].KeyLabelResourceKey);
        Assert.AreEqual("CommandPaletteHintFocus", hints[4].IntentLabelResourceKey);
    }

    /// <summary>Proves every searchable command and shared hint intent has one stable localized label.</summary>
    [TestMethod]
    public void CommandLabelsCoverTheCompleteClosedCorrespondence()
    {
        IReadOnlyList<(UserIntent Intent, string ResourceKey)> expected = ExpectedLabels();

        foreach ((UserIntent intent, string resourceKey) in expected)
        {
            CommandLabel label = CommandLabelCatalog.LabelFor(intent);

            Assert.AreSame(intent, label.Intent);
            Assert.AreEqual(resourceKey, label.ResourceKey);
        }
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CommandLabelCatalog.LabelFor(UserIntent.MoveNext));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CommandLabelCatalog.LabelFor(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandLabel(null!, "Resource"));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new CommandLabel(UserIntent.Refresh, string.Empty));
    }

    /// <summary>Proves every catalog row preserves its candidate, label, shortcut, scope, and UIA identity.</summary>
    [TestMethod]
    public async Task PresentProjectsTheCompleteCatalogContractAsync()
    {
        CommandPaletteOpen open = await OpenPaletteAsync(null);

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, ProjectResource);

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
            "<CommandPaletteTargetLeft>|<CommandPaletteOppositeRight>",
            available.Detail);
        Assert.AreEqual(
            "<IntentLabelOpenFocused>;<KeyLabelL>;<CommandPaletteTargetLeft>;" +
            "<CommandPaletteOppositeRight>",
            available.AutomationName);
    }

    /// <summary>Proves all closed unavailable reasons project exact right-target detail and UIA text.</summary>
    [TestMethod]
    public async Task PresentProjectsEveryUnavailableReasonWithoutChangingCandidateIdentityAsync()
    {
        CommandPaletteOpen template = await OpenPaletteAsync(null);
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
            CreateCandidate(item.Intent, CreateUnavailable(item.Reason)))];
        CommandPaletteOpen open = CreateOpen(template, PaneSide.Right, candidates.AsReadOnly());

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, ProjectResource);

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

    /// <summary>Proves empty, title, shortcut, and zero-result queries preserve the specified behavior.</summary>
    [TestMethod]
    public async Task PresentWhenQueryChangesFiltersLocalizedRowsAndSelectsFirstAsync()
    {
        CommandPaletteOpen open = await OpenPaletteAsync(null);

        CommandPaletteViewState view = CommandPalettePresenter.Present(open, Localize);

        Assert.HasCount(15, view.Rows);
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
        Assert.HasCount(1, view.Rows);
        Assert.AreSame(UserIntent.Copy, view.SelectedRow!.Intent);
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

    /// <summary>Proves zero-, one-, and many-row projections preserve bounded selection movement.</summary>
    [TestMethod]
    public async Task SelectionBoundariesRemainClosedForZeroOneAndManyRowsAsync()
    {
        CommandPaletteOpen open = await OpenPaletteAsync(null);
        CommandPaletteViewState many = CommandPalettePresenter.Present(open, ProjectResource);
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
            await OpenPaletteAsync(null),
            Localize);
        CommandPaletteViewState stale = CommandPalettePresenter.Present(
            await OpenPaletteAsync(null),
            Localize);

        bool selected = current.Select(current.Rows[1]);
        bool rejected = current.Select(stale.Rows[0]);

        Assert.IsTrue(selected);
        Assert.IsFalse(rejected);
        Assert.AreSame(UserIntent.NavigateParent, current.SelectedRow!.Intent);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => current.Select(null!));
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

    private static string ProjectResource(string key)
    {
        return key switch
        {
            "CommandPaletteAvailableDetailFormat" => "{0}|{1}",
            "CommandPaletteUnavailableDetailFormat" => "{0}|{1}|{2}",
            "CommandPaletteAvailableAutomationNameFormat" => "{0};{1};{2};{3}",
            "CommandPaletteUnavailableAutomationNameFormat" => "{0};{1};{2};{3};{4}",
            _ => $"<{key}>",
        };
    }

    private static IReadOnlyList<(UserIntent Intent, string ResourceKey)> ExpectedLabels()
    {
        return
        [
            (UserIntent.OpenFocused, "IntentLabelOpenFocused"),
            (UserIntent.NavigateParent, "IntentLabelNavigateParent"),
            (UserIntent.NavigateBack, "IntentLabelNavigateBack"),
            (UserIntent.NavigateForward, "IntentLabelNavigateForward"),
            (UserIntent.Refresh, "IntentLabelRefresh"),
            (UserIntent.FocusAddress, "IntentLabelFocusAddress"),
            (UserIntent.Rename, "IntentLabelRename"),
            (UserIntent.Copy, "IntentLabelCopy"),
            (UserIntent.Move, "IntentLabelMove"),
            (UserIntent.CreateDirectory, "IntentLabelCreateDirectory"),
            (UserIntent.Delete, "IntentLabelDelete"),
            (UserIntent.ToggleSelection, "IntentLabelToggleSelection"),
            (UserIntent.ToggleHiddenItems, "IntentLabelToggleHiddenItems"),
            (UserIntent.ActivateOtherPane, "IntentLabelActivateOtherPane"),
            (UserIntent.OpenSettings, "IntentLabelOpenSettings"),
            (UserIntent.OpenCommandPalette, "IntentLabelOpenCommandPalette"),
            (UserIntent.Escape, "IntentLabelEscape"),
            (UserIntent.Confirm, "IntentLabelConfirm"),
        ];
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
        ];
    }

    private static CommandUnavailable CreateUnavailable(CommandUnavailableReason reason)
    {
        return InvokeInternal<CommandUnavailable>(reason);
    }

    private static CommandCandidate CreateCandidate(
        UserIntent intent,
        CommandAvailability availability)
    {
        return InvokeInternal<CommandCandidate>(intent, availability);
    }

    private static CommandPaletteOpen CreateOpen(
        CommandPaletteOpen template,
        PaneSide activeSide,
        IReadOnlyList<CommandCandidate> candidates)
    {
        return InvokeInternal<CommandPaletteOpen>(
            template.Left,
            template.Right,
            activeSide,
            candidates);
    }

    private static T InvokeInternal<T>(params object[] arguments)
    {
        ConstructorInfo constructor = typeof(T).GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic).Single(candidate =>
                candidate.GetParameters().Length == arguments.Length &&
                (arguments.Length != 1 || candidate.GetParameters()[0].ParameterType != typeof(T)));
        return (T)constructor.Invoke(arguments);
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
