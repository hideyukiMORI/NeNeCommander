using System;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Carries the seams one live WSL root owner uses for every identity, setup, evidence, and cleanup
/// step: the production WSL file system, the same namespace mapping it was composed with, and the
/// link fixture creation and no-follow removal (ADR-0043, ADR-0049).
/// </summary>
internal sealed record LiveWslRootMechanism
{
    private LiveWslRootMechanism(
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath,
        Action<WslPath, WslPath> createLink,
        Action<WslPath> unlinkEntry)
    {
        FileSystem = fileSystem;
        ResolvePath = resolvePath;
        CreateLink = createLink;
        UnlinkEntry = unlinkEntry;
    }

    /// <summary>Gets the production WSL file system that owns every identity.</summary>
    internal WindowsWslFileSystem FileSystem { get; }

    /// <summary>Gets the namespace mapping used for setup, evidence, and cleanup.</summary>
    internal Func<WslPath, string> ResolvePath { get; }

    /// <summary>Gets the seam that creates one link fixture from its target and link path.</summary>
    internal Action<WslPath, WslPath> CreateLink { get; }

    /// <summary>Gets the seam that removes one link entry without following it.</summary>
    internal Action<WslPath> UnlinkEntry { get; }

    /// <summary>Creates the seams of one live root owner.</summary>
    /// <param name="fileSystem">Production WSL file system that owns every identity.</param>
    /// <param name="resolvePath">Namespace mapping that <paramref name="fileSystem"/> was composed with.</param>
    /// <param name="createLink">Creates one link fixture from its target and link path.</param>
    /// <param name="unlinkEntry">Removes one link entry without following it.</param>
    /// <returns>The seams.</returns>
    internal static LiveWslRootMechanism Create(
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath,
        Action<WslPath, WslPath> createLink,
        Action<WslPath> unlinkEntry)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(resolvePath);
        ArgumentNullException.ThrowIfNull(createLink);
        ArgumentNullException.ThrowIfNull(unlinkEntry);
        return new LiveWslRootMechanism(fileSystem, resolvePath, createLink, unlinkEntry);
    }
}
