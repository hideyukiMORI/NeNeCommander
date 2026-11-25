using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Bounds and verifies one length/checksum-framed UTF-8 record, including the exact framing bytes.</summary>
internal sealed record WindowsTransferRecoveryFrame
{
    internal const int MaximumPayloadLength = 1_048_576;
    internal const int HeaderLength = 74;
    internal const int MaximumFrameLength = MaximumPayloadLength + HeaderLength + 1;
    internal const int MaximumRecordLength = 2 * MaximumFrameLength;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private WindowsTransferRecoveryFrame(string json, RecoveryFrameEvidence evidence)
    {
        Json = json;
        Evidence = evidence;
    }

    internal string Json { get; }
    internal RecoveryFrameEvidence Evidence { get; }

    internal static byte[] Wrap(ReadOnlySpan<byte> payload)
    {
        if (payload.Length is 0 or > MaximumPayloadLength)
        {
            throw new InvalidDataException("The recovery payload exceeds its bounded frame.");
        }
        string header = payload.Length.ToString("X8", CultureInfo.InvariantCulture) + ":" + Convert.ToHexString(SHA256.HashData(payload)) + "\n";
        byte[] frame = new byte[payload.Length + HeaderLength + 1];
        _ = Encoding.ASCII.GetBytes(header, frame.AsSpan(0, HeaderLength));
        payload.CopyTo(frame.AsSpan(HeaderLength));
        frame[^1] = (byte)'\n';
        return frame;
    }

    internal static WindowsTransferRecoveryFrame Parse(ReadOnlySpan<byte> record)
    {
        int length = DeclaredLength(record);
        if (record.Length < length || record[length - 1] != (byte)'\n')
        {
            throw new InvalidDataException("The recovery frame is incomplete or has an invalid terminator.");
        }
        ReadOnlySpan<byte> payload = record.Slice(HeaderLength, length - HeaderLength - 1);
        string expected = Encoding.ASCII.GetString(record.Slice(9, 64));
        if (!string.Equals(expected, Convert.ToHexString(SHA256.HashData(payload)), StringComparison.Ordinal))
        {
            throw new InvalidDataException("The recovery frame checksum does not match.");
        }
        string json = DecodeUtf8(payload);
        RecoveryFrameEvidence evidence = RecoveryFrameEvidence.Create((KnownEntrySize)EntrySize.Create(length),
            FileContentHash.Create(Convert.ToHexString(SHA256.HashData(record[..length]))));
        return new WindowsTransferRecoveryFrame(json, evidence);
    }

    internal static int DeclaredLength(ReadOnlySpan<byte> record)
    {
        RequireHeaderPrefix(record);
        if (record.Length < HeaderLength)
        {
            throw new InvalidDataException("The recovery frame header is incomplete.");
        }
        uint payload = uint.Parse(Encoding.ASCII.GetString(record[..8]), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return payload is 0 or > MaximumPayloadLength
            ? throw new InvalidDataException("The declared recovery payload exceeds its bound.")
            : checked((int)payload + HeaderLength + 1);
    }

    internal static void RequireHeaderPrefix(ReadOnlySpan<byte> record)
    {
        for (int index = 0; index < Math.Min(record.Length, HeaderLength); index++)
        {
            bool accepted = index switch
            {
                8 => record[index] == (byte)':',
                73 => record[index] == (byte)'\n',
                _ => char.IsAsciiHexDigitUpper((char)record[index]),
            };
            if (!accepted)
            {
                throw new InvalidDataException("The recovery frame header is malformed.");
            }
        }
        RequirePossibleLength(record);
    }

    private static void RequirePossibleLength(ReadOnlySpan<byte> record)
    {
        int digits = Math.Min(record.Length, 8);
        if (digits == 0)
        {
            return;
        }
        uint prefix = uint.Parse(Encoding.ASCII.GetString(record[..digits]), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        uint minimum = prefix << ((8 - digits) * 4);
        if (minimum > MaximumPayloadLength || (digits == 8 && minimum == 0))
        {
            throw new InvalidDataException("The recovery length prefix cannot complete within its bound.");
        }
    }

    internal static void RequireText(string text)
    {
        try
        {
            _ = StrictUtf8.GetByteCount(text);
        }
        catch (EncoderFallbackException exception)
        {
            throw new InvalidDataException("The recovery text cannot round-trip exact UTF-16 content.", exception);
        }
    }

    private static string DecodeUtf8(ReadOnlySpan<byte> payload)
    {
        try
        {
            return StrictUtf8.GetString(payload);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("The recovery payload is not strict UTF-8.", exception);
        }
    }
}
