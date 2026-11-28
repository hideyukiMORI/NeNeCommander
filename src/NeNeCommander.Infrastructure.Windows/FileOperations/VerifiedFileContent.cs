using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Compares exact stream bytes with bounded buffers and retains SHA-256 evidence.</summary>
internal static class VerifiedFileContent
{
    private const int BufferSize = 65536;

    internal static FileContentHash Compare(string source, string target)
    {
        using FileStream sourceStream = Open(source);
        using FileStream targetStream = Open(target);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] sourceBuffer = new byte[BufferSize];
        byte[] targetBuffer = new byte[BufferSize];
        int count;
        do
        {
            count = sourceStream.ReadAtLeast(sourceBuffer, BufferSize, throwOnEndOfStream: false);
            int targetCount = targetStream.ReadAtLeast(targetBuffer, BufferSize, throwOnEndOfStream: false);
            if (count != targetCount || !sourceBuffer.AsSpan(0, count).SequenceEqual(targetBuffer.AsSpan(0, targetCount)))
            {
                throw new IOException("The copied file bytes differ.");
            }
            hash.AppendData(sourceBuffer, 0, count);
        }
        while (count != 0);
        return FileContentHash.Create(Convert.ToHexString(hash.GetHashAndReset()));
    }

    /// <summary>Compares exact admitted lengths through borrowed handles, without reopening originals or granting identity/cleanup authority.</summary>
    internal static async Task<FileContentEvidence> CompareHeldAsync(
        WindowsTransferContentPair pair, WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pair);
        ArgumentNullException.ThrowIfNull(io);
        cancellationToken.ThrowIfCancellationRequested();
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] sourceBuffer = new byte[WindowsTransferContentIo.BufferSize];
        byte[] targetBuffer = new byte[WindowsTransferContentIo.BufferSize];
        long offset = 0;
        while (offset < pair.Size.Bytes)
        {
            int count = (int)Math.Min(sourceBuffer.Length, pair.Size.Bytes - offset);
            await io.ReadExactlyAsync(pair.Source, sourceBuffer.AsMemory(0, count), offset, cancellationToken).ConfigureAwait(false);
            await io.ReadExactlyAsync(pair.Target, targetBuffer.AsMemory(0, count), offset, cancellationToken).ConfigureAwait(false);
            if (!sourceBuffer.AsSpan(0, count).SequenceEqual(targetBuffer.AsSpan(0, count)))
            {
                throw new IOException("The copied file bytes differ.");
            }
            hash.AppendData(sourceBuffer, 0, count);
            offset += count;
        }
        await io.RequireEndAsync(pair.Source, offset, cancellationToken).ConfigureAwait(false);
        await io.RequireEndAsync(pair.Target, offset, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return FileContentEvidence.Create(pair.Size, FileContentHash.Create(Convert.ToHexString(hash.GetHashAndReset())));
    }

    /// <summary>Reads exactly one held stream against a fixed length without reopening it or granting cleanup authority.</summary>
    internal static async Task<FileContentEvidence> ReadHeldAsync(SafeFileHandle handle, KnownEntrySize size,
        WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(io);
        cancellationToken.ThrowIfCancellationRequested();
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[WindowsTransferContentIo.BufferSize];
        long offset = 0;
        while (offset < size.Bytes)
        {
            int count = (int)Math.Min(buffer.Length, size.Bytes - offset);
            await io.ReadExactlyAsync(handle, buffer.AsMemory(0, count), offset, cancellationToken).ConfigureAwait(false);
            hash.AppendData(buffer, 0, count);
            offset += count;
        }
        await io.RequireEndAsync(handle, offset, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return FileContentEvidence.Create(size, FileContentHash.Create(Convert.ToHexString(hash.GetHashAndReset())));
    }

    internal static FileContentHash Read(string path)
    {
        using FileStream stream = Open(path);
        return FileContentHash.Create(Convert.ToHexString(SHA256.HashData(stream)));
    }

    private static FileStream Open(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
            BufferSize, FileOptions.SequentialScan);
    }
}
