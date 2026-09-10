namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents the explicit focus context that owns one keyboard event.</summary>
public abstract record KeyboardContext
{
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

    private KeyboardContext()
    {
    }

    private sealed record FileListContext : KeyboardContext;
    private sealed record NavigationSurfaceContext : KeyboardContext;
    private sealed record TextEntryContext : KeyboardContext;
    private sealed record AddressEntryContext : KeyboardContext;
    private sealed record ModalContext : KeyboardContext;
    private sealed record CommandPaletteContext : KeyboardContext;
}
