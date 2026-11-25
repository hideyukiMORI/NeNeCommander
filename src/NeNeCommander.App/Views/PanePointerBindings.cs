using System;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>Supplies both pane adapters with the window's one pointer map and intent route.</summary>
internal sealed record PanePointerBindings
{
    internal PanePointerBindings(PanePointerMapper mapper, Action<UserIntent> forward,
        Func<FrameworkElement, Point, UserIntent, Action?> requestContext)
    {
        RequestContext = requestContext;
        Mapper = mapper;
        Forward = forward;
    }

    internal PanePointerMapper Mapper { get; }

    internal Action<UserIntent> Forward { get; }

    internal Func<FrameworkElement, Point, UserIntent, Action?> RequestContext { get; }
}
