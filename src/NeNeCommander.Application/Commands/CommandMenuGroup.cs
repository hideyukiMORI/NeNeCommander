namespace NeNeCommander.Application.Commands;

/// <summary>Declares canonical menu membership and target grouping beside each catalog intent.</summary>
public abstract record CommandMenuGroup
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets commands available only from the searchable palette.</summary>
    public static CommandMenuGroup PaletteOnly { get; } = new PaletteOnlyGroup();

    /// <summary>Gets commands addressing the captured focused entry.</summary>
    public static CommandMenuGroup FocusedItem { get; } = new FocusedItemGroup();

    /// <summary>Gets commands addressing the captured selection or focused fallback.</summary>
    public static CommandMenuGroup Selection { get; } = new SelectionGroup();

    /// <summary>Gets commands addressing the captured current folder.</summary>
    public static CommandMenuGroup Folder { get; } = new FolderGroup();

    /// <summary>Gets name-entry commands creating one child of the captured folder.</summary>
    public static CommandMenuGroup New { get; } = new NewGroup();

    private CommandMenuGroup()
    {
    }

    internal bool AppearsOn(CommandSurface surface)
    {
        return surface switch
        {
            _ when surface == CommandSurface.Palette => true,
            _ when surface == CommandSurface.ItemContext => this != PaletteOnly,
            _ => this == Folder || this == New,
        };
    }

    private sealed record PaletteOnlyGroup : CommandMenuGroup
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record FocusedItemGroup : CommandMenuGroup
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record SelectionGroup : CommandMenuGroup
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record FolderGroup : CommandMenuGroup
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record NewGroup : CommandMenuGroup
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
