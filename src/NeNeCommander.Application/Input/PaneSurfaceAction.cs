namespace NeNeCommander.Application.Input;

/// <summary>Defines the closed actions admitted for one qualified pane gesture.</summary>
public abstract record PaneSurfaceAction
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the Activate action.</summary>
    public static PaneSurfaceAction Activate { get; } = new ActivateAction();

    /// <summary>Gets the ClearSelection action.</summary>
    public static PaneSurfaceAction ClearSelection { get; } = new ClearSelectionAction();

    private PaneSurfaceAction()
    {
    }

    private sealed record ActivateAction : PaneSurfaceAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record ClearSelectionAction : PaneSurfaceAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
