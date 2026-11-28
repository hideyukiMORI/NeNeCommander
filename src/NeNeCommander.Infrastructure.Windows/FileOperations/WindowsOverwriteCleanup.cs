using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Revalidates the existing attempt's remaining artifacts before each held-link removal. It borrows the owners and records no parallel receipt; the caller owns one-shot lifetime and awaits all I/O.</summary>
internal sealed class WindowsOverwriteCleanup
{
    private readonly WindowsTransferPreparationArtifacts _artifacts;
    private readonly WindowsTransferRecoveryMapping _mapping;
    private readonly VerifiedEntrySnapshot _recovery;
    private readonly WindowsOverwriteDependencies _dependencies;

    internal WindowsOverwriteCleanup(WindowsTransferPreparationArtifacts artifacts, WindowsTransferRecoveryMapping mapping,
        VerifiedEntrySnapshot recovery, WindowsOverwriteDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(recovery);
        ArgumentNullException.ThrowIfNull(dependencies);
        _artifacts = artifacts;
        _mapping = mapping;
        _recovery = recovery;
        _dependencies = dependencies;
    }

    internal async Task RunAsync(Func<CancellationToken, Task> requireDesired, CancellationToken cancellationToken)
    {
        try
        {
            foreach (WindowsTransferArtifactOwner owner in new[] { _artifacts.Prepared, _artifacts.Backup, _artifacts.Recovery })
            {
                InvalidatePaths();
                await requireDesired(cancellationToken).ConfigureAwait(false);
                RequireRemainingIdentities();
                await RequireRemainingContentAsync(cancellationToken).ConfigureAwait(false);
                await requireDesired(cancellationToken).ConfigureAwait(false);
                RequireRemainingIdentities();
                cancellationToken.ThrowIfCancellationRequested();
                owner.RemoveLink(Expected(owner), _dependencies.Removal);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            InvalidatePaths();
        }
    }

    private IEnumerable<WindowsTransferArtifactOwner> Remaining()
    {
        foreach (WindowsTransferArtifactOwner owner in new[] { _artifacts.Prepared, _artifacts.Backup, _artifacts.Recovery })
        {
            if (owner.Receipt is not IdentifiedTransferArtifact known || known.Link == ArtifactLinkObservation.Uncertain)
            {
                throw new IOException("Cleanup requires identified, unconsumed owned artifact links.");
            }
            if (known.Link == ArtifactLinkObservation.Retained)
            {
                yield return owner;
            }
        }
    }

    private void RequireRemainingIdentities()
    {
        foreach (WindowsTransferArtifactOwner owner in Remaining())
        {
            if (owner.AdmitAfterTargetMutation() != Expected(owner))
            {
                throw new IOException("A remaining artifact differs from its first cleanup identity.");
            }
        }
    }

    private async Task RequireRemainingContentAsync(CancellationToken cancellationToken)
    {
        foreach (WindowsTransferArtifactOwner owner in Remaining())
        {
            owner.InvalidatePathObservation();
            if (ReferenceEquals(owner, _artifacts.Recovery))
            {
                await WindowsTransferRecoveryWriter.RequireCompletedAsync(owner, _mapping, _dependencies.Content, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await RequireArtifactContentAsync(owner, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RequireArtifactContentAsync(WindowsTransferArtifactOwner owner, CancellationToken cancellationToken)
    {
        FileContentEvidence expected = ReferenceEquals(owner, _artifacts.Backup) ? _mapping.Original : _mapping.Desired;
        if (owner.Receipt is not IdentifiedTransferArtifact { Content: VerifiedArtifactContent saved } || saved.Content != expected)
        {
            throw new IOException("The original artifact content evidence is unavailable.");
        }
        FileContentEvidence current = await VerifiedFileContent.ReadHeldAsync(owner.Handle, expected.Size, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        if (current != expected)
        {
            throw new IOException("A remaining artifact differs from its first content baseline.");
        }
    }

    private VerifiedEntrySnapshot Expected(WindowsTransferArtifactOwner owner)
    {
        return ReferenceEquals(owner, _artifacts.Backup) ? _mapping.Files.Backup
            : ReferenceEquals(owner, _artifacts.Prepared) ? _mapping.Files.Prepared : _recovery;
    }

    private void InvalidatePaths()
    {
        _artifacts.Prepared.InvalidatePathObservation();
        _artifacts.Backup.InvalidatePathObservation();
        _artifacts.Recovery.InvalidatePathObservation();
    }
}
