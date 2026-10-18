namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents the closed result of parsing untrusted filesystem path text.
/// </summary>
public abstract record PathParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected PathParseOutcome()
    {
    }
}
