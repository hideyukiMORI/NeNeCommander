namespace NeNeCommander.Application.Input;

/// <summary>Defines the closed filter actions requested from a pane surface.</summary>
public abstract record PaneFilterEntryAction
{
    /// <summary>Closes concrete actions to this assembly and trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Opens the native editor for the addressed pane.</summary>
    public static PaneFilterEntryAction Open { get; } = new OpenAction();

    /// <summary>Clears the addressed pane through the same query transition.</summary>
    public static PaneFilterEntryAction Clear { get; } = new ClearAction();

    private PaneFilterEntryAction()
    {
    }

    private sealed record OpenAction : PaneFilterEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ClearAction : PaneFilterEntryAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
