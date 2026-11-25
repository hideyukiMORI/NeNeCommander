using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the filter editor lifetime without duplicating the pane's applied query.</summary>
public abstract record PaneFilterState
{
    /// <summary>Closes concrete states to this assembly and trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the initial closed editor with no focus-return effect.</summary>
    public static PaneFilterState Closed { get; } = new PaneFilterClosed(null);

    private protected PaneFilterState()
    {
    }

    internal static PaneFilterState CloseFocusing(PaneSide side)
    {
        return new PaneFilterClosed(side);
    }
}
