using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Application.Commands;

/// <summary>Declares one existing command and its menu target group in the sole catalog.</summary>
internal sealed record CommandDefinition
{
    internal CommandDefinition(UserIntent intent, CommandMenuGroup group)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(group);
        Intent = intent;
        Group = group;
    }

    internal UserIntent Intent { get; }

    internal CommandMenuGroup Group { get; }
}
