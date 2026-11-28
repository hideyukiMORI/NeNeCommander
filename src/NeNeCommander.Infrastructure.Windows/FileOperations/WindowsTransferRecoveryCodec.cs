using System;
using System.IO;
using System.Text;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Encodes and inspects at most one initial mapping and one exact-initial-bound completion, with no filesystem or recovery effects.</summary>
internal static class WindowsTransferRecoveryCodec
{
    internal static byte[] EncodeInitial(WindowsTransferRecoveryMapping mapping)
    {
        return WindowsTransferRecoveryFrame.Wrap(WindowsTransferRecoveryJson.WriteInitial(mapping));
    }

    internal static byte[] EncodeCompletion(ReadOnlyMemory<byte> initialFrame)
    {
        byte[] snapshot = Snapshot(initialFrame);
        InitialRecord initial = ReadInitial(snapshot);
        return initial.Frame.Evidence.Size.Bytes != snapshot.Length
            ? throw new InvalidDataException("Completion encoding requires exactly one complete initial frame.")
            : WindowsTransferRecoveryFrame.Wrap(WindowsTransferRecoveryJson.WriteCompletion(initial.Mapping, initial.Frame.Evidence.Hash));
    }

    internal static WindowsTransferRecoveryInspection Inspect(ReadOnlyMemory<byte> record)
    {
        byte[] snapshot = Snapshot(record);
        InitialRecord initial = ReadInitial(snapshot);
        ReadOnlySpan<byte> tail = snapshot.AsSpan(checked((int)initial.Frame.Evidence.Size.Bytes));
        WindowsTransferRecoveryFrame.RequireHeaderPrefix(tail);
        if (tail.Length < WindowsTransferRecoveryFrame.HeaderLength || tail.Length < WindowsTransferRecoveryFrame.DeclaredLength(tail))
        {
            return WindowsTransferRecoveryInspection.Prepared(initial.Mapping, initial.Frame.Evidence,
                (KnownEntrySize)EntrySize.Create(tail.Length));
        }
        WindowsTransferRecoveryFrame completion = WindowsTransferRecoveryFrame.Parse(tail);
        string expected = Encoding.UTF8.GetString(WindowsTransferRecoveryJson.WriteCompletion(initial.Mapping, initial.Frame.Evidence.Hash));
        return completion.Evidence.Size.Bytes != tail.Length || !string.Equals(completion.Json, expected, StringComparison.Ordinal)
            ? throw new InvalidDataException("The completion does not match this exact initial frame or is followed by extra bytes.")
            : WindowsTransferRecoveryInspection.Completed(initial.Mapping, initial.Frame.Evidence, completion.Evidence);
    }

    private static InitialRecord ReadInitial(ReadOnlySpan<byte> record)
    {
        WindowsTransferRecoveryFrame frame = WindowsTransferRecoveryFrame.Parse(record);
        return new InitialRecord(WindowsTransferRecoveryJson.ReadInitial(frame.Json), frame);
    }

    private static byte[] Snapshot(ReadOnlyMemory<byte> record)
    {
        return record.Length > WindowsTransferRecoveryFrame.MaximumRecordLength
            ? throw new InvalidDataException("The recovery log exceeds its total input bound.") : record.ToArray();
    }

    private sealed record InitialRecord
    {
        internal InitialRecord(WindowsTransferRecoveryMapping mapping, WindowsTransferRecoveryFrame frame)
        {
            Mapping = mapping;
            Frame = frame;
        }

        internal WindowsTransferRecoveryMapping Mapping { get; }
        internal WindowsTransferRecoveryFrame Frame { get; }
    }
}
