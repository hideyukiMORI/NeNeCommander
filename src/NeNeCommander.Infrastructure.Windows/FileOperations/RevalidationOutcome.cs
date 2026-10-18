namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Represents the closed result of checking a preflighted snapshot against the entry as it exists now.
/// </summary>
public abstract record RevalidationOutcome
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected RevalidationOutcome()
    {
    }
}
