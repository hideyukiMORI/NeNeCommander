using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Writes one initial mapping and its bound append through the same owned record, preserving independent byte and flush evidence.</summary>
internal static class WindowsTransferRecoveryWriter
{
    internal static async Task<VerifiedEntrySnapshot> WriteInitialAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(io);
        cancellationToken.ThrowIfCancellationRequested();
        RequireRecoveryOwner(owner, mapping, owner.AdmitBeforeTargetMutation);
        byte[] bytes = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        RecoveryFrameEvidence frame = WindowsTransferRecoveryCodec.Inspect(bytes).Initial;
        owner.InvalidateContent();
        await WriteBytesAsync(owner, bytes, io, cancellationToken).ConfigureAwait(false);
        await RequireBytesAsync(owner, bytes, io, cancellationToken).ConfigureAwait(false);
        owner.RecordInitialFrame(frame);
        _ = owner.AdmitBeforeTargetMutation();
        cancellationToken.ThrowIfCancellationRequested();
        owner.BeginInitialFlush();
        io.Flush(owner.Handle);
        owner.ConfirmInitialFlush();
        cancellationToken.ThrowIfCancellationRequested();
        return owner.AdmitBeforeTargetMutation();
    }

    /// <summary>Rechecks the complete saved initial frame without replacing its original byte or flush evidence.</summary>
    internal static async Task RequireInitialAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireRecoveryOwner(owner, mapping, owner.AdmitBeforeTargetMutation);
        byte[] bytes = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        await RequireConfirmedInitialAsync(owner, bytes, io, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Appends after exact initial-frame readmission; no call truncates, rewrites or substitutes the initial mapping.</summary>
    internal static async Task<VerifiedEntrySnapshot> AppendCompletionAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(io);
        cancellationToken.ThrowIfCancellationRequested();
        RequireRecoveryOwner(owner, mapping, owner.AdmitAfterTargetMutation);
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] completion = WindowsTransferRecoveryCodec.EncodeCompletion(initial);
        byte[] expected = [.. initial, .. completion];
        WindowsTransferRecoveryCompleted decoded = (WindowsTransferRecoveryCompleted)WindowsTransferRecoveryCodec.Inspect(expected);
        await RequireConfirmedInitialAsync(owner, initial, io, cancellationToken).ConfigureAwait(false);
        _ = owner.AdmitAfterTargetMutation();
        cancellationToken.ThrowIfCancellationRequested();
        await WriteCompletionBytesAsync(owner, completion, io, cancellationToken).ConfigureAwait(false);
        await RequireBytesAsync(owner, expected, io, cancellationToken).ConfigureAwait(false);
        owner.RecordCompletionFrame(decoded.Completion);
        _ = owner.AdmitAfterTargetMutation();
        cancellationToken.ThrowIfCancellationRequested();
        owner.BeginCompletionFlush();
        io.Flush(owner.Handle);
        owner.ConfirmCompletionFlush();
        cancellationToken.ThrowIfCancellationRequested();
        VerifiedEntrySnapshot completed = owner.AdmitAfterTargetMutation();
        cancellationToken.ThrowIfCancellationRequested();
        return completed;
    }

    /// <summary>Rechecks both exact saved frames and independent flush acknowledgements without replacing historical evidence.</summary>
    internal static async Task RequireCompletedAsync(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireRecoveryOwner(owner, mapping, owner.AdmitAfterTargetMutation);
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);
        byte[] expected = [.. initial, .. WindowsTransferRecoveryCodec.EncodeCompletion(initial)];
        WindowsTransferRecoveryCompleted decoded = (WindowsTransferRecoveryCompleted)WindowsTransferRecoveryCodec.Inspect(expected);
        if (owner.Receipt is not IdentifiedTransferArtifact { Content: RecoveryMappingContent saved } ||
            saved.InitialFrame.Frame != decoded.Initial || saved.InitialFrame.Flush != RecoveryFlushObservation.Confirmed ||
            saved.Append is not VerifiedRecoveryAppend append || append.Frame.Frame != decoded.Completion ||
            append.Frame.Flush != RecoveryFlushObservation.Confirmed)
        {
            throw new IOException("Cleanup requires both original bound recovery frames and confirmed flushes.");
        }
        owner.InvalidatePathObservation();
        await RequireBytesAsync(owner, expected, io, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RequireConfirmedInitialAsync(WindowsTransferArtifactOwner owner, ReadOnlyMemory<byte> bytes,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        RecoveryFrameEvidence expected = WindowsTransferRecoveryCodec.Inspect(bytes).Initial;
        if (owner.Receipt is not IdentifiedTransferArtifact { Content: RecoveryMappingContent saved } ||
            saved.InitialFrame.Frame != expected || saved.InitialFrame.Flush != RecoveryFlushObservation.Confirmed ||
            saved.Append != RecoveryAppendObservation.NotAttempted)
        {
            throw new IOException("The prepared recovery frame is not the original confirmed mapping.");
        }
        owner.InvalidatePathObservation();
        await RequireBytesAsync(owner, bytes, io, cancellationToken).ConfigureAwait(false);
    }

    private static void RequireRecoveryOwner(WindowsTransferArtifactOwner owner, WindowsTransferRecoveryMapping mapping, Func<VerifiedEntrySnapshot> admit)
    {
        if (owner.Receipt is not IdentifiedTransferArtifact receipt || receipt.Role != TransferArtifactRole.RecoveryRecord ||
            admit().Path != mapping.Names.Recovery)
        {
            throw new IOException("The recovery writer requires its admitted exact record owner.");
        }
    }

    private static async Task WriteCompletionBytesAsync(WindowsTransferArtifactOwner owner, ReadOnlyMemory<byte> bytes,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        if (owner.Receipt is not IdentifiedTransferArtifact { Content: RecoveryMappingContent mapping })
        {
            throw new InvalidOperationException("Completion writing requires the established recovery mapping.");
        }
        long start = mapping.InitialFrame.Frame.Size.Bytes;
        cancellationToken.ThrowIfCancellationRequested();
        owner.BeginCompletionAppend();
        int offset = 0;
        while (offset < bytes.Length)
        {
            int count = Math.Min(WindowsTransferContentIo.BufferSize, bytes.Length - offset);
            await io.WriteAsync(owner.Handle, bytes.Slice(offset, count), checked(start + offset), cancellationToken).ConfigureAwait(false);
            offset += count;
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task WriteBytesAsync(WindowsTransferArtifactOwner owner, ReadOnlyMemory<byte> bytes,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < bytes.Length)
        {
            int count = Math.Min(WindowsTransferContentIo.BufferSize, bytes.Length - offset);
            await io.WriteAsync(owner.Handle, bytes.Slice(offset, count), offset, cancellationToken).ConfigureAwait(false);
            offset += count;
        }
        cancellationToken.ThrowIfCancellationRequested();
        io.SetLength(owner.Handle, bytes.Length);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task RequireBytesAsync(WindowsTransferArtifactOwner owner, ReadOnlyMemory<byte> expected,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[WindowsTransferContentIo.BufferSize];
        int offset = 0;
        while (offset < expected.Length)
        {
            int count = Math.Min(buffer.Length, expected.Length - offset);
            await io.ReadExactlyAsync(owner.Handle, buffer.AsMemory(0, count), offset, cancellationToken).ConfigureAwait(false);
            if (!buffer.AsSpan(0, count).SequenceEqual(expected.Span.Slice(offset, count)))
            {
                throw new IOException("The recovery mapping readback differs from the encoded frame.");
            }
            offset += count;
        }
        await io.RequireEndAsync(owner.Handle, offset, cancellationToken).ConfigureAwait(false);
    }
}
