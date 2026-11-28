using System;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Keeps the three held-artifact observations on their existing native owners, with deterministic adapter-test injection.</summary>
internal sealed class WindowsTransferArtifactQueries
{
    internal WindowsTransferArtifactQueries(
        Func<SafeFileHandle, FileObjectIdentity> readIdentity,
        Func<SafeFileHandle, WindowsLocalHandleFacts> readFacts,
        Action<SafeFileHandle, WindowsLocalPath> requirePath)
    {
        ArgumentNullException.ThrowIfNull(readIdentity);
        ArgumentNullException.ThrowIfNull(readFacts);
        ArgumentNullException.ThrowIfNull(requirePath);
        ReadIdentity = readIdentity;
        ReadFacts = readFacts;
        RequirePath = requirePath;
    }

    internal static WindowsTransferArtifactQueries Native { get; } = new(
        handle => FileObjectIdentity.Create(WindowsFileIdentifier.DescribeHandle(handle)),
        WindowsFileIdentifier.ReadLocalFacts, WindowsTransferPathObservation.Require);

    internal Func<SafeFileHandle, FileObjectIdentity> ReadIdentity { get; }
    internal Func<SafeFileHandle, WindowsLocalHandleFacts> ReadFacts { get; }
    internal Action<SafeFileHandle, WindowsLocalPath> RequirePath { get; }
}
