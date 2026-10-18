namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the closed result of creating an initial pane state.
/// </summary>
public abstract record PaneStateCreation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected PaneStateCreation()
    {
    }
}
