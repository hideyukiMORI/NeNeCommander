namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents the closed content of one pane: nothing listed yet, or one listed location.
/// </summary>
public abstract record PaneContent
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected PaneContent()
    {
    }

    /// <summary>Gets the content of a pane that has not listed any location.</summary>
    public static PaneContent Absent { get; } = new PaneContentAbsent();

    private sealed record PaneContentAbsent : PaneContent
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
