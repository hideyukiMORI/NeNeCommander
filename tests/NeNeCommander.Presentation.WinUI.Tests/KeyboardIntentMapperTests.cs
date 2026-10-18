using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardIntentMapper behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardIntentMapperTests
{
    /// <summary>Proves every unmodified file-list binding.</summary>
    [TestMethod]
    public void MapWhenSingleFileListKeysAreUsedEmitsCanonicalIntents()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.J), UserIntent.MoveNext);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Down), UserIntent.MoveNext);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.K), UserIntent.MovePrevious);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Up), UserIntent.MovePrevious);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.H), UserIntent.NavigateParent);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Backspace), UserIntent.NavigateParent);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.L), UserIntent.OpenFocused);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Enter), UserIntent.OpenFocused);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.UpperG), UserIntent.FocusLast);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.PageDown), UserIntent.MoveHalfPageDown);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.PageUp), UserIntent.MoveHalfPageUp);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Tab), UserIntent.ActivateOtherPane);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Space), UserIntent.ToggleSelection);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Escape), UserIntent.Escape);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F2), UserIntent.Rename);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F5), UserIntent.Copy);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F6), UserIntent.Move);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F7), UserIntent.CreateDirectory);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F8), UserIntent.Delete);
    }

    /// <summary>Proves every modified Windows-compatible alias.</summary>
    [TestMethod]
    public void MapWhenModifiedAliasesAreUsedEmitsCanonicalIntents()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Up, KeyboardModifier.Alt), UserIntent.NavigateParent);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Left, KeyboardModifier.Alt), UserIntent.NavigateBack);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Right, KeyboardModifier.Alt), UserIntent.NavigateForward);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.D, KeyboardModifier.Control), UserIntent.MoveHalfPageDown);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.U, KeyboardModifier.Control), UserIntent.MoveHalfPageUp);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.L, KeyboardModifier.Control), UserIntent.FocusAddress);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.R, KeyboardModifier.Control), UserIntent.Refresh);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.P, KeyboardModifier.Control), UserIntent.OpenCommandPalette);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.H, KeyboardModifier.Control), UserIntent.ToggleHiddenItems);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Comma, KeyboardModifier.Control), UserIntent.OpenSettings);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.B, KeyboardModifier.Control), UserIntent.OpenBookmarks);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.One, KeyboardModifier.Control), UserIntent.BookmarkSlotOne);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Two, KeyboardModifier.Control), UserIntent.BookmarkSlotTwo);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Three, KeyboardModifier.Control), UserIntent.BookmarkSlotThree);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Four, KeyboardModifier.Control), UserIntent.BookmarkSlotFour);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Five, KeyboardModifier.Control), UserIntent.BookmarkSlotFive);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Six, KeyboardModifier.Control), UserIntent.BookmarkSlotSix);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Seven, KeyboardModifier.Control), UserIntent.BookmarkSlotSeven);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Eight, KeyboardModifier.Control), UserIntent.BookmarkSlotEight);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Nine, KeyboardModifier.Control), UserIntent.BookmarkSlotNine);
    }

    /// <summary>Proves bookmark and window entry chords retain their existing pane-surface bindings.</summary>
    [TestMethod]
    public void MapWhenScopeEntryChordsArriveUsesCanonicalPaneBindings()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext[] contexts = [KeyboardContext.FileList, KeyboardContext.NavigationSurface];
        foreach (KeyboardContext context in contexts)
        {
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.B, KeyboardModifier.Control, context), UserIntent.OpenBookmarks);
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.W, KeyboardModifier.Control, context), UserIntent.OpenWindowAdjustment);
        }
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.B, KeyboardModifier.Control, KeyboardContext.CommandPalette)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.W, KeyboardModifier.Control, KeyboardContext.CommandPalette)));
    }

    /// <summary>Proves repeated destructive keys do not emit commands.</summary>
    [TestMethod]
    public void MapWhenDestructiveKeyRepeatsPassesThroughWithoutIntent()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardKey[] keys = [KeyboardKey.F2, KeyboardKey.F5, KeyboardKey.F6, KeyboardKey.F7, KeyboardKey.F8];

        IEnumerable<KeyboardMappingOutcome> outcomes = keys.Select(key => mapper.Map(
            KeyboardInput.Create(
                key,
                KeyboardModifier.None,
                KeyRepeatState.Repeated,
                KeyboardContext.FileList)));
        foreach (KeyboardMappingOutcome outcome in outcomes)
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(outcome);
        }
    }

    /// <summary>Proves repeated movement remains responsive.</summary>
    [TestMethod]
    public void MapWhenMovementKeyRepeatsEmitsMovementIntent()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardInput input = KeyboardInput.Create(
            KeyboardKey.J,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.FileList);

        KeyboardMappingTestContext.AssertMaps(mapper, input, UserIntent.MoveNext);
    }

    /// <summary>Proves F5 meaning is selected by explicit context.</summary>
    [TestMethod]
    public void MapWhenNavigationSurfaceReceivesF5RefreshesWithoutCopying()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F5, KeyboardContext.NavigationSurface), UserIntent.Refresh);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Left, KeyboardModifier.Alt, KeyboardContext.NavigationSurface),
            UserIntent.NavigateBack);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Right, KeyboardModifier.Alt, KeyboardContext.NavigationSurface),
            UserIntent.NavigateForward);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Comma, KeyboardModifier.Control, KeyboardContext.NavigationSurface),
            UserIntent.OpenSettings);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.P, KeyboardModifier.Control, KeyboardContext.NavigationSurface),
            UserIntent.OpenCommandPalette);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.B, KeyboardModifier.Control, KeyboardContext.NavigationSurface),
            UserIntent.OpenBookmarks);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.Nine, KeyboardModifier.Control, KeyboardContext.NavigationSurface),
            UserIntent.BookmarkSlotNine);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J, KeyboardContext.NavigationSurface)));
    }

    /// <summary>Proves unmapped keys and modifier combinations pass through.</summary>
    [TestMethod]
    public void MapWhenKeyOrModifierIsUnmappedPassesThrough()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Other)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J, KeyboardModifier.Other)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Comma)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Left)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Right)));
    }

    /// <summary>
    /// Proves the mapper and its palette values reject every absent part, so no event without a
    /// clock, input, key, or intent can reach the canonical key map.
    /// </summary>
    [TestMethod]
    public void ConstructKeyboardMappingWhenAnyPartIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new KeyboardIntentMapper(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => KeyboardMappingTestContext.CreateMapper().Map(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new CommandPaletteKeyBinding(null!, CommandPaletteKeyAction.Execute));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new MappedKeyboardIntent(null!));
    }
}
