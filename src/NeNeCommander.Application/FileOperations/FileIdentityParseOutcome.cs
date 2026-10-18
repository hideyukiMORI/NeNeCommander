namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents the closed result of validating a provider identity token.
/// </summary>
public abstract record FileIdentityParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected FileIdentityParseOutcome()
    {
    }
}
