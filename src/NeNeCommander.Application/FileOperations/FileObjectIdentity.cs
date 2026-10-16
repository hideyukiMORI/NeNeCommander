using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Identifies a provider object independently of metadata changed by owned child deletion.</summary>
public sealed record FileObjectIdentity
{
    private FileObjectIdentity(string value)
    {
        Value = value;
    }

    /// <summary>Gets the opaque provider-owned object identity.</summary>
    public string Value { get; }

    /// <summary>Creates a bounded object identity supplied by its provider.</summary>
    public static FileObjectIdentity Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, 512);
        return new FileObjectIdentity(value);
    }
}
