using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the visible, focused name editor together with the text it starts from: empty for a
/// new directory, a localized suggestion for a text file, or the existing name for a rename.
/// </summary>
public sealed record ActiveNameEntry : NameEntryPresentation
{
    private protected override void RestrictDerivation()
    {
    }

    internal ActiveNameEntry(NameEntryInitialText initialText)
    {
        ArgumentNullException.ThrowIfNull(initialText);
        InitialText = initialText;
    }

    /// <summary>Gets the text the host assigns to the editor when it becomes visible.</summary>
    public NameEntryInitialText InitialText { get; }
}
