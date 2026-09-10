using System;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Declares one key owned by the command-palette keyboard context.</summary>
public sealed record CommandPaletteKeyBinding
{
    internal CommandPaletteKeyBinding(KeyboardKey key, CommandPaletteKeyAction action)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
        Action = action;
    }

    /// <summary>Gets the owned key.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the Presentation action emitted for the key.</summary>
    public CommandPaletteKeyAction Action { get; }
}
