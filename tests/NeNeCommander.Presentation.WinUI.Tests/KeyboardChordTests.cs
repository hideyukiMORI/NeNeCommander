using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardChord behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardChordTests
{
    /// <summary>Proves the gg chord includes its exact lifetime boundary.</summary>
    [TestMethod]
    public void MapWhenGgCompletesWithinLifetimeFocusFirstIntent()
    {
        AdjustableClock clock = AdjustableClock.Create();
        KeyboardIntentMapper mapper = new(clock);

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        clock.Advance(TimeSpan.FromMilliseconds(750));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.LowerG), UserIntent.FocusFirst);
    }

    /// <summary>Proves an expired gg suffix begins a fresh chord.</summary>
    [TestMethod]
    public void MapWhenGgExpiresCurrentGStartsNewChord()
    {
        AdjustableClock clock = AdjustableClock.Create();
        KeyboardIntentMapper mapper = new(clock);

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        clock.Advance(TimeSpan.FromMilliseconds(751));

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
    }

    /// <summary>Proves an unrelated suffix is processed normally.</summary>
    [TestMethod]
    public void MapWhenUnrelatedKeyFollowsGCancelsChordAndProcessesKey()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.J), UserIntent.MoveNext);
    }

    /// <summary>
    /// Proves Escape after a pending g still emits the one Escape intent and cancels the chord, so a
    /// loading panes read is abandoned and the chord is gone, in the keyboard models order (ADR-0058).
    /// </summary>
    [TestMethod]
    public void MapWhenEscapeFollowsGEmitsEscapeAndCancelsChord()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

        KeyboardMappingOutcome escape = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Escape));
        KeyboardMappingOutcome next = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG));

        Assert.AreSame(UserIntent.Escape, Assert.IsInstanceOfType<MappedKeyboardIntent>(escape).Intent);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(next);
    }

    /// <summary>Proves the raw virtual-key event of a printable key neither completes nor cancels a chord.</summary>
    [TestMethod]
    public void MapWhenUnmappedKeyFollowsGKeepsChordPending()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Other)));

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.LowerG), UserIntent.FocusFirst);
    }

    /// <summary>Proves plain F3 leaves a pending chord alone and Ctrl+F4 cancels it before sorting.</summary>
    [TestMethod]
    public void MapWhenSortKeyFollowsGKeepsOrCancelsChordByDeclaration()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F3)));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.LowerG), UserIntent.FocusFirst);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F4, KeyboardModifier.Control), UserIntent.SortByExtension);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
    }

    /// <summary>
    /// Proves address editing cancels a pending chord even when its own key is passed through, so
    /// a later file-list prefix starts a new chord instead of completing the abandoned one.
    /// </summary>
    [TestMethod]
    public void MapWhenAddressEntryKeyFollowsGCancelsTheChord()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

        KeyboardMappingOutcome typed = mapper.Map(KeyboardMappingTestContext.Input(
            KeyboardKey.Other,
            KeyboardContext.AddressEntry));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(typed);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
    }
}
