namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies the closed key that orders a pane's entries inside the directory group and the file
/// group. The key is a closed state rather than an enum so no arbitrary value can be cast into it.
/// </summary>
public abstract record SortKey
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the key that orders entries by name, ignoring case and then ordinally.</summary>
    public static SortKey Name { get; } = new NameKey();

    /// <summary>
    /// Gets the key that orders entries by the text after the last dot of the name, ignoring case,
    /// with the name order deciding ties.
    /// </summary>
    public static SortKey Extension { get; } = new ExtensionKey();

    /// <summary>
    /// Gets the key that orders entries by the provider-reported byte count, an unknown size after
    /// every known one, with the name order deciding ties (ADR-0056).
    /// </summary>
    public static SortKey Size { get; } = new SizeKey();

    /// <summary>
    /// Gets the key that orders entries by the provider-reported last-modification time, an unknown
    /// time after every known one, with the name order deciding ties (ADR-0056).
    /// </summary>
    public static SortKey Modified { get; } = new ModifiedKey();

    private SortKey()
    {
    }

    private sealed record NameKey : SortKey
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ExtensionKey : SortKey
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record SizeKey : SortKey
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ModifiedKey : SortKey
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
