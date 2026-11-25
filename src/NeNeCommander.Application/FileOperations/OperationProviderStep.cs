using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Binds a provider receipt to the source or creation target whose operation produced it.</summary>
public sealed record OperationProviderStep
{
    private OperationProviderStep(FileSystemPath source, ProviderStepReceipt receipt)
    {
        Source = source;
        Receipt = receipt;
    }

    /// <summary>Gets the original source, or the requested target for an entry creation.</summary>
    public FileSystemPath Source { get; }

    /// <summary>Gets the exact immutable provider receipt.</summary>
    public ProviderStepReceipt Receipt { get; }

    internal static OperationProviderStep Create(FileSystemPath source, ProviderStepReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(receipt);
        return new OperationProviderStep(source, receipt);
    }
}
