using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns one transfer attempt and its canonical receipt while borrowing the admitted original pair. The caller retains this owner before effects, starts it inside the existing I/O boundary, and awaits completion before disposal.</summary>
internal sealed class WindowsOverwriteAttempt : IDisposable
{
    private readonly WindowsOverwritePairLease _pair;
    private readonly TransferOverwriteEvidence _original;
    private readonly WindowsOverwriteDependencies _dependencies;
    private ProviderTargetObservation _target;
    private WindowsTransferPreparationArtifacts? _artifacts;
    private Lifetime _lifetime = new Ready();

    private WindowsOverwriteAttempt(WindowsOverwritePairLease pair, TransferOverwriteEvidence original, WindowsOverwriteDependencies dependencies)
    {
        _pair = pair;
        _original = original;
        _dependencies = dependencies;
        _target = ProviderTargetObservation.Untouched(ObservedTransferEntry.Create(original.Target.Observation.Entry, TransferPathObservation.LastKnownPath));
    }

    /// <summary>Gets one canonical target/artifact receipt on success, failure, cancellation and disposal; completed provider effects remain separate from these internal stages.</summary>
    internal ProviderStepReceipt Receipt => _artifacts?.Snapshot(_target) ?? ProviderStepReceipt.Create([], _target, []);

    internal static WindowsOverwriteAttempt Create(WindowsOverwritePairLease pair, TransferOverwriteEvidence original,
        WindowsOverwriteDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(pair);
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(dependencies);
        return new WindowsOverwriteAttempt(pair, original, dependencies);
    }

    /// <summary>Returns the saved preparation mapping, not a completed copy outcome or permission to bypass final target admission.</summary>
    internal async Task<WindowsTransferRecoveryMapping> PrepareAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        if (_lifetime is not Ready)
        {
            throw new InvalidOperationException("An overwrite preparation may be attempted only once.");
        }
        _lifetime = new Running();
        cancellationToken.ThrowIfCancellationRequested();
        _pair.Revalidate(_original);
        WindowsTransferArtifactNames.RequireCandidateShape(_pair.Target.Path);
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(_pair.Target.Path, _dependencies.Identifiers.NextIdentifier());
        RequireSeparateSource(names);
        _artifacts = WindowsTransferPreparationArtifacts.Create(names, _pair.Target, _dependencies.Artifacts);
        using WindowsTransferSecurityDescriptor security = WindowsTransferSecurityDescriptor.CaptureCurrent();
        cancellationToken.ThrowIfCancellationRequested();
        _artifacts.CreateNative(security, cancellationToken);
        WindowsTransferRecoveryMapping mapping = await PrepareContentAsync(_artifacts, cancellationToken).ConfigureAwait(false);
        VerifiedEntrySnapshot recovery = await WindowsTransferRecoveryWriter.WriteInitialAsync(_artifacts.Recovery, mapping, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        await RequireOriginalBaselinesAsync(_artifacts, mapping, recovery, cancellationToken).ConfigureAwait(false);
        _lifetime = new Prepared(mapping, recovery);
        return mapping;
    }

