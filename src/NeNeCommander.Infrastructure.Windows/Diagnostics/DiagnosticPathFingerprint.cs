using System;
using System.Security.Cryptography;
using System.Text;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Diagnostics;

/// <summary>Represents a bounded salted path identifier that does not disclose its source path.</summary>
public sealed record DiagnosticPathFingerprint
{
    private DiagnosticPathFingerprint(string value)
    {
        Value = value;
    }

    /// <summary>Gets the fixed-length uppercase hexadecimal fingerprint.</summary>
    public string Value { get; }

    /// <summary>Creates a stable per-installation fingerprint for a validated path.</summary>
    /// <param name="path">Validated path whose full text must not reach diagnostics.</param>
    /// <param name="salt">Validated per-installation entropy.</param>
    /// <returns>A fixed-length diagnostic fingerprint.</returns>
    public static DiagnosticPathFingerprint Create(FileSystemPath path, DiagnosticSalt salt)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(salt);
        byte[] key = Encoding.UTF8.GetBytes(salt.Value);
        byte[] input = Encoding.UTF8.GetBytes(path.CanonicalText);
        byte[] hash = HMACSHA256.HashData(key, input);
        return new DiagnosticPathFingerprint(Convert.ToHexString(hash.AsSpan(0, 8)));
    }
}
