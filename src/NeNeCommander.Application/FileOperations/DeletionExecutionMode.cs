namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents the closed deletion instruction passed to the provider port.
/// </summary>
public abstract record DeletionExecutionMode
{
    /// <summary>Gets the instruction that requires provider-guaranteed recycling.</summary>
    public static DeletionExecutionMode Recycle { get; } = new RecycleMode();

    /// <summary>Gets the instruction for explicitly confirmed permanent deletion.</summary>
    public static DeletionExecutionMode Permanent { get; } = new PermanentMode();

    /// <summary>Creates the distinct instruction that consumes verified-copy evidence.</summary>
    public static DeletionExecutionMode VerifiedCopy(VerifiedCopyReceipt receipt)
    {
        System.ArgumentNullException.ThrowIfNull(receipt);
        return new VerifiedCopyMode(receipt);
    }

    private DeletionExecutionMode()
    {
    }

    private sealed record RecycleMode : DeletionExecutionMode;
    private sealed record PermanentMode : DeletionExecutionMode;

    /// <summary>Requires source-provider deletion admission against the attached immutable tree.</summary>
    public sealed record VerifiedCopyMode : DeletionExecutionMode
    {
        internal VerifiedCopyMode(VerifiedCopyReceipt receipt)
        {
            Receipt = receipt;
        }

        /// <summary>Gets the evidence to revalidate before every deletion effect.</summary>
        public VerifiedCopyReceipt Receipt { get; }
    }
}
