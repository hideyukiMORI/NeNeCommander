namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies whether a conflict decision addresses one conflict or all visible conflicts.</summary>
public abstract record TransferConflictScope
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the scope for only the first unresolved conflict.</summary>
    public static TransferConflictScope Current { get; } = new CurrentScope();
    /// <summary>Gets the scope for every conflict currently shown.</summary>
    public static TransferConflictScope All { get; } = new AllScope();

    private TransferConflictScope()
    {
    }

    private sealed record CurrentScope : TransferConflictScope
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record AllScope : TransferConflictScope
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
