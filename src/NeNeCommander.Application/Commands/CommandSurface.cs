namespace NeNeCommander.Application.Commands;

/// <summary>Names the closed command-selection surfaces sharing one captured owner.</summary>
public abstract record CommandSurface
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the searchable palette containing every catalog command.</summary>
    public static CommandSurface Palette { get; } = new PaletteSurface();

    /// <summary>Gets the menu containing focused, selected-source and folder commands.</summary>
    public static CommandSurface ItemContext { get; } = new ItemContextSurface();

    /// <summary>Gets the menu containing only current-folder commands.</summary>
    public static CommandSurface FolderContext { get; } = new FolderContextSurface();

    private CommandSurface()
    {
    }

    private sealed record PaletteSurface : CommandSurface
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ItemContextSurface : CommandSurface
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record FolderContextSurface : CommandSurface
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
