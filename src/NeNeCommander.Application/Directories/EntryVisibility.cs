namespace NeNeCommander.Application.Directories;

/// <summary>
/// Identifies the closed visibility one provider reports for a direct directory entry. The value
/// is what the provider says about the entry itself, never a guess from its name: a Windows local
/// entry is <see cref="Hidden"/> only when the filesystem marks it hidden or system. A listing
/// always reports every entry (FS-011); this value is the input the pane transition uses to decide
/// which entries the pane shows.
/// </summary>
public abstract record EntryVisibility
{
    /// <summary>Gets the visibility of an entry the provider does not mark hidden or system.</summary>
    public static EntryVisibility Normal { get; } = new NormalEntryVisibility();

    /// <summary>Gets the visibility of an entry the provider marks hidden or system.</summary>
    public static EntryVisibility Hidden { get; } = new HiddenEntryVisibility();

    private EntryVisibility()
    {
    }

    private sealed record NormalEntryVisibility : EntryVisibility;
    private sealed record HiddenEntryVisibility : EntryVisibility;
}
