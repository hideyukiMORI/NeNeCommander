using Microsoft.UI.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.App.Input;

/// <summary>Reads only the current UI thread's framework modifier facts for a pane gesture.</summary>
internal static class WinUiPanePointerTranslator
{
    internal static PanePointerModifier GetModifier()
    {
        return PanePointerModifier.Translate(
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control),
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift),
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu),
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows) |
                InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows));
    }
}
