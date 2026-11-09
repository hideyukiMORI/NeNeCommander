using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Help;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Windowing;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>
/// Proves the keyboard help (ADR-0076): <c>F1</c> opens it only from the file list and navigation
/// surface, the help context owns its keys, and the projection shows exactly the canonical key map.
/// </summary>
[TestClass]
public sealed class KeyboardHelpTests
{
    private static readonly string[] PrimaryTitles = ["KeyboardHelpGroupFileList"];
    private static readonly string[] SecondaryTitles =
    [
        "KeyboardHelpGroupWindowAdjustment",
        "KeyboardHelpGroupLocations",
        "KeyboardHelpGroupSettings",
        "KeyboardHelpGroupCommandPalette",
        "KeyboardHelpGroupHelp",
    ];
    private static readonly string[] ChordCaps = ["KeyLabelChordGG"];
    private static readonly string[] SlotCaps = ["KeyLabelCtrl1To9"];
    private static readonly string[] MoveNextCaps = ["KeyLabelJ", "KeyLabelDown"];
    private static readonly string[] HelpLabels =
    [
        "KeyboardHelpScrollDown",
        "KeyboardHelpScrollUp",
        "KeyboardHelpPageDown",
        "KeyboardHelpPageUp",
        "KeyboardHelpClose",
    ];
    private static readonly string[] CloseCaps = ["KeyLabelEscape", "KeyLabelF1"];

