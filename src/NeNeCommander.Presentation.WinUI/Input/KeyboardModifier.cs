namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents the closed modifier state attached to one translated key.</summary>
public abstract record KeyboardModifier
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets input with no command modifier.</summary>
    public static KeyboardModifier None { get; } = new NoModifier();

    /// <summary>Gets input modified by Control.</summary>
    public static KeyboardModifier Control { get; } = new ControlModifier();

    /// <summary>Gets input modified by Alt.</summary>
    public static KeyboardModifier Alt { get; } = new AltModifier();

    /// <summary>Gets a modifier combination that has no command mapping.</summary>
    public static KeyboardModifier Other { get; } = new OtherModifier();

    private KeyboardModifier()
    {
    }

    private sealed record NoModifier : KeyboardModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ControlModifier : KeyboardModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AltModifier : KeyboardModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record OtherModifier : KeyboardModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
