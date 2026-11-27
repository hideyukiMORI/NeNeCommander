using System;
using System.IO;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Enriches one native admission refusal until the adapter translates it into the canonical capability.</summary>
internal sealed class WindowsOverwriteRefusalException : IOException
{
    internal WindowsOverwriteRefusalException(TransferOverwriteUnavailableReason reason, Exception? cause)
        : base("The observed overwrite prerequisite is unavailable.", cause)
    {
        ArgumentNullException.ThrowIfNull(reason);
        Reason = reason;
    }

    internal TransferOverwriteUnavailableReason Reason { get; }
}
