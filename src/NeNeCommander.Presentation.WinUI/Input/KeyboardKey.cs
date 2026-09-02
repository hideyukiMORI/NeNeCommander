namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Represents one closed, layout-translated key identity consumed by the canonical mapper.
/// </summary>
public abstract record KeyboardKey
{
    /// <summary>Gets the produced lower-case g key.</summary>
    public static KeyboardKey LowerG { get; } = new LowerGKey();

    /// <summary>Gets the produced upper-case G key.</summary>
    public static KeyboardKey UpperG { get; } = new UpperGKey();

    /// <summary>Gets the produced h key.</summary>
    public static KeyboardKey H { get; } = new HKey();

    /// <summary>Gets the produced j key.</summary>
    public static KeyboardKey J { get; } = new JKey();

    /// <summary>Gets the produced k key.</summary>
    public static KeyboardKey K { get; } = new KKey();

    /// <summary>Gets the produced l key.</summary>
    public static KeyboardKey L { get; } = new LKey();

    /// <summary>Gets the produced d key.</summary>
    public static KeyboardKey D { get; } = new DKey();

    /// <summary>Gets the produced r key.</summary>
    public static KeyboardKey R { get; } = new RKey();

    /// <summary>Gets the produced u key.</summary>
    public static KeyboardKey U { get; } = new UKey();

    /// <summary>Gets the down-arrow virtual key.</summary>
    public static KeyboardKey Down { get; } = new DownKey();

    /// <summary>Gets the up-arrow virtual key.</summary>
    public static KeyboardKey Up { get; } = new UpKey();

    /// <summary>Gets the Backspace virtual key.</summary>
    public static KeyboardKey Backspace { get; } = new BackspaceKey();

    /// <summary>Gets the Enter virtual key.</summary>
    public static KeyboardKey Enter { get; } = new EnterKey();

    /// <summary>Gets the Page Down virtual key.</summary>
    public static KeyboardKey PageDown { get; } = new PageDownKey();

    /// <summary>Gets the Page Up virtual key.</summary>
    public static KeyboardKey PageUp { get; } = new PageUpKey();

    /// <summary>Gets the Tab virtual key.</summary>
    public static KeyboardKey Tab { get; } = new TabKey();

    /// <summary>Gets the Space virtual key.</summary>
    public static KeyboardKey Space { get; } = new SpaceKey();

    /// <summary>Gets the Escape virtual key.</summary>
    public static KeyboardKey Escape { get; } = new EscapeKey();

    /// <summary>Gets the F2 virtual key.</summary>
    public static KeyboardKey F2 { get; } = new F2Key();

    /// <summary>Gets the F5 virtual key.</summary>
    public static KeyboardKey F5 { get; } = new F5Key();

    /// <summary>Gets the F6 virtual key.</summary>
    public static KeyboardKey F6 { get; } = new F6Key();

    /// <summary>Gets the F7 virtual key.</summary>
    public static KeyboardKey F7 { get; } = new F7Key();

    /// <summary>Gets the F8 virtual key.</summary>
    public static KeyboardKey F8 { get; } = new F8Key();

    /// <summary>Gets an input that has no command identity.</summary>
    public static KeyboardKey Other { get; } = new OtherKey();

    private KeyboardKey()
    {
    }

    private sealed record LowerGKey : KeyboardKey;
    private sealed record UpperGKey : KeyboardKey;
    private sealed record HKey : KeyboardKey;
    private sealed record JKey : KeyboardKey;
    private sealed record KKey : KeyboardKey;
    private sealed record LKey : KeyboardKey;
    private sealed record DKey : KeyboardKey;
    private sealed record RKey : KeyboardKey;
    private sealed record UKey : KeyboardKey;
    private sealed record DownKey : KeyboardKey;
    private sealed record UpKey : KeyboardKey;
    private sealed record BackspaceKey : KeyboardKey;
    private sealed record EnterKey : KeyboardKey;
    private sealed record PageDownKey : KeyboardKey;
    private sealed record PageUpKey : KeyboardKey;
    private sealed record TabKey : KeyboardKey;
    private sealed record SpaceKey : KeyboardKey;
    private sealed record EscapeKey : KeyboardKey;
    private sealed record F2Key : KeyboardKey;
    private sealed record F5Key : KeyboardKey;
    private sealed record F6Key : KeyboardKey;
    private sealed record F7Key : KeyboardKey;
    private sealed record F8Key : KeyboardKey;
    private sealed record OtherKey : KeyboardKey;
}
