using System;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>Supplies both pane adapters with the window's one pointer map and intent route.</summary>
internal sealed record PanePointerBindings
{
    internal PanePointerBindings(PanePointerMapper mapper, Action<UserIntent> forward)
    {
        Mapper = mapper;
        Forward = forward;
    }

    internal PanePointerMapper Mapper { get; }

    internal Action<UserIntent> Forward { get; }
}
