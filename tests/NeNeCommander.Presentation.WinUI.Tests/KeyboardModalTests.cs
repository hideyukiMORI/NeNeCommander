using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardModal behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardModalTests
{
    /// <summary>Proves text and modal contexts block underlying commands.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenTextEntryOrModalOwnsInputBlocksUnderlyingCommands()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));

        KeyboardMappingOutcome text = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F8, KeyboardContext.TextEntry));
        KeyboardMappingOutcome modal = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F8, KeyboardContext.Modal));
        KeyboardMappingOutcome textEscape = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalEscape = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.Modal));
        KeyboardMappingOutcome modalEnter = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.Modal));
        KeyboardMappingOutcome textEnter = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalMovement = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J, KeyboardContext.Modal));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(text);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modal);
        Assert.AreSame(UserIntent.Escape, Assert.IsInstanceOfType<MappedKeyboardIntent>(textEscape).Intent);
        Assert.AreSame(UserIntent.Escape, Assert.IsInstanceOfType<MappedKeyboardIntent>(modalEscape).Intent);
        Assert.AreSame(UserIntent.Confirm, Assert.IsInstanceOfType<MappedKeyboardIntent>(modalEnter).Intent);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(textEnter);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modalMovement);
        AssertOwnedNavigationIsolation(mapper);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.LowerG)));
    }

    /// <summary>Proves a held Enter cannot cross into a newly opened confirmation scope.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenModalEnterRepeatsConsumesItUntilTheNextInitialPress()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingOutcome repeated = mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.Modal));
        KeyboardMappingOutcome initial = mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Initial,
            KeyboardContext.Modal));

        _ = Assert.IsInstanceOfType<KeyboardConsumed>(repeated);
        Assert.AreSame(
            UserIntent.Confirm,
            Assert.IsInstanceOfType<MappedKeyboardIntent>(initial).Intent);
    }

    /// <summary>Proves address editing owns only confirm, cancel, and focus-address bindings.</summary>
    [TestMethod]
    public void MapWhenAddressEntryOwnsInputMapsNavigationKeysAndPassesNativeEditingThrough()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.AddressEntry), UserIntent.Confirm);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.AddressEntry), UserIntent.Escape);
        KeyboardMappingTestContext.AssertMaps(
            mapper,
            KeyboardMappingTestContext.Input(KeyboardKey.L, KeyboardModifier.Control, KeyboardContext.AddressEntry),
            UserIntent.FocusAddress);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Other, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.J, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.L, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.L, KeyboardModifier.Other, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Left, KeyboardModifier.Alt, KeyboardContext.AddressEntry)));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Right, KeyboardModifier.Alt, KeyboardContext.AddressEntry)));
    }

    /// <summary>Proves a native-control modal leaves Enter and Space to focus while retaining Escape.</summary>
    [TestMethod]
    public void DeferModalConfirmToNativeControlPreservesFocusedControlKeyboardBehavior()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardMappingOutcome enter = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Enter, KeyboardContext.Modal));
        KeyboardMappingOutcome space = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Space, KeyboardContext.Modal));
        KeyboardMappingOutcome escape = mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.Escape, KeyboardContext.Modal));
        KeyboardMappingOutcome repeatedEnter = mapper.Map(KeyboardInput.Create(
            KeyboardKey.Enter,
            KeyboardModifier.None,
            KeyRepeatState.Repeated,
            KeyboardContext.Modal));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            KeyboardIntentMapper.DeferModalConfirmToNativeControl(enter));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            KeyboardIntentMapper.DeferModalConfirmToNativeControl(space));
        Assert.AreSame(
            UserIntent.Escape,
            Assert.IsInstanceOfType<MappedKeyboardIntent>(
                KeyboardIntentMapper.DeferModalConfirmToNativeControl(escape)).Intent);
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            KeyboardIntentMapper.DeferModalConfirmToNativeControl(repeatedEnter));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            KeyboardIntentMapper.DeferModalConfirmToNativeControl(null!));
    }

    /// <summary>
    /// Proves only a repeated modal Enter is consumed, so a held Enter still reaches a text editor
    /// and a held Escape still cancels the modal that owns it.
    /// </summary>
    [TestMethod]
    public void MapWhenOwnedContextKeyRepeatsOnlyModalEnterIsConsumed()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingOutcome textEnter = mapper.Map(KeyboardMappingTestContext.Repeated(
            KeyboardKey.Enter,
            KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalEscape = mapper.Map(KeyboardMappingTestContext.Repeated(
            KeyboardKey.Escape,
            KeyboardContext.Modal));
        KeyboardMappingOutcome textEscape = mapper.Map(KeyboardMappingTestContext.Repeated(
            KeyboardKey.Escape,
            KeyboardContext.TextEntry));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(textEnter);
        Assert.AreSame(
            UserIntent.Escape,
            Assert.IsInstanceOfType<MappedKeyboardIntent>(modalEscape).Intent);
        Assert.AreSame(
            UserIntent.Escape,
            Assert.IsInstanceOfType<MappedKeyboardIntent>(textEscape).Intent);
    }

    private static void AssertOwnedNavigationIsolation(KeyboardIntentMapper mapper)
    {
        KeyboardMappingOutcome textHiddenToggle = mapper.Map(KeyboardInput.Create(
            KeyboardKey.H,
            KeyboardModifier.Control,
            KeyRepeatState.Initial,
            KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalHiddenToggle = mapper.Map(KeyboardInput.Create(
            KeyboardKey.H,
            KeyboardModifier.Control,
            KeyRepeatState.Initial,
            KeyboardContext.Modal));
        KeyboardMappingOutcome textBack = mapper.Map(KeyboardMappingTestContext.Input(
            KeyboardKey.Left,
            KeyboardModifier.Alt,
            KeyboardContext.TextEntry));
        KeyboardMappingOutcome textForward = mapper.Map(KeyboardMappingTestContext.Input(
            KeyboardKey.Right,
            KeyboardModifier.Alt,
            KeyboardContext.TextEntry));
        KeyboardMappingOutcome modalBack = mapper.Map(KeyboardMappingTestContext.Input(
            KeyboardKey.Left,
            KeyboardModifier.Alt,
            KeyboardContext.Modal));
        KeyboardMappingOutcome modalForward = mapper.Map(KeyboardMappingTestContext.Input(
            KeyboardKey.Right,
            KeyboardModifier.Alt,
            KeyboardContext.Modal));

        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(textHiddenToggle);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modalHiddenToggle);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(textBack);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(textForward);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modalBack);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(modalForward);
    }
}
