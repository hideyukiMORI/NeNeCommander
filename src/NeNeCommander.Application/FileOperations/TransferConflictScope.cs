namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies whether a conflict decision addresses one conflict or all visible conflicts.</summary>
public abstract record TransferConflictScope
{
    /// <summary>Gets the scope for only the first unresolved conflict.</summary>
    public static TransferConflictScope Current { get; } = new CurrentScope();
    /// <summary>Gets the scope for every conflict currently shown.</summary>
    public static TransferConflictScope All { get; } = new AllScope();

    private TransferConflictScope()
    {
    }

    private sealed record CurrentScope : TransferConflictScope;
    private sealed record AllScope : TransferConflictScope;
}
