using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardPalette behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardPaletteTests
{
    /// <summary>Proves a palette Enter repeat cannot cross into any resulting input owner.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenPaletteEnterChangesOwnerConsumesItsRepeatUntilNewInitialEnter()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        AssertPaletteAction(mapper, KeyboardKey.Enter, CommandPaletteKeyAction.Execute);

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.J), UserIntent.MoveNext);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.FileList)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.Modal)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.AddressEntry), UserIntent.Confirm);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardInput.Create(
                KeyboardKey.Enter,
                KeyboardModifier.None,
                KeyRepeatState.Repeated,
                KeyboardContext.FileList),
            UserIntent.OpenFocused);
    }

    /// <summary>Proves the palette context owns selection, execution, cancellation, and focus-loop keys.</summary>
    [TestMethod]
    public void MapWhenContextIsCommandPaletteEmitsPresentationActionsOnly()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        AssertPaletteAction(mapper, KeyboardKey.Up, CommandPaletteKeyAction.MovePrevious);
        AssertPaletteAction(mapper, KeyboardKey.Down, CommandPaletteKeyAction.MoveNext);
        AssertPaletteAction(mapper, KeyboardKey.Enter, CommandPaletteKeyAction.Execute);
        AssertPaletteAction(mapper, KeyboardKey.Escape, CommandPaletteKeyAction.Cancel);
        AssertPaletteAction(mapper, KeyboardKey.Tab, CommandPaletteKeyAction.MoveFocus);
        MappedCommandPaletteAction controlTab = Assert.IsInstanceOfType<MappedCommandPaletteAction>(
            mapper.Map(KeyboardMappingTestContext.Input(
                KeyboardKey.Tab,
                KeyboardModifier.Control,
                KeyboardContext.CommandPalette)));
        MappedCommandPaletteAction altTab = Assert.IsInstanceOfType<MappedCommandPaletteAction>(
            mapper.Map(KeyboardMappingTestContext.Input(
                KeyboardKey.Tab,
                KeyboardModifier.Alt,
                KeyboardContext.CommandPalette)));
        Assert.AreSame(CommandPaletteKeyAction.MoveFocus, controlTab.Action);
        Assert.AreSame(CommandPaletteKeyAction.MoveFocus, altTab.Action);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J, KeyboardContext.CommandPalette)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.CommandPalette)));
        AssertPaletteAction(mapper, KeyboardKey.Down, CommandPaletteKeyAction.MoveNext, KeyRepeatState.Repeated);
        Assert.HasCount(5, KeyboardIntentMapper.CommandPaletteBindings);
    }

    /// <summary>
    /// Proves a held Enter that the palette never started is consumed rather than executed, so a
    /// repeat produced by an earlier owner cannot run the selected command.
    /// </summary>
    [TestMethod]
    public void MapWhenPaletteReceivesAnUnstartedEnterRepeatConsumesItWithoutExecuting()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingOutcome repeated = mapper.Map(KeyboardMappingTestContext.Repeated(
            KeyboardKey.Enter,
            KeyboardContext.CommandPalette));

        _ = Assert.IsInstanceOfType<KeyboardConsumed>(repeated);
        AssertPaletteAction(mapper, KeyboardKey.Enter, CommandPaletteKeyAction.Execute);
    }

    private static void AssertPaletteAction(
        KeyboardIntentMapper mapper,
        KeyboardKey key,
        CommandPaletteKeyAction expected)
    {
        AssertPaletteAction(mapper, key, expected, KeyRepeatState.Initial);
    }

    private static void AssertPaletteAction(
        KeyboardIntentMapper mapper,
        KeyboardKey key,
        CommandPaletteKeyAction expected,
        KeyRepeatState repeatState)
    {
        MappedCommandPaletteAction mapped = Assert.IsInstanceOfType<MappedCommandPaletteAction>(
            mapper.Map(KeyboardInput.Create(
                key,
                KeyboardModifier.None,
                repeatState,
                KeyboardContext.CommandPalette)));
        Assert.AreEqual(expected, mapped.Action);
    }
}
