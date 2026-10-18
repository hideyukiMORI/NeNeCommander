namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed admission decision for one address editing request. It changes no scope
/// state, so the session can activate the requested pane before the editor opens.
/// </summary>
public abstract record AddressEditAdmission
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the decision that refuses the request and leaves the scope untouched.</summary>
    public static AddressEditAdmission Refused { get; } = new RefusedRequest();

    private protected AddressEditAdmission()
    {
    }

    private sealed record RefusedRequest : AddressEditAdmission
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
