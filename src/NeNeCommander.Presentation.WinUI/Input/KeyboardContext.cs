namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents the explicit focus context that owns one keyboard event.</summary>
public abstract record KeyboardContext
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the file-list context with file commands enabled.</summary>
    public static KeyboardContext FileList { get; } = new FileListContext();

    /// <summary>Gets a non-file navigation surface where refresh remains available.</summary>
    public static KeyboardContext NavigationSurface { get; } = new NavigationSurfaceContext();

    /// <summary>Gets a text-editor context that owns printable and editing input.</summary>
    public static KeyboardContext TextEntry { get; } = new TextEntryContext();

    /// <summary>Gets the address editor context that owns only navigation submission keys.</summary>
    public static KeyboardContext AddressEntry { get; } = new AddressEntryContext();

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

    private KeyboardContext()
    {
    }

    private sealed record FileListContext : KeyboardContext
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
}
