using System;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Declares one key owned by the bookmark manager (ADR-0080): the context that owns it, the
/// layout-translated key, modifier and the single action it emits.
/// </summary>
public sealed record BookmarkManagerKeyBinding
{
    internal BookmarkManagerKeyBinding(
        KeyboardContext context,
        KeyboardKey key,
        KeyboardModifier modifier,
        BookmarkManagerKeyAction action)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentNullException.ThrowIfNull(action);
        Context = context;
        Key = key;
        Modifier = modifier;
        Action = action;
    }

    /// <summary>Gets the bookmark-manager context that owns the key.</summary>
    public KeyboardContext Context { get; }

    /// <summary>Gets the owned key.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the modifier required by this declaration.</summary>
    public KeyboardModifier Modifier { get; }

    /// <summary>Gets the Presentation action emitted for the key.</summary>
    public BookmarkManagerKeyAction Action { get; }

    /// <summary>Gets the localized key-cap resource of the key.</summary>
    public string KeyLabelResourceKey => KeyBinding.LabelFor(Key, Modifier);
}
