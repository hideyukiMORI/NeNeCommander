using System;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents one fully translated framework key event and its explicit context.</summary>
public sealed record KeyboardInput
{
    private KeyboardInput(
        KeyboardKey key,
        KeyboardModifier modifier,
        KeyRepeatState repeatState,
        KeyboardContext context)
    {
        Key = key;
        Modifier = modifier;
        RepeatState = repeatState;
        Context = context;
    }

    /// <summary>Gets the layout-translated key identity.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the explicit modifier state.</summary>
    public KeyboardModifier Modifier { get; }

    /// <summary>Gets the initial or auto-repeat state.</summary>
    public KeyRepeatState RepeatState { get; }

    /// <summary>Gets the focus context that owns the event.</summary>
    public KeyboardContext Context { get; }

    /// <summary>Creates a complete input value from already translated framework data.</summary>
    /// <param name="key">Translated key identity.</param>
    /// <param name="modifier">Translated modifier state.</param>
    /// <param name="repeatState">Translated repeat state.</param>
    /// <param name="context">Explicit focus context.</param>
    /// <returns>A complete immutable input value.</returns>
    public static KeyboardInput Create(
        KeyboardKey key,
        KeyboardModifier modifier,
        KeyRepeatState repeatState,
        KeyboardContext context)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentNullException.ThrowIfNull(repeatState);
        ArgumentNullException.ThrowIfNull(context);
        return new KeyboardInput(key, modifier, repeatState, context);
    }
}
