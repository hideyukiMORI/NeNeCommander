using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Time;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Owns the sole context-aware mapping from translated keyboard input to application intent, and
/// the single table of declared bindings that mapping and every displayed shortcut hint read
/// (KBD-005). The <c>gg</c> chord is not a single binding; it is the only stateful entry and is
/// resolved before the table is consulted.
/// </summary>
public sealed class KeyboardIntentMapper
{
    private static readonly TimeSpan ChordLifetime = TimeSpan.FromMilliseconds(750);

    /// <summary>
    /// The canonical key map. Every keystroke appears at most once per context (KBD-005). The
    /// modal and text-entry contexts own their declared keys regardless of modifier state so a
    /// stuck modifier cannot bypass a destructive confirmation (KBD-002).
    /// </summary>
    private static readonly IReadOnlyList<KeyBinding> DeclaredBindings =
    [
        new(KeyboardContext.FileList, KeyboardKey.F10, KeyboardModifier.Shift, UserIntent.OpenContextMenu),
        new(KeyboardContext.FileList, KeyboardKey.Apps, KeyboardModifier.None, UserIntent.OpenContextMenu),
        new(KeyboardContext.FileList, KeyboardKey.J, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.FileList, KeyboardKey.Down, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.FileList, KeyboardKey.K, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.FileList, KeyboardKey.Up, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.FileList, KeyboardKey.H, KeyboardModifier.None, UserIntent.NavigateParent),
        new(KeyboardContext.FileList, KeyboardKey.Backspace, KeyboardModifier.None, UserIntent.NavigateParent),
        new(KeyboardContext.FileList, KeyboardKey.L, KeyboardModifier.None, UserIntent.OpenFocused),
        new(KeyboardContext.FileList, KeyboardKey.Enter, KeyboardModifier.None, UserIntent.OpenFocused),
        new(KeyboardContext.FileList, KeyboardKey.UpperG, KeyboardModifier.None, UserIntent.FocusLast),
        new(KeyboardContext.FileList, KeyboardKey.PageDown, KeyboardModifier.None, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.FileList, KeyboardKey.PageUp, KeyboardModifier.None, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.FileList, KeyboardKey.Tab, KeyboardModifier.None, UserIntent.ActivateOtherPane),
        new(KeyboardContext.FileList, KeyboardKey.Space, KeyboardModifier.None, UserIntent.ToggleSelection),
        new(KeyboardContext.FileList, KeyboardKey.H, KeyboardModifier.Control, UserIntent.ToggleHiddenItems),
        new(KeyboardContext.FileList, KeyboardKey.F3, KeyboardModifier.Control, UserIntent.SortByName),
        new(KeyboardContext.FileList, KeyboardKey.F4, KeyboardModifier.Control, UserIntent.SortByExtension),
        new(KeyboardContext.FileList, KeyboardKey.F5, KeyboardModifier.Control, UserIntent.SortBySize),
        new(KeyboardContext.FileList, KeyboardKey.F6, KeyboardModifier.Control, UserIntent.SortByModified),
        new(KeyboardContext.FileList, KeyboardKey.F7, KeyboardModifier.Control, UserIntent.CreateTextFile),
        new(KeyboardContext.FileList, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.FileList, KeyboardKey.F2, KeyboardModifier.None, UserIntent.Rename),
        new(KeyboardContext.FileList, KeyboardKey.F5, KeyboardModifier.None, UserIntent.Copy),
        new(KeyboardContext.FileList, KeyboardKey.F6, KeyboardModifier.None, UserIntent.Move),
        new(KeyboardContext.FileList, KeyboardKey.F7, KeyboardModifier.None, UserIntent.CreateDirectory),
        new(KeyboardContext.FileList, KeyboardKey.F8, KeyboardModifier.None, UserIntent.Delete),
        new(KeyboardContext.FileList, KeyboardKey.Up, KeyboardModifier.Alt, UserIntent.NavigateParent),
        new(KeyboardContext.FileList, KeyboardKey.Left, KeyboardModifier.Alt, UserIntent.NavigateBack),
        new(KeyboardContext.FileList, KeyboardKey.Right, KeyboardModifier.Alt, UserIntent.NavigateForward),
        new(KeyboardContext.FileList, KeyboardKey.D, KeyboardModifier.Control, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.FileList, KeyboardKey.U, KeyboardModifier.Control, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.FileList, KeyboardKey.L, KeyboardModifier.Control, UserIntent.FocusAddress),
        new(KeyboardContext.FileList, KeyboardKey.E, KeyboardModifier.Control, UserIntent.OpenCurrentLocationInOtherPane),
        new(KeyboardContext.FileList, KeyboardKey.R, KeyboardModifier.Control, UserIntent.Refresh),
        new(KeyboardContext.FileList, KeyboardKey.P, KeyboardModifier.Control, UserIntent.OpenCommandPalette),
        new(KeyboardContext.FileList, KeyboardKey.Comma, KeyboardModifier.Control, UserIntent.OpenSettings),
        new(KeyboardContext.FileList, KeyboardKey.B, KeyboardModifier.Control, UserIntent.OpenBookmarks),
        new(KeyboardContext.FileList, KeyboardKey.W, KeyboardModifier.Control, UserIntent.OpenWindowAdjustment),
        new(KeyboardContext.FileList, KeyboardKey.LowerG, KeyboardModifier.Control, UserIntent.OpenLocations),
        new(KeyboardContext.FileList, KeyboardKey.F1, KeyboardModifier.None, UserIntent.OpenHelp),
        new(KeyboardContext.CommandEntry, KeyboardKey.B, KeyboardModifier.Control, UserIntent.OpenBookmarks),
        new(KeyboardContext.CommandEntry, KeyboardKey.Comma, KeyboardModifier.Control, UserIntent.OpenSettings),
        new(KeyboardContext.CommandEntry, KeyboardKey.F1, KeyboardModifier.None, UserIntent.OpenHelp),
        new(KeyboardContext.FileList, KeyboardKey.One, KeyboardModifier.Control, UserIntent.BookmarkSlotOne),
        new(KeyboardContext.FileList, KeyboardKey.Two, KeyboardModifier.Control, UserIntent.BookmarkSlotTwo),
        new(KeyboardContext.FileList, KeyboardKey.Three, KeyboardModifier.Control, UserIntent.BookmarkSlotThree),
        new(KeyboardContext.FileList, KeyboardKey.Four, KeyboardModifier.Control, UserIntent.BookmarkSlotFour),
        new(KeyboardContext.FileList, KeyboardKey.Five, KeyboardModifier.Control, UserIntent.BookmarkSlotFive),
        new(KeyboardContext.FileList, KeyboardKey.Six, KeyboardModifier.Control, UserIntent.BookmarkSlotSix),
        new(KeyboardContext.FileList, KeyboardKey.Seven, KeyboardModifier.Control, UserIntent.BookmarkSlotSeven),
        new(KeyboardContext.FileList, KeyboardKey.Eight, KeyboardModifier.Control, UserIntent.BookmarkSlotEight),
        new(KeyboardContext.FileList, KeyboardKey.Nine, KeyboardModifier.Control, UserIntent.BookmarkSlotNine),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F5, KeyboardModifier.None, UserIntent.Refresh),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Up, KeyboardModifier.Alt, UserIntent.NavigateParent),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Left, KeyboardModifier.Alt, UserIntent.NavigateBack),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Right, KeyboardModifier.Alt, UserIntent.NavigateForward),
        new(KeyboardContext.NavigationSurface, KeyboardKey.D, KeyboardModifier.Control, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.NavigationSurface, KeyboardKey.U, KeyboardModifier.Control, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.NavigationSurface, KeyboardKey.L, KeyboardModifier.Control, UserIntent.FocusAddress),
        new(KeyboardContext.NavigationSurface, KeyboardKey.E, KeyboardModifier.Control, UserIntent.OpenCurrentLocationInOtherPane),
        new(KeyboardContext.NavigationSurface, KeyboardKey.R, KeyboardModifier.Control, UserIntent.Refresh),
        new(KeyboardContext.NavigationSurface, KeyboardKey.P, KeyboardModifier.Control, UserIntent.OpenCommandPalette),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Comma, KeyboardModifier.Control, UserIntent.OpenSettings),
        new(KeyboardContext.NavigationSurface, KeyboardKey.B, KeyboardModifier.Control, UserIntent.OpenBookmarks),
        new(KeyboardContext.NavigationSurface, KeyboardKey.W, KeyboardModifier.Control, UserIntent.OpenWindowAdjustment),
        new(KeyboardContext.NavigationSurface, KeyboardKey.LowerG, KeyboardModifier.Control, UserIntent.OpenLocations),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F1, KeyboardModifier.None, UserIntent.OpenHelp),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F3, KeyboardModifier.Control, UserIntent.SortByName),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F4, KeyboardModifier.Control, UserIntent.SortByExtension),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F5, KeyboardModifier.Control, UserIntent.SortBySize),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F6, KeyboardModifier.Control, UserIntent.SortByModified),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F7, KeyboardModifier.Control, UserIntent.CreateTextFile),
        new(KeyboardContext.NavigationSurface, KeyboardKey.One, KeyboardModifier.Control, UserIntent.BookmarkSlotOne),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Two, KeyboardModifier.Control, UserIntent.BookmarkSlotTwo),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Three, KeyboardModifier.Control, UserIntent.BookmarkSlotThree),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Four, KeyboardModifier.Control, UserIntent.BookmarkSlotFour),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Five, KeyboardModifier.Control, UserIntent.BookmarkSlotFive),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Six, KeyboardModifier.Control, UserIntent.BookmarkSlotSix),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Seven, KeyboardModifier.Control, UserIntent.BookmarkSlotSeven),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Eight, KeyboardModifier.Control, UserIntent.BookmarkSlotEight),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Nine, KeyboardModifier.Control, UserIntent.BookmarkSlotNine),
        new(KeyboardContext.Modal, KeyboardKey.Enter, KeyboardModifier.None, UserIntent.Confirm),
        new(KeyboardContext.Modal, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.TextEntry, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.Locations, KeyboardKey.J, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.Locations, KeyboardKey.Down, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.Locations, KeyboardKey.K, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.Locations, KeyboardKey.Up, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.Locations, KeyboardKey.Enter, KeyboardModifier.None, UserIntent.Confirm),
        new(KeyboardContext.Locations, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.Settings, KeyboardKey.J, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.Settings, KeyboardKey.Down, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.Settings, KeyboardKey.K, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.Settings, KeyboardKey.Up, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.Settings, KeyboardKey.Space, KeyboardModifier.None, UserIntent.Confirm),
        new(KeyboardContext.Settings, KeyboardKey.Enter, KeyboardModifier.None, UserIntent.Confirm),
        new(KeyboardContext.Settings, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.J, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.Down, KeyboardModifier.None, UserIntent.MoveNext),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.K, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.Up, KeyboardModifier.None, UserIntent.MovePrevious),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.PageDown, KeyboardModifier.None, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.D, KeyboardModifier.Control, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.PageUp, KeyboardModifier.None, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.U, KeyboardModifier.Control, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.KeyboardHelp, KeyboardKey.F1, KeyboardModifier.None, UserIntent.OpenHelp),
        new(KeyboardContext.AddressEntry, KeyboardKey.Enter, KeyboardModifier.None, UserIntent.Confirm),
        new(KeyboardContext.AddressEntry, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.AddressEntry, KeyboardKey.L, KeyboardModifier.Control, UserIntent.FocusAddress),
    ];

    private static readonly IReadOnlyList<CommandPaletteKeyBinding> DeclaredPaletteBindings =
        Array.AsReadOnly<CommandPaletteKeyBinding>(
    [
        new(KeyboardKey.Up, CommandPaletteKeyAction.MovePrevious),
        new(KeyboardKey.Down, CommandPaletteKeyAction.MoveNext),
        new(KeyboardKey.Enter, CommandPaletteKeyAction.Execute),
        new(KeyboardKey.Escape, CommandPaletteKeyAction.Cancel),
        new(KeyboardKey.Tab, CommandPaletteKeyAction.MoveFocus),
    ]);

    private readonly IClock _clock;
    private TimeSpan? _pendingChordStartedAt;
    private readonly KeyboardRepeatGuard _repeatGuard = new();

    /// <summary>Initializes the mapper with a monotonic clock used only for chord expiry.</summary>
    /// <param name="clock">Monotonic clock.</param>
    public KeyboardIntentMapper(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>
    /// Returns the bindings the canonical key map declares for one focus context, in declaration
    /// order. The list is the same data <see cref="Map"/> consults, so a hint can never drift from
    /// the behavior it advertises.
    /// </summary>
    /// <param name="context">Focus context whose declarations are requested.</param>
    /// <returns>The declared bindings of that context; empty when the context declares none.</returns>
    public static IReadOnlyList<KeyBinding> BindingsFor(KeyboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        List<KeyBinding> declared = [.. DeclaredBindings.Where(binding => binding.Context == context)];
        return declared.AsReadOnly();
    }

    /// <summary>Gets the keys owned by the Presentation palette state in stable order.</summary>
    public static IReadOnlyList<CommandPaletteKeyBinding> CommandPaletteBindings { get; } =
        DeclaredPaletteBindings;

    /// <summary>Gets the keys owned by the window-adjustment mode in declaration order.</summary>
    public static IReadOnlyList<WindowAdjustmentKeyBinding> WindowAdjustmentBindings { get; } =
        WindowAdjustmentKeyMap.Bindings;

    /// <summary>
    /// Gets the keys owned by the bookmark manager's browse and search-field contexts in declaration
    /// order (ADR-0080). The list is the same data <see cref="Map"/> consults.
    /// </summary>
    public static IReadOnlyList<BookmarkManagerKeyBinding> BookmarkManagerBindings => BookmarkManagerKeyMap.Bindings;

    /// <summary>Gets the canonical focus-only bindings used by mapping and displayed help.</summary>
    public static IReadOnlyList<KeyboardFocusBinding> FocusBindings => KeyboardFocusMap.Bindings;

    /// <summary>Retains native Enter/Escape ownership until the next physical press.</summary>
    internal void ObserveNativeInput(KeyboardInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _repeatGuard.ObserveNative(input);
    }

    internal static KeyboardMappingOutcome DeferModalConfirmToNativeControl(
        KeyboardMappingOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return outcome is MappedKeyboardIntent mapped && mapped.Intent == UserIntent.Confirm
            ? new KeyboardPassThrough()
            : outcome;
    }

    /// <summary>Maps exactly one translated event under its explicit focus context.</summary>
    /// <param name="input">Complete translated keyboard event.</param>
    /// <returns>A mapped intent, pass-through decision, or pending-chord state.</returns>
    public KeyboardMappingOutcome Map(KeyboardInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (_repeatGuard.Consumes(input))
        {
            if (input.Key == KeyboardKey.Enter || IsDeclaredInContext(input))
            {
                _pendingChordStartedAt = null;
            }
            return new KeyboardConsumed();
        }
        KeyboardMappingOutcome outcome = input.Context == KeyboardContext.FileList || input.Context == KeyboardContext.NavigationSurface
            ? MapPaneKey(input)
            : MapContextKey(input);
        _repeatGuard.Observe(input, outcome);
        return outcome;
    }

    private KeyboardMappingOutcome MapContextKey(KeyboardInput input)
    {
        _pendingChordStartedAt = null;
        return input.Context switch
        {
            KeyboardContext context when context == KeyboardContext.ContextMenu => KeyboardRepeatGuard.MapNativeMenu(input),
            KeyboardContext context when context == KeyboardContext.CommandPalette => MapCommandPaletteKey(input),
            KeyboardContext context when context == KeyboardContext.WindowAdjustment => WindowAdjustmentKeyMap.Map(input),
            KeyboardContext context when context == KeyboardContext.Locations => MapLocationsKey(input),
            KeyboardContext context when context == KeyboardContext.KeyboardHelp => MapKeyboardHelpKey(input),
            KeyboardContext context when context == KeyboardContext.Settings => MapSettingsKey(input),
            KeyboardContext context when context == KeyboardContext.CommandEntry => MapCommandEntryKey(input),
            KeyboardContext context when BookmarkManagerKeyMap.Owns(context) => BookmarkManagerKeyMap.Map(input),
            KeyboardContext context when context == KeyboardContext.TextEntry || context == KeyboardContext.Modal =>
                MapOwnedKey(input),
            _ => MapDeclaredKey(input),
        };
    }

    private KeyboardMappingOutcome MapPaneKey(KeyboardInput input)
    {
        if (input.Key == KeyboardKey.Other)
        {
            // A printable key arrives twice: as a raw virtual key and as its produced character.
            // Only the produced character is mapped, so the raw event must not disturb a chord.
            return new KeyboardPassThrough();
        }

        if (TryCompleteChord(input) is MappedKeyboardIntent chordOutcome)
        {
            return chordOutcome;
        }

        if (IsChordPrefix(input))
        {
            _pendingChordStartedAt = _clock.GetMonotonicTime();
            return new KeyboardAwaitingChord();
        }

        return IsRepeatedSingleShotCommand(input)
            ? new KeyboardPassThrough()
            : MapDeclaredKey(input);
    }

    /// <summary>
    /// Resolves one key against a pending chord. A key the current context does not declare leaves
    /// the chord untouched, which is what <c>KEYBOARD_MODEL.md</c> states: only a mapped second key
    /// cancels a pending prefix. Deciding by declaration rather than by <see cref="KeyboardKey.Other"/>
    /// is what lets keys owned by another context, such as the window mode's <c>m</c>, <c>+</c>, and
    /// <c>-</c>, exist without disturbing the file list's <c>gg</c>.
    /// </summary>
    private MappedKeyboardIntent? TryCompleteChord(KeyboardInput input)
    {
        if (_pendingChordStartedAt is not TimeSpan startedAt ||
            !(IsChordPrefix(input) || IsDeclaredInContext(input)))
        {
            return null;
        }

        _pendingChordStartedAt = null;
        TimeSpan elapsed = _clock.GetMonotonicTime() - startedAt;
        return elapsed <= ChordLifetime && IsChordPrefix(input)
            ? new MappedKeyboardIntent(UserIntent.FocusFirst)
            : null;
    }

    private static bool IsDeclaredInContext(KeyboardInput input)
    {
        return KeyboardFocusMap.BindingFor(input) is not null || DeclaredBindings.Any(binding =>
            binding.Context == input.Context &&
            binding.Key == input.Key &&
            binding.Modifier == input.Modifier);
    }

    /// <summary>
    /// Maps a key a modal or text editor owns. Those contexts own their declared keys whatever the
    /// modifier state, so the modifier of the declaration is not compared here (KBD-002).
    /// </summary>
    private static KeyboardMappingOutcome MapOwnedKey(KeyboardInput input)
    {
        if (input.Context == KeyboardContext.Modal &&
            input.Key == KeyboardKey.Enter &&
            input.RepeatState == KeyRepeatState.Repeated)
        {
            return new KeyboardConsumed();
        }
        KeyBinding? binding = DeclaredBindings.FirstOrDefault(binding =>
            binding.Context == input.Context && binding.Key == input.Key);
        return binding is null ? new KeyboardPassThrough() : new MappedKeyboardIntent(binding.Intent);
    }

    private static KeyboardMappingOutcome MapCommandPaletteKey(KeyboardInput input)
    {
        CommandPaletteKeyBinding? binding = DeclaredPaletteBindings.FirstOrDefault(binding =>
            binding.Key == input.Key);
        return binding is null
            ? new KeyboardPassThrough()
            : binding.Action == CommandPaletteKeyAction.Execute &&
                input.RepeatState == KeyRepeatState.Repeated
                    ? new KeyboardConsumed()
                    : new MappedCommandPaletteAction(binding.Action);
    }

    /// <summary>
    /// Maps one key while the Locations picker owns input (KBD-002). A declared key with its declared
    /// modifier yields its intent and a repeated <c>Enter</c> is consumed, so a held key selects at
    /// most once; every other identified key is consumed, so no pane or native control receives it.
    /// The raw virtual-key event that precedes a produced character passes through, because
    /// <c>j</c> and <c>k</c> are produced characters (KBD-003).
    /// </summary>
    private static KeyboardMappingOutcome MapLocationsKey(KeyboardInput input)
    {
        if (input.Key == KeyboardKey.Other)
        {
            return new KeyboardPassThrough();
        }
        KeyboardMappingOutcome declared = input.Key == KeyboardKey.Enter &&
            input.RepeatState == KeyRepeatState.Repeated
                ? new KeyboardConsumed()
                : MapDeclaredKey(input);
        return declared is KeyboardPassThrough ? new KeyboardConsumed() : declared;
    }

    /// <summary>
    /// Maps one key while the keyboard help owns input (ADR-0076). A declared key with its declared
    /// modifier yields its intent; a repeated close key is consumed so a held <c>F1</c> or
    /// <c>Escape</c> cannot close and reopen the help. Every other identified key is consumed, so the
    /// frozen panes receive nothing. The raw virtual-key event that precedes a produced character
    /// passes through, because <c>j</c> and <c>k</c> are produced characters (KBD-003).
    /// </summary>
    private static KeyboardMappingOutcome MapKeyboardHelpKey(KeyboardInput input)
    {
        if (input.Key == KeyboardKey.Other)
        {
            return new KeyboardPassThrough();
        }
        KeyboardMappingOutcome declared = MapDeclaredKey(input);
        return declared is MappedKeyboardIntent mapped &&
            !(input.RepeatState == KeyRepeatState.Repeated &&
                (mapped.Intent == UserIntent.Escape || mapped.Intent == UserIntent.OpenHelp))
                ? declared
                : new KeyboardConsumed();
    }

    /// <summary>
    /// Maps one key while the settings editor owns input (ADR-0077). A declared key with its declared
    /// modifier yields its intent and a repeated activation is consumed, so a held <c>Space</c> or
    /// <c>Enter</c> changes a setting at most once. An undeclared key passes through, so <c>Tab</c> and
    /// the native controls keep their own keyboard behavior.
    /// </summary>
    private static KeyboardMappingOutcome MapSettingsKey(KeyboardInput input)
    {
        KeyboardMappingOutcome declared = MapDeclaredKey(input);
        return declared is MappedKeyboardIntent mapped &&
            mapped.Intent == UserIntent.Confirm &&
            input.RepeatState == KeyRepeatState.Repeated
                ? new KeyboardConsumed()
                : declared;
    }

    private static KeyboardMappingOutcome MapCommandEntryKey(KeyboardInput input)
    {
        return input.RepeatState == KeyRepeatState.Repeated &&
            (input.Key == KeyboardKey.Enter || input.Key == KeyboardKey.Space || input.Key == KeyboardKey.F1)
                ? new KeyboardConsumed()
                : MapDeclaredKey(input);
    }

    private static KeyboardMappingOutcome MapDeclaredKey(KeyboardInput input)
    {
        KeyBinding? binding = DeclaredBindings.FirstOrDefault(binding =>
            binding.Context == input.Context &&
            binding.Key == input.Key &&
            binding.Modifier == input.Modifier);
        return binding is null ? KeyboardFocusMap.Map(input)
            : binding.Intent == UserIntent.OpenContextMenu && input.RepeatState == KeyRepeatState.Repeated
                ? new KeyboardConsumed() : new MappedKeyboardIntent(binding.Intent);
    }

    private static bool IsChordPrefix(KeyboardInput input)
    {
        return input.Context == KeyboardContext.FileList &&
            input.Key == KeyboardKey.LowerG &&
            input.Modifier == KeyboardModifier.None &&
            input.RepeatState == KeyRepeatState.Initial;
    }

    private static bool IsRepeatedSingleShotCommand(KeyboardInput input)
    {
        return input.RepeatState == KeyRepeatState.Repeated &&
            (input.Key == KeyboardKey.F2 ||
                input.Key == KeyboardKey.F5 ||
                input.Key == KeyboardKey.F6 ||
                input.Key == KeyboardKey.F7 ||
                input.Key == KeyboardKey.F8 ||
                input.Key == KeyboardKey.F1 ||
                (input.Key == KeyboardKey.E && input.Modifier == KeyboardModifier.Control));
    }

}
