namespace NeNeCommander.Application.Panes;

/// <summary>Captures whether the explicit opposite-pane command currently owns an abandonment route.</summary>
internal abstract record OtherPaneNavigationState
{
    private protected abstract void RestrictDerivation();

    internal static OtherPaneNavigationState Idle { get; } = new IdleOtherPaneNavigation();

    private protected OtherPaneNavigationState()
    {
    }

    private sealed record IdleOtherPaneNavigation : OtherPaneNavigationState
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
