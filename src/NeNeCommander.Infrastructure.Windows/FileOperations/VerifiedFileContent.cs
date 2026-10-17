using System;
using System.IO;
using System.Security.Cryptography;
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
