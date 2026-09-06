namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>
/// Represents one closed, layout-translated key identity consumed by the canonical mapper. Each
/// identity also names the localization resource for its key-cap label so a displayed shortcut
/// hint never assembles that text in code (KBD-005, CS-025).
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

    /// <summary>Gets the produced b key.</summary>
    public static KeyboardKey B { get; } = new BKey();

    /// <summary>Gets the 1 key.</summary>
    public static KeyboardKey One { get; } = new NumberKey("KeyLabel1");

    /// <summary>Gets the 2 key.</summary>
    public static KeyboardKey Two { get; } = new NumberKey("KeyLabel2");

    /// <summary>Gets the 3 key.</summary>
    public static KeyboardKey Three { get; } = new NumberKey("KeyLabel3");

    /// <summary>Gets the 4 key.</summary>
    public static KeyboardKey Four { get; } = new NumberKey("KeyLabel4");

    /// <summary>Gets the 5 key.</summary>
    public static KeyboardKey Five { get; } = new NumberKey("KeyLabel5");

    /// <summary>Gets the 6 key.</summary>
    public static KeyboardKey Six { get; } = new NumberKey("KeyLabel6");

    /// <summary>Gets the 7 key.</summary>
    public static KeyboardKey Seven { get; } = new NumberKey("KeyLabel7");

    /// <summary>Gets the 8 key.</summary>
    public static KeyboardKey Eight { get; } = new NumberKey("KeyLabel8");

    /// <summary>Gets the 9 key.</summary>
    public static KeyboardKey Nine { get; } = new NumberKey("KeyLabel9");

    /// <summary>Gets the produced comma key.</summary>
    public static KeyboardKey Comma { get; } = new CommaKey();

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

    /// <summary>
    /// Gets the localization resource key that names this key on a displayed shortcut hint.
    /// <see cref="Other"/> names an empty label because no canonical binding declares it.
    /// </summary>
    public abstract string LabelResourceKey { get; }

    private sealed record LowerGKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelLowerG";
    }

    private sealed record UpperGKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelUpperG";
    }

    private sealed record HKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelH";
    }

    private sealed record JKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelJ";
    }

    private sealed record KKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelK";
    }

    private sealed record LKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelL";
    }

    private sealed record DKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelD";
    }

    private sealed record RKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelR";
    }

    private sealed record UKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelU";
    }

    private sealed record BKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelB";
    }

    private sealed record NumberKey : KeyboardKey
    {
        internal NumberKey(string labelResourceKey)
        {
            LabelResourceKey = labelResourceKey;
        }

        public override string LabelResourceKey { get; }
    }

    private sealed record CommaKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelComma";
    }

    private sealed record DownKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelDown";
    }

    private sealed record UpKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelUp";
    }

    private sealed record BackspaceKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelBackspace";
    }

    private sealed record EnterKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelEnter";
    }

    private sealed record PageDownKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelPageDown";
    }

    private sealed record PageUpKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelPageUp";
    }

    private sealed record TabKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelTab";
    }

    private sealed record SpaceKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelSpace";
    }

    private sealed record EscapeKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelEscape";
    }

    private sealed record F2Key : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelF2";
    }

    private sealed record F5Key : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelF5";
    }

    private sealed record F6Key : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelF6";
    }

    private sealed record F7Key : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelF7";
    }

    private sealed record F8Key : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelF8";
    }

    private sealed record OtherKey : KeyboardKey
    {
        public override string LabelResourceKey => "KeyLabelUnmapped";
    }
}
