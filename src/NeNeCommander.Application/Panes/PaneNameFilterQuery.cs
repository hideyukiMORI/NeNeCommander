using System;

namespace NeNeCommander.Application.Panes;

/// <summary>Owns exact committed text and its literal, ordinal-ignore-case AND terms.</summary>
public sealed record PaneNameFilterQuery
{
    private readonly string[] _terms;

    private PaneNameFilterQuery(string text)
    {
        Text = text;
        _terms = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Gets the exact native committed text without trimming or normalization.</summary>
    public string Text { get; }

    /// <summary>Gets whether no term restricts the loaded entry names.</summary>
    public bool IsEmpty => _terms.Length == 0;

    /// <summary>Gets the immutable unrestricted query.</summary>
    public static PaneNameFilterQuery Empty { get; } = new(string.Empty);

    /// <summary>Parses any non-null committed text once without path or wildcard interpretation.</summary>
    /// <param name="text">Exact native committed text.</param>
    /// <returns>The owned literal query.</returns>
    public static PaneNameFilterQuery Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new PaneNameFilterQuery(text);
    }

    internal bool Matches(string name)
    {
        foreach (string term in _terms)
        {
            if (!name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }
}
