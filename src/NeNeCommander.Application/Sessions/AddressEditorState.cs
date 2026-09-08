using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Represents the closed set of application-owned address editing states.</summary>
public abstract record AddressEditorState
{
    /// <summary>Gets the shared closed state that requests no focus change.</summary>
    public static AddressEditorState Closed { get; } = new AddressEditorClosed(null);

    private protected AddressEditorState()
    {
    }

    internal static AddressEditorState CloseFocusing(PaneSide side)
    {
        return new AddressEditorClosed(side);
    }
}
