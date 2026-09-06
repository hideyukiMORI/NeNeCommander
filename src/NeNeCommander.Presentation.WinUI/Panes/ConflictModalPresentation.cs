namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents whether the host shows the transfer conflict modal.</summary>
public abstract record ConflictModalPresentation
{
    /// <summary>Gets the hidden modal state.</summary>
    public static ConflictModalPresentation Hidden { get; } = new HiddenConflictModal();

    private protected ConflictModalPresentation()
    {
    }

    private sealed record HiddenConflictModal : ConflictModalPresentation;
}
