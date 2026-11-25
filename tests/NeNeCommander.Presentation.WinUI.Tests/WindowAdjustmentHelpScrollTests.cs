using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves mode-help paging uses the canonical map without acquiring geometry or editor input.</summary>
[TestClass]
public sealed class WindowAdjustmentHelpScrollTests
{
    /// <summary>Both initial and repeated page keys reach only the view-owned paging action.</summary>
    [TestMethod]
    [DataRow((int)VirtualKey.PageUp)]
    [DataRow((int)VirtualKey.PageDown)]
    public void MapWhenHelpPageKeyArrivesRepeatsWithoutAnApplicationWindowAction(int virtualKey)
    {
        KeyboardIntentMapper mapper = new(AdjustableClock.Create());
        WindowAdjustmentKeyAction expected = virtualKey == (int)VirtualKey.PageUp
            ? WindowAdjustmentKeyAction.PreviousHelpPage
            : WindowAdjustmentKeyAction.NextHelpPage;

        foreach (KeyRepeatState repeat in new[] { KeyRepeatState.Initial, KeyRepeatState.Repeated })
        {
            KeyboardInput input = KeyboardInputTranslator.TranslateKeyData(
                virtualKey, repeat, KeyboardContext.WindowAdjustment, KeyboardModifier.None);
            WindowAdjustmentKeyAction actual = Assert.IsInstanceOfType<MappedWindowAdjustmentAction>(mapper.Map(input)).Action;
            Assert.AreSame(expected, actual);
            Assert.IsNull(actual.WindowAction);
        }
    }

    /// <summary>Page keys retain ordinary pane/editor ownership outside the mode and reject mode modifiers.</summary>
    [TestMethod]
    [DataRow((int)VirtualKey.PageUp)]
    [DataRow((int)VirtualKey.PageDown)]
    public void MapWhenHelpPageKeyChangesContextPreservesTheDeclaredOwner(int virtualKey)
    {
        KeyboardIntentMapper mapper = new(AdjustableClock.Create());
        UserIntent paneIntent = virtualKey == (int)VirtualKey.PageUp ? UserIntent.MoveHalfPageUp : UserIntent.MoveHalfPageDown;
        KeyboardInput pane = KeyboardInputTranslator.TranslateKeyData(
            virtualKey, KeyRepeatState.Initial, KeyboardContext.FileList, KeyboardModifier.None);
        KeyboardInput editor = KeyboardInputTranslator.TranslateKeyData(
            virtualKey, KeyRepeatState.Initial, KeyboardContext.TextEntry, KeyboardModifier.None);
        KeyboardInput modified = KeyboardInputTranslator.TranslateKeyData(
            virtualKey, KeyRepeatState.Initial, KeyboardContext.WindowAdjustment, KeyboardModifier.Control);

        Assert.AreSame(paneIntent, Assert.IsInstanceOfType<MappedKeyboardIntent>(mapper.Map(pane)).Intent);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(editor));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(mapper.Map(modified));
    }
}
