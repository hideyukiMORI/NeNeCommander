namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Identifies whether one transient scope owns keyboard input for the decision it is making, or
/// whether another scope, a modal, or in-flight pane work holds that input instead.
/// </summary>
public abstract record InteractionOwnership
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the ownership the scope receives while it may act on input.</summary>
    public static InteractionOwnership ScopeOwnsInput { get; } = new OwnedByThisScope();

    /// <summary>Gets the ownership the scope receives while another owner holds input.</summary>
    public static InteractionOwnership AnotherScopeOwnsInput { get; } = new OwnedByAnotherScope();

    private InteractionOwnership()
    {
    }

    private sealed record OwnedByThisScope : InteractionOwnership
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record OwnedByAnotherScope : InteractionOwnership
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