    /// <summary>Writes prepared bytes through the exact target, retaining uncertainty before every possible effect; this stage does not complete cleanup, copy success or source deletion.</summary>
    internal async Task OverwritePreparedAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        Prepared prepared = _lifetime as Prepared ?? throw new InvalidOperationException("Target content requires one completed, unconsumed preparation.");
        WindowsTransferPreparationArtifacts artifacts = _artifacts ?? throw new InvalidOperationException("Prepared artifact owners are absent.");
        _lifetime = new Overwriting();
        cancellationToken.ThrowIfCancellationRequested();
        await RequireOriginalBaselinesAsync(artifacts, prepared.Mapping, prepared.Recovery, cancellationToken).ConfigureAwait(false);
        WindowsTransferContentPair content = WindowsTransferContentPair.Create(artifacts.Prepared.Handle, _pair.Target.Handle, prepared.Mapping.Desired.Size);
        WindowsTransferContentIo targetIo = new(_dependencies.Content.ReadAsync, WriteTargetAsync, SetTargetLength, _dependencies.Content.Flush);
        await WindowsTransferContentCopy.CopyAsync(content, targetIo, cancellationToken).ConfigureAwait(false);
        await VerifyTargetAsync(prepared, cancellationToken).ConfigureAwait(false);
        _lifetime = new ContentsVerified(prepared);
    }

    /// <summary>Records the successful desired-content observation once; failure keeps every artifact and never enters cleanup.</summary>
    internal async Task AppendCompletionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        ContentsVerified verified = _lifetime as ContentsVerified ?? throw new InvalidOperationException("Completion requires a successfully returned target-verification stage.");
        WindowsTransferPreparationArtifacts artifacts = _artifacts ?? throw new InvalidOperationException("Prepared artifact owners are absent.");
        _lifetime = new Appending();
        InvalidateTargetPath();
        cancellationToken.ThrowIfCancellationRequested();
        VerifiedEntrySnapshot recovery = await WindowsTransferRecoveryWriter.AppendCompletionAsync(artifacts.Recovery, verified.Preparation.Mapping,
            _dependencies.Content, cancellationToken).ConfigureAwait(false);
        _lifetime = new CompletionRecorded(verified.Preparation, recovery);
    }

    /// <summary>Freshly verifies the completed attempt before each owned link removal, with recovery last; partial failure cannot resume and never authorizes source deletion.</summary>
    internal async Task CleanupAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        CompletionRecorded completed = _lifetime as CompletionRecorded ?? throw new InvalidOperationException("Cleanup requires a successfully returned completion stage.");
        WindowsTransferPreparationArtifacts artifacts = _artifacts ?? throw new InvalidOperationException("Prepared artifact owners are absent.");
        _lifetime = new Cleaning();
        InvalidateTargetPath();
        WindowsOverwriteCleanup cleanup = new(artifacts, completed.Preparation.Mapping, completed.Recovery, _dependencies);
        try
        {
            await cleanup.RunAsync(token => VerifyCleanupTargetAsync(completed.Preparation, token), cancellationToken).ConfigureAwait(false);
            ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
            _lifetime = new Cleaned();
        }
        finally
        {
            if (_lifetime is not Cleaned)
            {
                InvalidateTargetPath();
            }
        }
    }

    private async Task VerifyCleanupTargetAsync(Prepared prepared, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        InvalidateTargetPath();
        await VerifyTargetAsync(prepared, cancellationToken).ConfigureAwait(false);
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
    }

    private async ValueTask WriteTargetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> bytes, long offset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginTargetMutation(handle);
        await _dependencies.Content.WriteAsync(handle, bytes, offset, cancellationToken).ConfigureAwait(false);
    }

    private void SetTargetLength(SafeFileHandle handle, long length)
    {
        BeginTargetMutation(handle);
        _dependencies.Content.SetLength(handle, length);
    }

    private void BeginTargetMutation(SafeFileHandle handle)
    {
        if (!ReferenceEquals(handle, _pair.Target.Handle))
        {
            throw new InvalidOperationException("The target writer must use the admitted exact target handle.");
        }
        _target = ProviderTargetObservation.MayHaveChanged(ObservedTransferEntry.Create(_pair.Target.Baseline, TransferPathObservation.LastKnownPath));
    }

    private async Task VerifyTargetAsync(Prepared prepared, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireOriginalSource();
        TransferOverwriteFileObservation before = WindowsOverwriteFileAdmission.ObserveAfterContentChange(_pair.Target);
        if (before.Size != prepared.Mapping.Desired.Size)
        {
            throw new IOException("The exact target length differs from the first desired baseline.");
        }
        WindowsTransferContentPair pair = WindowsTransferContentPair.Create(_pair.Source.Handle, _pair.Target.Handle, prepared.Mapping.Desired.Size);
        FileContentEvidence current = await VerifiedFileContent.CompareHeldAsync(pair, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        if (current != prepared.Mapping.Desired)
        {
            throw new IOException("The target content differs from the first desired baseline.");
        }
        RequireOriginalSource();
        TransferOverwriteFileObservation after = WindowsOverwriteFileAdmission.ObserveAfterContentChange(_pair.Target);
        if (after != before)
        {
            throw new IOException("The exact target facts changed during desired-content verification.");
        }
        _target = ProviderTargetObservation.DesiredVerified(after.Observation, prepared.Mapping.Desired);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private void RequireOriginalSource()
    {
        if (WindowsOverwriteFileAdmission.Observe(_pair.Source) != _original.Source)
        {
            throw new IOException("The original source admission changed after target mutation.");
        }
    }

    private void RequireSeparateSource(WindowsTransferArtifactNames names)
    {
        foreach (WindowsLocalPath candidate in new[] { names.Recovery, names.OriginalBackup, names.PreparedContent })
        {
            if (FileSystemPathIdentityComparer.Instance.Equals(candidate, _pair.Source.Path))
            {
                throw new IOException("An artifact candidate collides with the admitted source.");
            }
        }
    }

    private async Task<WindowsTransferRecoveryMapping> PrepareContentAsync(WindowsTransferPreparationArtifacts artifacts, CancellationToken cancellationToken)
    {
        _pair.Revalidate(_original);
        FileContentEvidence original = await CopyArtifactAsync(WindowsTransferContentPair.Create(_pair.Target.Handle, artifacts.Backup.Handle, _original.Target.Size),
            artifacts.Backup, cancellationToken).ConfigureAwait(false);
        FileContentEvidence desired = await CopyArtifactAsync(WindowsTransferContentPair.Create(_pair.Source.Handle, artifacts.Prepared.Handle, _original.Source.Size),
            artifacts.Prepared, cancellationToken).ConfigureAwait(false);
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryFiles.Create(_original.Source.Observation.Entry, _original.Target.Observation.Entry,
            artifacts.Backup.AdmitBeforeTargetMutation(), artifacts.Prepared.AdmitBeforeTargetMutation());
        return WindowsTransferRecoveryMapping.Create(artifacts.Names.Token, files, original, desired);
    }

    private async Task<FileContentEvidence> CopyArtifactAsync(WindowsTransferContentPair pair, WindowsTransferArtifactOwner owner, CancellationToken cancellationToken)
    {
        owner.InvalidateContent();
        await WindowsTransferContentCopy.CopyAsync(pair, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        FileContentEvidence content = await VerifiedFileContent.CompareHeldAsync(pair, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        owner.RecordVerifiedContent(content);
        _ = owner.AdmitBeforeTargetMutation();
        return content;
    }

    private async Task RequireOriginalBaselinesAsync(WindowsTransferPreparationArtifacts artifacts,
        WindowsTransferRecoveryMapping mapping, VerifiedEntrySnapshot recovery, CancellationToken cancellationToken)
    {
        RequirePreparedIdentities(artifacts, mapping, recovery);
        await RequireContentBaselineAsync(WindowsTransferContentPair.Create(_pair.Target.Handle, artifacts.Backup.Handle, mapping.Original.Size),
            artifacts.Backup, mapping.Original, cancellationToken).ConfigureAwait(false);
        await RequireContentBaselineAsync(WindowsTransferContentPair.Create(_pair.Source.Handle, artifacts.Prepared.Handle, mapping.Desired.Size),
            artifacts.Prepared, mapping.Desired, cancellationToken).ConfigureAwait(false);
        await WindowsTransferRecoveryWriter.RequireInitialAsync(artifacts.Recovery, mapping, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        RequirePreparedIdentities(artifacts, mapping, recovery);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task RequireContentBaselineAsync(WindowsTransferContentPair pair, WindowsTransferArtifactOwner owner,
        FileContentEvidence baseline, CancellationToken cancellationToken)
    {
        owner.InvalidatePathObservation();
        FileContentEvidence current = await VerifiedFileContent.CompareHeldAsync(pair, _dependencies.Content, cancellationToken).ConfigureAwait(false);
        if (current != baseline)
        {
            throw new IOException("The original prepared content baseline changed.");
        }
    }

    private void RequirePreparedIdentities(WindowsTransferPreparationArtifacts artifacts, WindowsTransferRecoveryMapping mapping, VerifiedEntrySnapshot recovery)
    {
        _pair.Revalidate(_original);
        if (artifacts.Backup.AdmitBeforeTargetMutation() != mapping.Files.Backup || artifacts.Prepared.AdmitBeforeTargetMutation() != mapping.Files.Prepared)
        {
            throw new IOException("The prepared artifact identity changed.");
        }
        if (artifacts.Recovery.AdmitBeforeTargetMutation() != recovery)
        {
            throw new IOException("The prepared recovery record identity changed.");
        }
    }

    public void Dispose()
    {
        InvalidateTargetPath();
        _artifacts?.Dispose();
        _lifetime = new Disposed();
    }

    private void InvalidateTargetPath()
    {
        ObservedTransferEntry entry = ObservedTransferEntry.Create(_target.Target.Entry, TransferPathObservation.LastKnownPath);
        _target = _target switch
        {
            UntouchedTransferTarget => ProviderTargetObservation.Untouched(entry),
            TargetMayHaveChanged => ProviderTargetObservation.MayHaveChanged(entry),
            DesiredTargetContentVerified desired => ProviderTargetObservation.DesiredVerified(entry, desired.Content),
            OriginalTargetContentVerified original => ProviderTargetObservation.OriginalVerified(entry, original.Content),
            _ => throw new InvalidOperationException("Unknown canonical target observation."),
        };
    }

    private abstract record Lifetime;
    private sealed record Ready : Lifetime;
    private sealed record Running : Lifetime;
    private sealed record Prepared : Lifetime
    {
        internal Prepared(WindowsTransferRecoveryMapping mapping, VerifiedEntrySnapshot recovery)
        {
            Mapping = mapping;
            Recovery = recovery;
        }

        internal WindowsTransferRecoveryMapping Mapping { get; }
        internal VerifiedEntrySnapshot Recovery { get; }
    }
    private sealed record Overwriting : Lifetime;
    private sealed record ContentsVerified : Lifetime
    {
        internal ContentsVerified(Prepared preparation)
        {
            Preparation = preparation;
        }

        internal Prepared Preparation { get; }
    }
    private sealed record Appending : Lifetime;
    private sealed record CompletionRecorded : Lifetime
    {
        internal CompletionRecorded(Prepared preparation, VerifiedEntrySnapshot recovery)
        {
            Preparation = preparation;
            Recovery = recovery;
        }

        internal Prepared Preparation { get; }
        internal VerifiedEntrySnapshot Recovery { get; }
    }
    private sealed record Cleaning : Lifetime;
    private sealed record Cleaned : Lifetime;
    private sealed record Disposed : Lifetime;
}
