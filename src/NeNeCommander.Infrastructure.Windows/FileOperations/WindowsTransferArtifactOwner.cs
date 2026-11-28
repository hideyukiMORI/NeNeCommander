using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Owns one create-new attempt and monotonically retains learned identities; one-shot removal requires fresh admission and never reopens or retries an artifact. The caller retains and outlives the borrowed parent lease.</summary>
internal sealed class WindowsTransferArtifactOwner : IDisposable
{
    private readonly TransferArtifactRole _role;
    private readonly WindowsLocalPath _candidate;
    private readonly WindowsTransferPathLease _parentOwner;
    private readonly WindowsTransferArtifactQueries _queries;
    private Lifetime _lifetime = new Ready();
    private SafeFileHandle? _handle;

    private WindowsTransferArtifactOwner(TransferArtifactRole role, WindowsLocalPath candidate,
        WindowsTransferPathLease parentOwner, WindowsTransferArtifactQueries queries)
    {
        _role = role;
        _candidate = candidate;
        _parentOwner = parentOwner;
        _queries = queries;
    }

    /// <summary>Gets absence only before an attempt; the snapshot remains readable after failures and disposal.</summary>
    internal TransferArtifactReceipt? Receipt { get; private set; }

    internal SafeFileHandle Handle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
            return _lifetime is RemovalAttempted
                ? throw new InvalidOperationException("A removal attempt consumes access to the owned artifact handle.")
                : _handle ?? throw new InvalidOperationException("No artifact handle was acquired.");
        }
    }

    internal static WindowsTransferArtifactOwner Prepare(TransferArtifactRole role, WindowsLocalPath candidate,
        WindowsTransferPathLease parentOwner, WindowsTransferArtifactQueries queries)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(parentOwner);
        ArgumentNullException.ThrowIfNull(queries);
        return candidate.Parent != parentOwner.ParentPath
            ? throw new ArgumentException("An artifact must belong to its retained parent's exact path.", nameof(candidate))
            : new WindowsTransferArtifactOwner(role, candidate, parentOwner, queries);
    }

    /// <summary>Registers possible creation before the sole native call; a returned handle is retained before any fallible query.</summary>
    internal void CreateNative(WindowsTransferSecurityDescriptor security)
    {
        ArgumentNullException.ThrowIfNull(security);
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        if (_lifetime is not Ready)
        {
            throw new InvalidOperationException("An artifact creation may be attempted only once.");
        }
        Receipt = TransferArtifactReceipt.Unidentified(_role, _candidate);
        _lifetime = new Attempted();
        _handle = WindowsTransferEntryOpen.CreateArtifact(_parentOwner, _candidate, security);
        ObserveBeforeTargetMutation();
    }

    /// <summary>Refreshes descriptive observations before target effects; parent validation still requires the original anchor file.</summary>
    internal void ObserveBeforeTargetMutation()
    {
        _ = CaptureFacts();
        RequireCurrentPath(_parentOwner.Revalidate);
        RecordCurrentPath();
    }

    /// <summary>Admits current artifact shape and stream membership after its own writes, preserving stable identity rather than the old full metadata.</summary>
    internal VerifiedEntrySnapshot AdmitBeforeTargetMutation()
    {
        return Admit(_parentOwner.Revalidate);
    }

    /// <summary>Uses the same retained parent chain after target content changed, without requiring the target's old full metadata or weakening artifact admission.</summary>
    internal VerifiedEntrySnapshot AdmitAfterTargetMutation()
    {
        return Admit(_parentOwner.RevalidateObject);
    }

    private VerifiedEntrySnapshot Admit(Action revalidateParent)
    {
        WindowsLocalHandleFacts before = CaptureFacts();
        WindowsOverwriteFileAdmission.RequireShape(before);
        RequireCurrentPath(revalidateParent);
        WindowsFileIdentifier.RequireUnnamedStream(Handle, before.Standard.EndOfFile);
        WindowsLocalHandleFacts after = CaptureFacts();
        if (before != after)
        {
            throw new IOException("The artifact facts changed during stream observation.");
        }
        RecordCurrentPath();
        return WindowsLocalEntryIdentity.CaptureHeld(_candidate, after);
    }

    /// <summary>Consumes one freshly admitted owned link, recording uncertainty before native entry and removal only after acknowledged close. The enclosing attempt must await all I/O and verify current content first.</summary>
    internal void RemoveLink(VerifiedEntrySnapshot expected, WindowsTransferArtifactRemoval removal)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(removal);
        ObjectDisposedException.ThrowIf(_lifetime is Disposed, this);
        if (_lifetime is not Attempted)
        {
            throw new InvalidOperationException("An owned artifact removal may be attempted only once.");
        }
        _lifetime = new AdmittingRemoval();
        try
        {
            if (AdmitAfterTargetMutation() != expected)
            {
                throw new IOException("The owned artifact no longer matches its saved cleanup identity.");
            }
            SafeFileHandle handle = Handle;
            _lifetime = new RemovalAttempted();
            InvalidatePathObservation();
            RecordLink(ArtifactLinkObservation.Uncertain);
            removal.Remove(handle);
            RecordLink(ArtifactLinkObservation.Removed);
        }
        finally
        {
            if (_lifetime is not Disposed)
            {
                _lifetime = new RemovalAttempted();
            }
            InvalidatePathObservation();
        }
    }

    private void RecordLink(ArtifactLinkObservation link)
    {
        IdentifiedTransferArtifact known = RequireIdentified();
        Receipt = TransferArtifactReceipt.Identified(_role, known.Artifact, known.Content, link);
    }

    /// <summary>Marks unverified content before a possible write; an established content baseline or mapping cannot be rewritten.</summary>
    internal void InvalidateContent()
    {
        _ = Handle;
        if (RequireIdentified().Content is VerifiedArtifactContent or RecoveryMappingContent)
        {
            throw new InvalidOperationException("An established artifact content baseline cannot be rewritten.");
        }
        InvalidatePathObservation();
    }

    /// <summary>Retains completed byte verification immediately, before further fallible metadata observations.</summary>
    internal void RecordVerifiedContent(FileContentEvidence content)
    {
        _ = Handle;
        if (RequireIdentified().Content is VerifiedArtifactContent)
        {
            throw new InvalidOperationException("The original artifact content baseline is already established.");
        }
        RecordContent(ArtifactContentObservation.Verified(content));
    }

    /// <summary>Records a complete initial frame once, without claiming that a flush has occurred.</summary>
    internal void RecordInitialFrame(RecoveryFrameEvidence frame)
    {
        _ = Handle;
        if (RequireIdentified().Content is RecoveryMappingContent)
        {
            throw new InvalidOperationException("The initial recovery frame is already established.");
        }
        RecordContent(ArtifactContentObservation.RecoveryMapping(
            RecoveryFrameObservation.Create(frame, RecoveryFlushObservation.NotAttempted), RecoveryAppendObservation.NotAttempted));
    }

    /// <summary>Retains flush uncertainty before the native call; no byte evidence is discarded on failure.</summary>
    internal void BeginInitialFlush()
    {
        AdvanceInitialFlush(RecoveryFlushObservation.NotAttempted, RecoveryFlushObservation.OutcomeUnconfirmed);
    }

    /// <summary>Records native acknowledgement before the caller observes cancellation; this is not hardware durability.</summary>
    internal void ConfirmInitialFlush()
    {
        AdvanceInitialFlush(RecoveryFlushObservation.OutcomeUnconfirmed, RecoveryFlushObservation.Confirmed);
    }

    /// <summary>Records append uncertainty before its first possible write, preserving the original confirmed mapping.</summary>
    internal void BeginCompletionAppend()
    {
        RecoveryMappingContent mapping = RequireConfirmedInitial();
        if (mapping.Append != RecoveryAppendObservation.NotAttempted)
        {
            throw new InvalidOperationException("A completion append may be attempted only once.");
        }
        InvalidatePathObservation();
        RecordContent(ArtifactContentObservation.RecoveryMapping(mapping.InitialFrame, RecoveryAppendObservation.OutcomeUnconfirmed));
    }

    /// <summary>Retains the completely read-back bound completion frame before later metadata, flush or cancellation failure.</summary>
    internal void RecordCompletionFrame(RecoveryFrameEvidence frame)
    {
        RecoveryMappingContent mapping = RequireConfirmedInitial();
        if (mapping.Append != RecoveryAppendObservation.OutcomeUnconfirmed)
        {
            throw new InvalidOperationException("Completion verification requires one pending append attempt.");
        }
        RecordContent(ArtifactContentObservation.RecoveryMapping(mapping.InitialFrame,
            RecoveryAppendObservation.Verified(RecoveryFrameObservation.Create(frame, RecoveryFlushObservation.NotAttempted))));
    }

    /// <summary>Retains independent completion-flush uncertainty before the native call.</summary>
    internal void BeginCompletionFlush()
    {
        AdvanceCompletionFlush(RecoveryFlushObservation.NotAttempted, RecoveryFlushObservation.OutcomeUnconfirmed);
    }

    /// <summary>Retains native completion-flush acknowledgement without claiming hardware durability.</summary>
    internal void ConfirmCompletionFlush()
    {
        AdvanceCompletionFlush(RecoveryFlushObservation.OutcomeUnconfirmed, RecoveryFlushObservation.Confirmed);
    }

    private void AdvanceCompletionFlush(RecoveryFlushObservation expected, RecoveryFlushObservation next)
    {
        RecoveryMappingContent mapping = RequireConfirmedInitial();
        if (mapping.Append is not VerifiedRecoveryAppend append || append.Frame.Flush != expected)
        {
            throw new InvalidOperationException("The completion recovery flush transition is out of order.");
        }
        RecordContent(ArtifactContentObservation.RecoveryMapping(mapping.InitialFrame,
            RecoveryAppendObservation.Verified(RecoveryFrameObservation.Create(append.Frame.Frame, next))));
    }

    private RecoveryMappingContent RequireConfirmedInitial()
    {
        _ = Handle;
        return RequireIdentified().Content is RecoveryMappingContent mapping && mapping.InitialFrame.Flush == RecoveryFlushObservation.Confirmed
            ? mapping : throw new InvalidOperationException("A completion requires the original confirmed mapping.");
    }

    private void AdvanceInitialFlush(RecoveryFlushObservation expected, RecoveryFlushObservation next)
    {
        _ = Handle;
        if (RequireIdentified().Content is not RecoveryMappingContent mapping || mapping.InitialFrame.Flush != expected)
        {
            throw new InvalidOperationException("The initial recovery flush transition is out of order.");
        }
        RecordContent(ArtifactContentObservation.RecoveryMapping(RecoveryFrameObservation.Create(mapping.InitialFrame.Frame, next), mapping.Append));
    }

    private void RecordContent(ArtifactContentObservation content)
    {
        IdentifiedTransferArtifact known = RequireIdentified();
        Receipt = TransferArtifactReceipt.Identified(_role, known.Artifact, content, known.Link);
    }

    private WindowsLocalHandleFacts CaptureFacts()
    {
        SafeFileHandle handle = Handle;
        InvalidatePathObservation();
        FileObjectIdentity identity = _queries.ReadIdentity(handle);
        RecordStableIdentity(identity);
        WindowsLocalHandleFacts facts = _queries.ReadFacts(handle);
        if (facts.ObjectIdentity != identity)
        {
            throw new IOException("The artifact metadata describes a different object.");
        }
        FileIdentity full = WindowsLocalEntryIdentity.CaptureHeld(_candidate, facts).Identity;
        RecordObservation(ArtifactFullIdentityObservation.Observed(full), TransferPathObservation.LastKnownPath);
        return facts;
    }

    private void RequireCurrentPath(Action revalidateParent)
    {
        revalidateParent();
        _queries.RequirePath(Handle, _candidate);
    }

    private void RecordCurrentPath()
    {
        RecordObservation(RequireIdentified().Artifact.FullIdentity, TransferPathObservation.CurrentPathVerified);
    }

    private void RecordStableIdentity(FileObjectIdentity identity)
    {
        if (Receipt is IdentifiedTransferArtifact known)
        {
            if (known.Artifact.ObjectIdentity != identity)
            {
                throw new IOException("The artifact stable identity changed.");
            }
            return;
        }
        TransferArtifactEntry entry = TransferArtifactEntry.Create(_candidate, identity,
            ArtifactFullIdentityObservation.NotObserved, TransferPathObservation.LastKnownPath);
        Receipt = TransferArtifactReceipt.Identified(_role, entry,
            ArtifactContentObservation.Unknown, ArtifactLinkObservation.Retained);
    }

    private IdentifiedTransferArtifact RequireIdentified()
    {
        return Receipt as IdentifiedTransferArtifact ?? throw new InvalidOperationException("No artifact identity has been observed.");
    }

    private void RecordObservation(ArtifactFullIdentityObservation full, TransferPathObservation path)
    {
        IdentifiedTransferArtifact known = RequireIdentified();
        TransferArtifactEntry entry = TransferArtifactEntry.Create(_candidate, known.Artifact.ObjectIdentity, full, path);
        Receipt = TransferArtifactReceipt.Identified(_role, entry, known.Content, known.Link);
    }

    /// <summary>Removes current-path qualification while retaining all previously learned identity and content evidence.</summary>
    internal void InvalidatePathObservation()
    {
        if (Receipt is IdentifiedTransferArtifact known)
        {
            RecordObservation(known.Artifact.FullIdentity, TransferPathObservation.LastKnownPath);
        }
    }

    public void Dispose()
    {
        InvalidatePathObservation();
        _lifetime = new Disposed();
        _handle?.Dispose();
    }

    private abstract record Lifetime;
    private sealed record Ready : Lifetime;
    private sealed record Attempted : Lifetime;
    private sealed record AdmittingRemoval : Lifetime;
    private sealed record RemovalAttempted : Lifetime;
    private sealed record Disposed : Lifetime;
}
