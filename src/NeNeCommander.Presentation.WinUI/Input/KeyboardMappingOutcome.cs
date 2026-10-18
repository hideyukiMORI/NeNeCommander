namespace NeNeCommander.Presentation.WinUI.Input;

/// <summary>Represents the closed outcome of canonical keyboard mapping.</summary>
public abstract record KeyboardMappingOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected KeyboardMappingOutcome()
    {
    }
}
