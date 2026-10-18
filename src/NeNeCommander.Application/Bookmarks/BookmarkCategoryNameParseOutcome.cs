namespace NeNeCommander.Application.Bookmarks;

/// <summary>Represents the closed result of parsing one untrusted category name.</summary>
public abstract record BookmarkCategoryNameParseOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected BookmarkCategoryNameParseOutcome()
    {
    }
}
