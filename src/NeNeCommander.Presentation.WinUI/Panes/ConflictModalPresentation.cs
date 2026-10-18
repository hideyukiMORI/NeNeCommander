namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents whether the host shows the transfer conflict modal.</summary>
public abstract record ConflictModalPresentation
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the hidden modal state.</summary>
    public static ConflictModalPresentation Hidden { get; } = new HiddenConflictModal();

    private protected ConflictModalPresentation()
    {
    }

    private sealed record HiddenConflictModal : ConflictModalPresentation
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
