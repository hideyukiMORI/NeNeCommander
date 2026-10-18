namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies one closed pane-state construction failure.
/// </summary>
public abstract record PaneStateFailureKind
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the failure for a null visible item.</summary>
    public static PaneStateFailureKind NullItem { get; } = new NullItemFailure();

    /// <summary>Gets the failure for a duplicate visible path.</summary>
    public static PaneStateFailureKind DuplicateItem { get; } = new DuplicateItemFailure();

    private PaneStateFailureKind()
    {
    }

    private sealed record NullItemFailure : PaneStateFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record DuplicateItemFailure : PaneStateFailureKind
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
