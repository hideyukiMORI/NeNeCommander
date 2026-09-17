namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the closed result of validating one intent against the command palette scope. The
/// scope state is already final when the result is returned.
/// </summary>
public abstract record CommandPaletteValidation
{
    /// <summary>Gets the result that leaves the session no palette intent to dispatch.</summary>
    public static CommandPaletteValidation NothingToDispatch { get; } = new NoDispatch();

    private protected CommandPaletteValidation()
    {
    }

    private sealed record NoDispatch : CommandPaletteValidation;
}
