namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed result of parsing one untrusted bookmark path.</summary>
public abstract record BookmarkPathParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected BookmarkPathParseOutcome()
    {
    }
}
