namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents the closed delete behavior reported by a provider for one entry.
/// </summary>
public abstract record DeletionCapability
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the capability that guarantees recycle semantics.</summary>
    public static DeletionCapability Recycle { get; } = new RecycleCapability();

    /// <summary>Gets the capability that permits only permanent deletion.</summary>
    public static DeletionCapability PermanentOnly { get; } = new PermanentOnlyCapability();

    private DeletionCapability()
    {
    }

    private sealed record RecycleCapability : DeletionCapability
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record PermanentOnlyCapability : DeletionCapability
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
