namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents the explicit focus context that owns one keyboard event.</summary>
public abstract record KeyboardContext
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the file-list context with file commands enabled.</summary>
    public static KeyboardContext FileList { get; } = new FileListContext();

    /// <summary>Gets the native command-entry focus group, including visible read-only transfer details, without file commands.</summary>
    public static KeyboardContext CommandEntry { get; } = new CommandEntryContext();

    /// <summary>Gets a non-file navigation surface where refresh remains available.</summary>
    public static KeyboardContext NavigationSurface { get; } = new NavigationSurfaceContext();

    /// <summary>Gets a text-editor context that owns printable and editing input.</summary>
    public static KeyboardContext TextEntry { get; } = new TextEntryContext();

    /// <summary>Gets the address editor context that owns only navigation submission keys.</summary>
    public static KeyboardContext AddressEntry { get; } = new AddressEntryContext();

    /// <summary>Gets the native pane-filter editor context with qualified completion and cancellation.</summary>
    public static KeyboardContext PaneFilter { get; } = new PaneFilterContext();

    private sealed record PaneFilterContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    /// <summary>Gets a modal context that blocks the underlying file-list map.</summary>
    public static KeyboardContext Modal { get; } = new ModalContext();

    /// <summary>Gets the command-palette context that owns selection, execution, and cancellation.</summary>
    public static KeyboardContext CommandPalette { get; } = new CommandPaletteContext();

    /// <summary>Gets the window-adjustment context that owns movement, sizing, and leaving the mode.</summary>
    public static KeyboardContext WindowAdjustment { get; } = new WindowAdjustmentContext();

    /// <summary>Gets the Locations picker context that owns focus movement, selection, and closing.</summary>
    public static KeyboardContext Locations { get; } = new LocationsContext();

    /// <summary>Gets the settings editor context that owns item movement, activation and closing (ADR-0077).</summary>
    public static KeyboardContext Settings { get; } = new SettingsContext();

    /// <summary>Gets the keyboard help context that owns scrolling and closing the help (ADR-0076).</summary>
    public static KeyboardContext KeyboardHelp { get; } = new KeyboardHelpContext();

    /// <summary>
    /// Gets the bookmark manager's browse context that owns movement, region switching, the row and
    /// category commands and closing (ADR-0080).
    /// </summary>
    public static KeyboardContext Bookmarks { get; } = new BookmarksContext();

    /// <summary>
    /// Gets the bookmark manager's search-field context: text passes to the field and only the keys
    /// that return to the bookmark list are owned (ADR-0080).
    /// </summary>
    public static KeyboardContext BookmarkSearch { get; } = new BookmarkSearchContext();

    /// <summary>Gets the bookmark draft context with explicit save and cancel keys.</summary>
    public static KeyboardContext BookmarkDraft { get; } = new BookmarkDraftContext();

    /// <summary>Gets the native context menu whose navigation stays with its framework control.</summary>
    public static KeyboardContext ContextMenu { get; } = new ContextMenuContext();

    private sealed record ContextMenuContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private KeyboardContext()
    {
    }

    private sealed record FileListContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CommandEntryContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record NavigationSurfaceContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record TextEntryContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AddressEntryContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ModalContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record CommandPaletteContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record WindowAdjustmentContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record LocationsContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record SettingsContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record KeyboardHelpContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarksContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarkSearchContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record BookmarkDraftContext : KeyboardContext
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
