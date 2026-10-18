namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed result of parsing one untrusted bookmark display name.</summary>
public abstract record BookmarkDisplayNameParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected BookmarkDisplayNameParseOutcome()
    {
    }
}
