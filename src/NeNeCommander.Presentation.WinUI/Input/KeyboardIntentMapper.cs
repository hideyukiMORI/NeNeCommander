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
        new(KeyboardContext.FileList, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.FileList, KeyboardKey.F2, KeyboardModifier.None, UserIntent.Rename),
        new(KeyboardContext.FileList, KeyboardKey.F5, KeyboardModifier.None, UserIntent.Copy),
        new(KeyboardContext.FileList, KeyboardKey.F6, KeyboardModifier.None, UserIntent.Move),
        new(KeyboardContext.FileList, KeyboardKey.F7, KeyboardModifier.None, UserIntent.CreateDirectory),
        new(KeyboardContext.FileList, KeyboardKey.F8, KeyboardModifier.None, UserIntent.Delete),
        new(KeyboardContext.FileList, KeyboardKey.Up, KeyboardModifier.Alt, UserIntent.NavigateParent),
        new(KeyboardContext.FileList, KeyboardKey.D, KeyboardModifier.Control, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.FileList, KeyboardKey.U, KeyboardModifier.Control, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.FileList, KeyboardKey.L, KeyboardModifier.Control, UserIntent.FocusAddress),
        new(KeyboardContext.FileList, KeyboardKey.R, KeyboardModifier.Control, UserIntent.Refresh),
        new(KeyboardContext.FileList, KeyboardKey.Comma, KeyboardModifier.Control, UserIntent.OpenSettings),
        new(KeyboardContext.NavigationSurface, KeyboardKey.F5, KeyboardModifier.None, UserIntent.Refresh),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Up, KeyboardModifier.Alt, UserIntent.NavigateParent),
        new(KeyboardContext.NavigationSurface, KeyboardKey.D, KeyboardModifier.Control, UserIntent.MoveHalfPageDown),
        new(KeyboardContext.NavigationSurface, KeyboardKey.U, KeyboardModifier.Control, UserIntent.MoveHalfPageUp),
        new(KeyboardContext.NavigationSurface, KeyboardKey.L, KeyboardModifier.Control, UserIntent.FocusAddress),
        new(KeyboardContext.NavigationSurface, KeyboardKey.R, KeyboardModifier.Control, UserIntent.Refresh),
        new(KeyboardContext.NavigationSurface, KeyboardKey.Comma, KeyboardModifier.Control, UserIntent.OpenSettings),
        new(KeyboardContext.Modal, KeyboardKey.Enter, KeyboardModifier.None, UserIntent.Confirm),
        new(KeyboardContext.Modal, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
        new(KeyboardContext.TextEntry, KeyboardKey.Escape, KeyboardModifier.None, UserIntent.Escape),
    ];

    private readonly IClock _clock;
    private TimeSpan? _pendingChordStartedAt;

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

    internal static KeyboardMappingOutcome DeferConflictConfirmToNativeControl(
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
        if (input.Context == KeyboardContext.TextEntry || input.Context == KeyboardContext.Modal)
        {
            _pendingChordStartedAt = null;
            return MapOwnedKey(input);
        }

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

        return IsRepeatedDestructiveCommand(input)
            ? new KeyboardPassThrough()
            : MapDeclaredKey(input);
    }

    private MappedKeyboardIntent? TryCompleteChord(KeyboardInput input)
    {
        if (_pendingChordStartedAt is not TimeSpan startedAt)
        {
            return null;
        }

        _pendingChordStartedAt = null;
        TimeSpan elapsed = _clock.GetMonotonicTime() - startedAt;
        return elapsed <= ChordLifetime && IsChordPrefix(input)
            ? MapIntent(UserIntent.FocusFirst)
            : null;
    }

    /// <summary>
    /// Maps a key a modal or text editor owns. Those contexts own their declared keys whatever the
    /// modifier state, so the modifier of the declaration is not compared here (KBD-002).
    /// </summary>
    private static KeyboardMappingOutcome MapOwnedKey(KeyboardInput input)
    {
        KeyBinding? binding = DeclaredBindings.FirstOrDefault(binding =>
            binding.Context == input.Context && binding.Key == input.Key);
        return binding is null ? new KeyboardPassThrough() : MapIntent(binding.Intent);
    }

    private static KeyboardMappingOutcome MapDeclaredKey(KeyboardInput input)
    {
        KeyBinding? binding = DeclaredBindings.FirstOrDefault(binding =>
            binding.Context == input.Context &&
            binding.Key == input.Key &&
            binding.Modifier == input.Modifier);
        return binding is null ? new KeyboardPassThrough() : MapIntent(binding.Intent);
    }

    private static bool IsChordPrefix(KeyboardInput input)
    {
        return input.Context == KeyboardContext.FileList &&
            input.Key == KeyboardKey.LowerG &&
            input.Modifier == KeyboardModifier.None &&
            input.RepeatState == KeyRepeatState.Initial;
    }

    private static bool IsRepeatedDestructiveCommand(KeyboardInput input)
    {
        return input.RepeatState == KeyRepeatState.Repeated &&
            (input.Key == KeyboardKey.F2 ||
                input.Key == KeyboardKey.F5 ||
                input.Key == KeyboardKey.F6 ||
                input.Key == KeyboardKey.F7 ||
                input.Key == KeyboardKey.F8);
    }

    private static MappedKeyboardIntent MapIntent(UserIntent intent)
    {
        return new MappedKeyboardIntent(intent);
    }
}
