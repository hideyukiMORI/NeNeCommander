namespace NeNeCommander.Application.Input;

/// <summary>Defines the closed ways native filter editing ends.</summary>
public abstract record PaneFilterEditAction
{
    /// <summary>Closes concrete actions to this assembly and trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Keeps the applied query and returns focus once.</summary>
    public static PaneFilterEditAction Commit { get; } = new CommitAction();

    /// <summary>Clears the query and returns focus once.</summary>
    public static PaneFilterEditAction Cancel { get; } = new CancelAction();

    /// <summary>Keeps the query without moving focus.</summary>
    public static PaneFilterEditAction Blur { get; } = new BlurAction();

    private PaneFilterEditAction()
    {
    }

    private sealed record CommitAction : PaneFilterEditAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record CancelAction : PaneFilterEditAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record BlurAction : PaneFilterEditAction
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
