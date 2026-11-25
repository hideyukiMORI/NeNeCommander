using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Names the modifier combinations admitted by the pane pointer map.</summary>
public abstract record PanePointerModifier
{
    /// <summary>Closes concrete modifier cases outside this assembly.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets an unmodified gesture.</summary>
    public static PanePointerModifier None { get; } = new NoModifier();

    /// <summary>Gets Control alone.</summary>
    public static PanePointerModifier Control { get; } = new ControlModifier();

    /// <summary>Gets Shift alone.</summary>
    public static PanePointerModifier Shift { get; } = new ShiftModifier();

    /// <summary>Gets Control and Shift together.</summary>
    public static PanePointerModifier ControlShift { get; } = new ControlShiftModifier();

    /// <summary>Gets every unsupported combination.</summary>
    public static PanePointerModifier Other { get; } = new OtherModifier();

    private PanePointerModifier()
    {
    }

    /// <summary>Translates framework key states without inspecting process-global input.</summary>
    public static PanePointerModifier Translate(
        CoreVirtualKeyStates control,
        CoreVirtualKeyStates shift,
        CoreVirtualKeyStates alt,
        CoreVirtualKeyStates windows)
    {
        return ((alt | windows) & CoreVirtualKeyStates.Down) != 0
            ? Other
            : (control & CoreVirtualKeyStates.Down) != 0
            ? (shift & CoreVirtualKeyStates.Down) != 0 ? ControlShift : Control
            : (shift & CoreVirtualKeyStates.Down) != 0 ? Shift : None;
    }

    private sealed record NoModifier : PanePointerModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ControlModifier : PanePointerModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ShiftModifier : PanePointerModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ControlShiftModifier : PanePointerModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record OtherModifier : PanePointerModifier
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
