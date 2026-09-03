namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Represents the closed result of checking a preflighted snapshot against the entry as it exists now.
/// </summary>
public abstract record RevalidationOutcome
{
    private protected RevalidationOutcome()
    {
    }
}
