using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Help;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves transfer uses produced letters and guarded control-arrow aliases only in window mode.</summary>
[TestClass]
public sealed class WindowTransferKeyboardTests
{
    private static readonly string[] PreviousCaps = ["KeyLabelP", "KeyLabelCtrlLeft"];
    private static readonly string[] NextCaps = ["KeyLabelN", "KeyLabelCtrlRight"];

    /// <summary>Produced letters request their exact transfer once; held or modified letters are consumed.</summary>
    [TestMethod]
    [DataRow('p')]
    [DataRow('n')]
    public void MapWhenTransferLetterArrivesAdmitsOnlyInitialUnmodifiedCharacter(char character)
    {
        KeyboardIntentMapper mapper = new(AdjustableClock.Create());
        WindowAdjustmentKeyAction expected = character == 'p' ? WindowAdjustmentKeyAction.PreviousDisplay : WindowAdjustmentKeyAction.NextDisplay;
        KeyboardInput input = KeyboardInputTranslator.TranslateCharacterData(character, KeyRepeatState.Initial,
            KeyboardContext.WindowAdjustment, KeyboardModifier.None);

        Assert.AreSame(expected, Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(input)).Action);
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInputTranslator.TranslateCharacterData(
            character, KeyRepeatState.Repeated, KeyboardContext.WindowAdjustment, KeyboardModifier.None)));
        foreach (KeyboardModifier modifier in new[] { KeyboardModifier.Control, KeyboardModifier.Alt, KeyboardModifier.Other })
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInputTranslator.TranslateCharacterData(
                character, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment, modifier)));
        }
    }

    /// <summary>Only control-arrow key data requests transfer; ordinary arrows retain repeatable movement.</summary>
    [TestMethod]
    [DataRow((int)VirtualKey.Left)]
    [DataRow((int)VirtualKey.Right)]
    public void MapWhenArrowArrivesUsesModifierToSeparateTransferFromMovement(int virtualKey)
    {
        KeyboardIntentMapper mapper = new(AdjustableClock.Create());
        WindowAdjustmentKeyAction transfer = virtualKey == (int)VirtualKey.Left ? WindowAdjustmentKeyAction.PreviousDisplay : WindowAdjustmentKeyAction.NextDisplay;
        WindowAdjustmentKeyAction move = virtualKey == (int)VirtualKey.Left ? WindowAdjustmentKeyAction.MoveLeft : WindowAdjustmentKeyAction.MoveRight;
        Assert.AreSame(transfer, Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(
            KeyboardInputTranslator.TranslateKeyData(virtualKey, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment, KeyboardModifier.Control))).Action);
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(
            KeyboardInputTranslator.TranslateKeyData(virtualKey, KeyRepeatState.Repeated, KeyboardContext.WindowAdjustment, KeyboardModifier.Control)));
        _ = Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(
            KeyboardInputTranslator.TranslateKeyData(virtualKey, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment, KeyboardModifier.None)));
        Assert.AreSame(move, Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(
            KeyboardInputTranslator.TranslateKeyData(virtualKey, KeyRepeatState.Repeated, KeyboardContext.WindowAdjustment, KeyboardModifier.None))).Action);
    }

    /// <summary>Raw letters, uppercase, dead-key markers and Japanese characters are never interpreted as transfer.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenRawOrComposedInputArrivesPassesThroughWithoutTransfer()
    {
        KeyboardIntentMapper mapper = new(AdjustableClock.Create());
        foreach (int virtualKey in new[] { (int)VirtualKey.N, (int)VirtualKey.P, 229, 0 })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateKeyData(
                virtualKey, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment, KeyboardModifier.None)));
        }
        foreach (char character in new[] { 'N', 'P', 'あ', '漢', 'é' })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateCharacterData(
                character, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment, KeyboardModifier.None)));
        }
    }

    /// <summary>Transfer-only letters neither cancel a file-list gg prefix nor acquire editor or palette input.</summary>
    [TestMethod]
    [DataRow('p')]
    [DataRow('n')]
    public void MapWhenTransferLetterArrivesOutsideWindowModePreservesOtherOwnership(char character)
    {
        KeyboardIntentMapper mapper = new(AdjustableClock.Create());
        KeyboardInput prefix = KeyboardInput.Create(KeyboardKey.LowerG, KeyboardModifier.None, KeyRepeatState.Initial, KeyboardContext.FileList);
        _ = Assert.IsInstanceOfType<KeyboardAwaitingChord>(mapper.Map(prefix));
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateCharacterData(
            character, KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None)));
        Assert.AreSame(UserIntent.FocusFirst, Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(prefix)).Intent);
        foreach (KeyboardContext context in new[] { KeyboardContext.TextEntry, KeyboardContext.AddressEntry, KeyboardContext.CommandPalette, KeyboardContext.Modal })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInputTranslator.TranslateCharacterData(
                character, KeyRepeatState.Initial, context, KeyboardModifier.None)));
        }
    }

    /// <summary>A held transfer press remains consumed after input-owner changes until its next initial event.</summary>
    [TestMethod]
    public void MapWhenTransferPressOutlivesModeKeepsTheExistingRepeatGuard()
    {
        foreach (WindowAdjustmentKeyBinding binding in KeyboardIntentMapper.WindowAdjustmentBindings.Where(
            candidate => candidate.Action.WindowAction is { ChangesDisplay: true }))
        {
            KeyboardIntentMapper mapper = new(AdjustableClock.Create());
            _ = Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(KeyboardInput.Create(
                binding.Key, binding.Modifier, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment)));
            foreach (KeyboardContext context in new[] { KeyboardContext.FileList, KeyboardContext.TextEntry, KeyboardContext.CommandPalette, KeyboardContext.WindowAdjustment })
            {
                _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(KeyboardInput.Create(
                    binding.Key, binding.Modifier, KeyRepeatState.Repeated, context)));
            }
            _ = Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(KeyboardInput.Create(
                binding.Key, binding.Modifier, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment)));
        }
    }
    /// <summary>The generated help contains both letter and control-arrow caps in each single action row.</summary>
    [TestMethod]
    public void PresentHelpWhenTransferIsDeclaredContainsBothAliasesOnce()
    {
        KeyboardHelpGroup group = KeyboardHelpPresenter.Present().SecondaryGroups.Single(
            candidate => candidate.TitleResourceKey == "KeyboardHelpGroupWindowAdjustment");
        KeyboardHelpRow previous = group.Rows.Single(row => row.ActionLabelResourceKey == "WindowAdjustmentHintPreviousDisplay");
        KeyboardHelpRow next = group.Rows.Single(row => row.ActionLabelResourceKey == "WindowAdjustmentHintNextDisplay");
        CollectionAssert.AreEqual(PreviousCaps, previous.KeyLabelResourceKeys.ToArray());
        CollectionAssert.AreEqual(NextCaps, next.KeyLabelResourceKeys.ToArray());
        Assert.AreEqual("KeyLabelP", KeyboardKey.P.LabelResourceKey);
        Assert.AreEqual("KeyLabelN", KeyboardKey.N.LabelResourceKey);
    }
}
