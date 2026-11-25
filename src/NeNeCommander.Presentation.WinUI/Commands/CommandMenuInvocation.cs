using System;
using System.Linq;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Qualifies one native flyout's invocation-turn callbacks with its existing Application owner.</summary>
internal sealed class CommandMenuInvocation
{
    private bool _completed;
    private bool _closeQueued;

    internal CommandMenuInvocation(CommandPaletteOpen sourceState)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        SourceState = sourceState;
    }

    internal CommandPaletteOpen SourceState { get; }

    internal UserIntent? Submit(CommandCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (_completed || candidate.Availability != CommandAvailability.Available ||
            !SourceState.Candidates.Any(owned => ReferenceEquals(owned, candidate)))
        {
            return null;
        }
        _completed = true;
        return UserIntent.SubmitCommand(SourceState, candidate.Intent);
    }

    internal void Close(Action<Action> enqueue, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(enqueue);
        ArgumentNullException.ThrowIfNull(forward);
        if (_completed || _closeQueued)
        {
            return;
        }
        _closeQueued = true;
        enqueue(() => FinalizeClose(forward));
    }

    private void FinalizeClose(Action<UserIntent> forward)
    {
        if (!_completed)
        {
            _completed = true;
            forward(UserIntent.CancelCommandPalette(SourceState));
        }
    }

    internal void Detach()
    {
        _completed = true;
    }
}
