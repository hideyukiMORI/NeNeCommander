using System;
using System.Linq;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains immutable SHA-256 evidence without retaining file contents.</summary>
public sealed record FileContentHash
{
    private FileContentHash(string value)
    {
        Value = value;
    }

    /// <summary>Gets the canonical uppercase SHA-256 hexadecimal value.</summary>
    public string Value { get; }

    /// <summary>Creates evidence from exactly one SHA-256 digest.</summary>
    public static FileContentHash Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length != 64 || value.Any(character => !char.IsAsciiHexDigitUpper(character))
            ? throw new ArgumentException("Content evidence must be one uppercase SHA-256 digest.", nameof(value))
            : new FileContentHash(value);
    }
}
