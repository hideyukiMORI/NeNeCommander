using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace NeNeCommander.App.Views;

/// <summary>Holds framework geometry for one synchronous context request, never its command target.</summary>
internal sealed record CommandMenuPlacement
{
    internal CommandMenuPlacement(FrameworkElement anchor, Point point)
    {
        Anchor = anchor;
        Point = point;
    }

    internal FrameworkElement Anchor { get; }

    internal Point Point { get; }
}
