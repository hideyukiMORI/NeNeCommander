using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Retains same-handle native observations; path ownership, local-drive admission and metadata stability remain separate.</summary>
internal sealed record WindowsLocalHandleFacts
{
    internal WindowsLocalHandleFacts(
        FileObjectIdentity objectIdentity,
        WindowsLocalBasicFacts basic,
        WindowsLocalStandardFacts standard,
        string fileSystemName)
    {
        ArgumentNullException.ThrowIfNull(objectIdentity);
        ArgumentNullException.ThrowIfNull(basic);
        ArgumentNullException.ThrowIfNull(standard);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileSystemName);
        ObjectIdentity = objectIdentity;
        Basic = basic;
        Standard = standard;
        FileSystemName = fileSystemName;
    }

    internal FileObjectIdentity ObjectIdentity { get; }
    internal WindowsLocalBasicFacts Basic { get; }
    internal WindowsLocalStandardFacts Standard { get; }
    internal string FileSystemName { get; }
}
