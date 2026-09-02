namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Represents the closed result of parsing untrusted filesystem path text.
/// </summary>
public abstract record PathParseOutcome
{
    private protected PathParseOutcome()
    {
    }
}
