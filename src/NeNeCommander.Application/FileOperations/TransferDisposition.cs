namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies whether one planned source is transferred or deliberately left in place.</summary>
public abstract record TransferDisposition
{
    /// <summary>Gets the disposition that performs copy or move.</summary>
    public static TransferDisposition Transfer { get; } = new TransferEntryDisposition();
    /// <summary>Gets the disposition that leaves the source untransferred.</summary>
    public static TransferDisposition Skip { get; } = new SkipEntryDisposition();

    private TransferDisposition()
    {
    }

    private sealed record TransferEntryDisposition : TransferDisposition;
    private sealed record SkipEntryDisposition : TransferDisposition;
}