    /// <summary>Proves F1 maps to OpenHelp in the admitted contexts and passes through in text contexts.</summary>
    [TestMethod]
    public void MapWhenF1ArrivesOpensHelpOnlyFromAdmittedContexts()
    {
        KeyboardIntentMapper mapper = CreateMapper();

        Assert.AreSame(UserIntent.OpenHelp, MappedIntent(mapper.Map(Input(KeyboardKey.F1, KeyboardContext.FileList))));
        Assert.AreSame(UserIntent.OpenHelp, MappedIntent(mapper.Map(Input(KeyboardKey.F1, KeyboardContext.NavigationSurface))));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.F1, KeyboardContext.TextEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.F1, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.F1, KeyboardContext.Modal)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Repeated(KeyboardKey.F1, KeyboardContext.FileList)));
        Assert.AreEqual("KeyLabelF1", KeyboardKey.F1.LabelResourceKey);
    }

    /// <summary>Proves the help context maps its declared keys and consumes every other identified key.</summary>
    [TestMethod]
    public void MapWhenHelpOwnsInputMapsDeclaredKeysAndConsumesTheRest()
    {
        KeyboardIntentMapper mapper = CreateMapper();
        KeyboardContext help = KeyboardContext.KeyboardHelp;

        Assert.AreSame(UserIntent.MoveNext, MappedIntent(mapper.Map(Input(KeyboardKey.J, help))));
        Assert.AreSame(UserIntent.MovePrevious, MappedIntent(mapper.Map(Input(KeyboardKey.Up, help))));
        Assert.AreSame(UserIntent.MoveHalfPageDown, MappedIntent(mapper.Map(Input(KeyboardKey.D, KeyboardModifier.Control, help))));
        Assert.AreSame(UserIntent.MoveHalfPageUp, MappedIntent(mapper.Map(Input(KeyboardKey.PageUp, help))));
        Assert.AreSame(UserIntent.Escape, MappedIntent(mapper.Map(Input(KeyboardKey.Escape, help))));
        Assert.AreSame(UserIntent.OpenHelp, MappedIntent(mapper.Map(Input(KeyboardKey.F1, help))));
        Assert.AreSame(UserIntent.MoveNext, MappedIntent(mapper.Map(Repeated(KeyboardKey.Down, help))));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Repeated(KeyboardKey.F1, help)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Repeated(KeyboardKey.Escape, help)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Input(KeyboardKey.F8, help)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Input(KeyboardKey.Tab, help)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(Input(KeyboardKey.J, KeyboardModifier.Control, help)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(Input(KeyboardKey.Other, help)));
    }

    /// <summary>Proves the help projects two columns in the approved group order.</summary>
    [TestMethod]
    public void PresentShowsTheFileListThenTheScopedGroupsInOrder()
    {
        KeyboardHelpPresentation help = KeyboardHelpPresenter.Present();

        CollectionAssert.AreEqual(
            PrimaryTitles,
            help.PrimaryGroups.Select(group => group.TitleResourceKey).ToArray());
        CollectionAssert.AreEqual(
            SecondaryTitles,
            help.SecondaryGroups.Select(group => group.TitleResourceKey).ToArray());
    }

    /// <summary>
    /// Proves the file-list group shows every declared binding exactly once and nothing else: each
    /// action's aliases share one row, the <c>gg</c> chord precedes <c>G</c>, and the nine bookmark
    /// slots share one row.
    /// </summary>
    [TestMethod]
    public void PresentWhenFileListIsProjectedShowsEveryDeclaredBindingOnce()
    {
        KeyboardHelpGroup fileList = KeyboardHelpPresenter.Present().PrimaryGroups.Single();
        IReadOnlyList<KeyBinding> bindings = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList);
        List<string> shown = [.. fileList.Rows.SelectMany(row => row.KeyLabelResourceKeys)];

        foreach (KeyBinding binding in bindings.Where(binding => binding.Intent is not BookmarkShortcutSelection))
        {
            KeyboardHelpRow row = fileList.Rows.Single(row => row.KeyLabelResourceKeys.Contains(binding.KeyLabelResourceKey));
            Assert.AreEqual(CommandLabelCatalog.LabelFor(binding.Intent).ResourceKey, row.ActionLabelResourceKey);
        }
        List<string> declared = [.. bindings
            .Where(binding => binding.Intent is not BookmarkShortcutSelection)
            .Select(binding => binding.KeyLabelResourceKey)
            .Append("KeyLabelChordGG")
            .Append("KeyLabelCtrl1To9")];
        CollectionAssert.AreEquivalent(declared, shown);
        int chord = IndexOf(fileList, "IntentLabelFocusFirst");
        Assert.AreEqual(chord + 1, IndexOf(fileList, "IntentLabelFocusLast"));
        CollectionAssert.AreEqual(ChordCaps, fileList.Rows[chord].KeyLabelResourceKeys.ToArray());
        KeyboardHelpRow slots = fileList.Rows.Single(row => row.ActionLabelResourceKey == "IntentLabelBookmarkSlots");
        CollectionAssert.AreEqual(SlotCaps, slots.KeyLabelResourceKeys.ToArray());
        Assert.AreEqual(9, bindings.Count(binding => binding.Intent is BookmarkShortcutSelection));
        CollectionAssert.AreEqual(
            MoveNextCaps,
            fileList.Rows[IndexOf(fileList, "IntentLabelMoveNext")].KeyLabelResourceKeys.ToArray());
    }

    /// <summary>Proves the scoped groups reuse the canonical scope projections and bindings.</summary>
    [TestMethod]
    public void PresentWhenScopesAreProjectedMatchesTheirCanonicalDeclarations()
    {
        IReadOnlyList<KeyboardHelpGroup> groups = KeyboardHelpPresenter.Present().SecondaryGroups;
        IReadOnlyList<WindowAdjustmentKeyHint> window = WindowAdjustmentKeyHintPresenter.Present();
        IReadOnlyList<CommandPaletteKeyHint> palette = CommandPaletteKeyHintPresenter.Present();

        Assert.HasCount(window.Count, groups[0].Rows);
        for (int index = 0; index < window.Count; index++)
        {
            CollectionAssert.AreEqual(window[index].KeyLabelResourceKeys.ToArray(), groups[0].Rows[index].KeyLabelResourceKeys.ToArray());
            Assert.AreEqual(window[index].IntentLabelResourceKey, groups[0].Rows[index].ActionLabelResourceKey);
        }
        CollectionAssert.AreEquivalent(
            KeyboardIntentMapper.BindingsFor(KeyboardContext.Locations).Select(binding => binding.KeyLabelResourceKey).ToArray(),
            groups[1].Rows.SelectMany(row => row.KeyLabelResourceKeys).ToArray());
        CollectionAssert.AreEquivalent(
            KeyboardIntentMapper.BindingsFor(KeyboardContext.Settings).Select(binding => binding.KeyLabelResourceKey).ToArray(),
            groups[2].Rows.SelectMany(row => row.KeyLabelResourceKeys).ToArray());
        Assert.HasCount(palette.Count, groups[3].Rows);
        CollectionAssert.AreEqual(
            palette.Select(hint => hint.KeyLabelResourceKey).ToArray(),
            groups[3].Rows.SelectMany(row => row.KeyLabelResourceKeys).ToArray());
        CollectionAssert.AreEqual(
            HelpLabels,
            groups[4].Rows.Select(row => row.ActionLabelResourceKey).ToArray());
        CollectionAssert.AreEquivalent(
            KeyboardIntentMapper.BindingsFor(KeyboardContext.KeyboardHelp).Select(binding => binding.KeyLabelResourceKey).ToArray(),
            groups[4].Rows.SelectMany(row => row.KeyLabelResourceKeys).ToArray());
        CollectionAssert.AreEqual(
            CloseCaps,
            groups[4].Rows[4].KeyLabelResourceKeys.ToArray());
    }

    private static int IndexOf(KeyboardHelpGroup group, string actionLabel)
    {
        for (int index = 0; index < group.Rows.Count; index++)
        {
            if (group.Rows[index].ActionLabelResourceKey == actionLabel)
            {
                return index;
            }
        }
        throw new AssertFailedException("Missing help row " + actionLabel);
    }

    private static UserIntent MappedIntent(KeyboardMappingOutcome outcome)
    {
        return Assert.IsInstanceOfType<MappedKeyboardIntent>(outcome).Intent;
    }

    private static KeyboardIntentMapper CreateMapper()
    {
        return new KeyboardIntentMapper(AdjustableClock.Create());
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardContext context)
    {
        return Input(key, KeyboardModifier.None, context);
    }

    private static KeyboardInput Input(KeyboardKey key, KeyboardModifier modifier, KeyboardContext context)
    {
        return KeyboardInput.Create(key, modifier, KeyRepeatState.Initial, context);
    }

    private static KeyboardInput Repeated(KeyboardKey key, KeyboardContext context)
    {
        return KeyboardInput.Create(key, KeyboardModifier.None, KeyRepeatState.Repeated, context);
    }
}
