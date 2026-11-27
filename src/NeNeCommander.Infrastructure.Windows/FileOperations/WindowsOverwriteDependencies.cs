using System;
using NeNeCommander.Application.Identifiers;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Supplies the existing identifier, held-content and artifact-query boundaries to one transfer attempt.</summary>
internal sealed class WindowsOverwriteDependencies
{
    internal WindowsOverwriteDependencies(IIdentifierSource identifiers, WindowsTransferContentIo content, WindowsTransferArtifactQueries artifacts, WindowsTransferArtifactRemoval removal)
    {
        ArgumentNullException.ThrowIfNull(identifiers);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(removal);
        Identifiers = identifiers;
        Content = content;
        Artifacts = artifacts;
        Removal = removal;
    }

    internal static WindowsOverwriteDependencies Native(IIdentifierSource identifiers)
    {
        return new WindowsOverwriteDependencies(identifiers, WindowsTransferContentIo.Native,
            WindowsTransferArtifactQueries.Native, WindowsTransferArtifactRemoval.Native);
    }

    internal IIdentifierSource Identifiers { get; }
    internal WindowsTransferContentIo Content { get; }
    internal WindowsTransferArtifactQueries Artifacts { get; }
    internal WindowsTransferArtifactRemoval Removal { get; }
}
