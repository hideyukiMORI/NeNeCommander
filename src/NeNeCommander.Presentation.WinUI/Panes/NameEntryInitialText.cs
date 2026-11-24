using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Owns the closed choice between literal initial text and the localized text-file suggestion.</summary>
public abstract record NameEntryInitialText
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    internal static NameEntryInitialText SuggestedTextFileName { get; } = new SuggestedText();

    private protected NameEntryInitialText()
    {
    }

    internal static NameEntryInitialText Create(string literal)
    {
        ArgumentNullException.ThrowIfNull(literal);
        return new LiteralText(literal);
    }

    /// <summary>Resolves the initial text using the host's localized filename suggestion.</summary>
    /// <param name="localizedSuggestion">The resource text for a new text file, never a path.</param>
    /// <returns>The unchanged literal or the supplied localized suggestion.</returns>
    public abstract string Resolve(string localizedSuggestion);

    private sealed record LiteralText : NameEntryInitialText
    {
        private readonly string _literal;

        internal LiteralText(string literal)
        {
            _literal = literal;
        }

        private protected override void RestrictDerivation()
        {
        }

        public override string Resolve(string localizedSuggestion)
        {
            ArgumentNullException.ThrowIfNull(localizedSuggestion);
            return _literal;
        }
    }

    private sealed record SuggestedText : NameEntryInitialText
    {
        private protected override void RestrictDerivation()
        {
        }

        public override string Resolve(string localizedSuggestion)
        {
            ArgumentNullException.ThrowIfNull(localizedSuggestion);
            return localizedSuggestion;
        }
    }
}
