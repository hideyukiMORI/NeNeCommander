using System;
using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Holds focus-only declarations consulted exclusively by KeyboardIntentMapper.</summary>
internal static class KeyboardFocusMap
{
    internal static IReadOnlyList<KeyboardFocusBinding> Bindings { get; } = Array.AsReadOnly<KeyboardFocusBinding>(
    [
        new(KeyboardContext.FileList, KeyboardKey.F10, KeyboardFocusAction.CommandEntry),
        new(KeyboardContext.CommandEntry, KeyboardKey.F10, KeyboardFocusAction.ActivePane),
        new(KeyboardContext.CommandEntry, KeyboardKey.Escape, KeyboardFocusAction.ActivePane),
    ]);

    internal static KeyboardFocusBinding? BindingFor(KeyboardInput input)
    {
        return input.Modifier == KeyboardModifier.None
            ? Bindings.FirstOrDefault(binding => binding.Context == input.Context && binding.Key == input.Key)
            : null;
    }

    internal static KeyboardMappingOutcome Map(KeyboardInput input)
    {
        KeyboardFocusBinding? binding = BindingFor(input);
        return binding is null
            ? new KeyboardPassThrough()
            : input.RepeatState == KeyRepeatState.Repeated
                ? new KeyboardConsumed()
                : new MappedKeyboardFocusAction(binding.Action);
    }
}
