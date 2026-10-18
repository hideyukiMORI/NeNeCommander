namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies whether one planned source is transferred or deliberately left in place.</summary>
public abstract record TransferDisposition
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the disposition that performs copy or move.</summary>
    public static TransferDisposition Transfer { get; } = new TransferEntryDisposition();
    /// <summary>Gets the disposition that leaves the source untransferred.</summary>
    public static TransferDisposition Skip { get; } = new SkipEntryDisposition();

    private TransferDisposition()
    {
    }

    private sealed record TransferEntryDisposition : TransferDisposition
    {
        private protected override void RestrictDerivation()
        {
        }
    }
    private sealed record SkipEntryDisposition : TransferDisposition
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
