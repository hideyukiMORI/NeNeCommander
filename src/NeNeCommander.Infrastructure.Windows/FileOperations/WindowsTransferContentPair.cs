using System;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Borrows two live handles and an expected byte count; identity, permissions and lifetime remain the enclosing attempt's responsibility.</summary>
internal sealed class WindowsTransferContentPair
{
    private WindowsTransferContentPair(SafeFileHandle source, SafeFileHandle target, KnownEntrySize size)
    {
        Source = source;
        Target = target;
        Size = size;
    }

    internal SafeFileHandle Source { get; }
    internal SafeFileHandle Target { get; }
    internal KnownEntrySize Size { get; }

    internal static WindowsTransferContentPair Create(SafeFileHandle source, SafeFileHandle target, KnownEntrySize size)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(size);
        return ReferenceEquals(source, target)
            ? throw new ArgumentException("A transfer content pair requires separate borrowed handles.", nameof(target))
            : new WindowsTransferContentPair(source, target, size);
    }
}
