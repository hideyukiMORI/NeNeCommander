using System;
using System.Text;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Admits exactly one complete native unnamed-stream entry of the expected byte length.</summary>
internal static class WindowsUnnamedStream
{
    // FILE_STREAM_INFO has a 24-byte header and seven UTF-16 characters, rounded to eight-byte alignment.
    internal const int ObservationLength = 40;

    internal static bool Matches(ReadOnlySpan<byte> observation, long expectedLength)
    {
        return observation.Length == ObservationLength && expectedLength >= 0 &&
            BitConverter.ToUInt32(observation) == 0 && BitConverter.ToUInt32(observation[4..]) == 14 &&
            BitConverter.ToInt64(observation[8..]) == expectedLength && BitConverter.ToInt64(observation[16..]) >= 0 &&
            string.Equals(Encoding.Unicode.GetString(observation.Slice(24, 14)), "::$DATA", StringComparison.Ordinal);
    }
}
