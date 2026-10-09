namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies the closed key that orders a pane's entries inside the directory group and the file
/// group. The key is a closed state rather than an enum so no arbitrary value can be cast into it.
/// </summary>
public abstract record SortKey
{
    /// <summary>Gets the key that orders entries by name, ignoring case and then ordinally.</summary>
    public static SortKey Name { get; } = new NameKey();

    /// <summary>
    /// Gets the key that orders entries by the text after the last dot of the name, ignoring case,
    /// with the name order deciding ties.
    /// </summary>
    public static SortKey Extension { get; } = new ExtensionKey();

    private SortKey()
    {
    }

    private sealed record NameKey : SortKey;
    private sealed record ExtensionKey : SortKey;
}
