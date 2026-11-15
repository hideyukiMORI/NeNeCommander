using System;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Declares one key owned by the bookmark manager (ADR-0079): the context that owns it, the
/// layout-translated key and the single action it emits. Every bookmark-manager key is declared
/// with no modifier; the same key under a modifier is not declared.
/// </summary>
public sealed record BookmarkManagerKeyBinding
{
    internal BookmarkManagerKeyBinding(
        KeyboardContext context,
        KeyboardKey key,
        BookmarkManagerKeyAction action)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(action);
        Context = context;
        Key = key;
        Action = action;
    }

    /// <summary>Gets the bookmark-manager context that owns the key.</summary>
    public KeyboardContext Context { get; }

    /// <summary>Gets the owned key.</summary>
    public KeyboardKey Key { get; }

    /// <summary>Gets the Presentation action emitted for the key.</summary>
    public BookmarkManagerKeyAction Action { get; }

    /// <summary>Gets the localized key-cap resource of the key.</summary>
    public string KeyLabelResourceKey => Key.LabelResourceKey;
}
