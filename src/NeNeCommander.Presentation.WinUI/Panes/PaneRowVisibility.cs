using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed rendering of one entry's visibility: the semantic brush the row's name
/// uses. A row exists only when the pane shows the entry, so the hidden rendering means "shown
/// while hidden entries are shown", which is why it is muted rather than absent. The mapping from
/// the application's <see cref="EntryVisibility"/> happens once here so no view decides it.
/// </summary>
public abstract record PaneRowVisibility
{
    /// <summary>Gets the rendering of an entry the provider does not mark hidden or system.</summary>
    public static PaneRowVisibility Normal { get; } = new NormalRowVisibility();

    /// <summary>Gets the rendering of an entry the provider marks hidden or system.</summary>
    public static PaneRowVisibility Hidden { get; } = new HiddenRowVisibility();

    private PaneRowVisibility()
    {
    }

    /// <summary>Gets the semantic brush resource key of the row's entry name.</summary>
    public abstract string NameBrushResourceKey { get; }

    /// <summary>Translates the application's closed entry visibility into its rendering.</summary>
    /// <param name="visibility">Closed visibility reported by the provider.</param>
    /// <returns>The rendering of that visibility.</returns>
    public static PaneRowVisibility For(EntryVisibility visibility)
    {
        ArgumentNullException.ThrowIfNull(visibility);
        return visibility == EntryVisibility.Hidden ? Hidden : Normal;
    }

    private sealed record NormalRowVisibility : PaneRowVisibility
    {
        public override string NameBrushResourceKey => "TextPrimaryBrush";
    }

    private sealed record HiddenRowVisibility : PaneRowVisibility
    {
        public override string NameBrushResourceKey => "TextHiddenBrush";
    }
}
