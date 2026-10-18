namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents the closed result of parsing untrusted persisted color-scheme text.
/// </summary>
public abstract record ColorSchemeParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected ColorSchemeParseOutcome()
    {
    }
}
