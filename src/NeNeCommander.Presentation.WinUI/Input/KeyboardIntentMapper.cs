using System;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Time;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Owns the sole context-aware mapping from translated keyboard input to application intent.
/// </summary>
public sealed class KeyboardIntentMapper
{
    private static readonly TimeSpan ChordLifetime = TimeSpan.FromMilliseconds(750);
    private readonly IClock _clock;
    private TimeSpan? _pendingChordStartedAt;

    /// <summary>Initializes the mapper with a monotonic clock used only for chord expiry.</summary>
    /// <param name="clock">Monotonic clock.</param>
    public KeyboardIntentMapper(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
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
            return input.Key == KeyboardKey.Escape
                ? MapIntent(UserIntent.Escape)
                : new KeyboardPassThrough();
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
            : MapSingleKey(input);
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

    private static KeyboardMappingOutcome MapSingleKey(KeyboardInput input)
    {
        UserIntent? intent = input.Modifier == KeyboardModifier.None
            ? MapUnmodified(input)
            : MapModified(input);
        return intent is null ? new KeyboardPassThrough() : MapIntent(intent);
    }

    private static UserIntent? MapUnmodified(KeyboardInput input)
    {
        return input.Context == KeyboardContext.NavigationSurface
            ? input.Key == KeyboardKey.F5 ? UserIntent.Refresh : null
            : input.Key switch
            {
                KeyboardKey key when key == KeyboardKey.J || key == KeyboardKey.Down => UserIntent.MoveNext,
                KeyboardKey key when key == KeyboardKey.K || key == KeyboardKey.Up => UserIntent.MovePrevious,
                KeyboardKey key when key == KeyboardKey.H || key == KeyboardKey.Backspace => UserIntent.NavigateParent,
                KeyboardKey key when key == KeyboardKey.L || key == KeyboardKey.Enter => UserIntent.OpenFocused,
                KeyboardKey key when key == KeyboardKey.UpperG => UserIntent.FocusLast,
                KeyboardKey key when key == KeyboardKey.PageDown => UserIntent.MoveHalfPageDown,
                KeyboardKey key when key == KeyboardKey.PageUp => UserIntent.MoveHalfPageUp,
                KeyboardKey key when key == KeyboardKey.Tab => UserIntent.ActivateOtherPane,
                KeyboardKey key when key == KeyboardKey.Space => UserIntent.ToggleSelection,
                KeyboardKey key when key == KeyboardKey.Escape => UserIntent.Escape,
                KeyboardKey key when key == KeyboardKey.F2 => UserIntent.Rename,
                KeyboardKey key when key == KeyboardKey.F5 => UserIntent.Copy,
                KeyboardKey key when key == KeyboardKey.F6 => UserIntent.Move,
                KeyboardKey key when key == KeyboardKey.F7 => UserIntent.CreateDirectory,
                KeyboardKey key when key == KeyboardKey.F8 => UserIntent.Delete,
                _ => null,
            };
    }

    private static UserIntent? MapModified(KeyboardInput input)
    {
        return input.Modifier == KeyboardModifier.Alt && input.Key == KeyboardKey.Up
            ? UserIntent.NavigateParent
            : input.Modifier == KeyboardModifier.Control && input.Key == KeyboardKey.D
                ? UserIntent.MoveHalfPageDown
                : input.Modifier == KeyboardModifier.Control && input.Key == KeyboardKey.U
                    ? UserIntent.MoveHalfPageUp
                    : input.Modifier == KeyboardModifier.Control && input.Key == KeyboardKey.L
                        ? UserIntent.FocusAddress
                        : input.Modifier == KeyboardModifier.Control && input.Key == KeyboardKey.R
                            ? UserIntent.Refresh
                            : null;
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
