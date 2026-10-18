namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed result of validating one intent against the address editor scope. The
/// scope state is already final when the result is returned.
/// </summary>
public abstract record AddressEditorValidation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the result that leaves the session no address target to navigate.</summary>
    public static AddressEditorValidation NothingToNavigate { get; } = new NoNavigation();

    private protected AddressEditorValidation()
    {
    }

    private sealed record NoNavigation : AddressEditorValidation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
