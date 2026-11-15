namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>Represents the closed pointer state one caption button shows (ADR-0078).</summary>
public abstract record CaptionButtonPointer
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the state of a button without the pointer over it.</summary>
    public static CaptionButtonPointer Rest { get; } = new RestPointer();

    /// <summary>Gets the state of a button with the pointer over it.</summary>
    public static CaptionButtonPointer Hovered { get; } = new HoveredPointer();

    /// <summary>Gets the state of a button while a pointer press on it is held.</summary>
    public static CaptionButtonPointer Pressed { get; } = new PressedPointer();

    private CaptionButtonPointer()
    {
    }

    private sealed record RestPointer : CaptionButtonPointer
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record HoveredPointer : CaptionButtonPointer
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record PressedPointer : CaptionButtonPointer
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
