using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the CommandLabelCatalog contract.</summary>
[TestClass]
public sealed class CommandLabelCatalogTests
{
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
            CommandLabelCatalog.LabelFor(UserIntent.FocusFirst));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CommandLabelCatalog.LabelFor(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandLabel(null!, "Resource"));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new CommandLabel(UserIntent.Refresh, string.Empty));
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
            (UserIntent.SortByName, "IntentLabelSortByName"),
            (UserIntent.SortByExtension, "IntentLabelSortByExtension"),
            (UserIntent.OpenLocations, "IntentLabelOpenLocations"),
            (UserIntent.SortBySize, "IntentLabelSortBySize"),
            (UserIntent.SortByModified, "IntentLabelSortByModified"),
            (UserIntent.MoveNext, "IntentLabelMoveNext"),
            (UserIntent.MovePrevious, "IntentLabelMovePrevious"),
            (UserIntent.OpenCommandPalette, "IntentLabelOpenCommandPalette"),
            (UserIntent.OpenBookmarks, "IntentLabelOpenBookmarks"),
            (UserIntent.OpenWindowAdjustment, "IntentLabelOpenWindowAdjustment"),
            (UserIntent.Escape, "IntentLabelEscape"),
            (UserIntent.Confirm, "IntentLabelConfirm"),
        ];
    }
}
