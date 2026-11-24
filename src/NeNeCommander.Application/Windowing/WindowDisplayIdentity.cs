namespace NeNeCommander.Application.Windowing;

/// <summary>Names one currently observed display without exposing a platform handle or saved index.</summary>
public sealed record WindowDisplayIdentity
{
    private WindowDisplayIdentity(ulong value)
    {
        Value = value;
    }

    /// <summary>Gets the opaque SDK identity, meaningful only within fresh observations.</summary>
    public ulong Value { get; }

    /// <summary>Wraps one identity supplied by the declared window adapter.</summary>
    /// <param name="value">Opaque display identity; no ordering or persistence is inferred.</param>
    /// <returns>The immutable identity.</returns>
    public static WindowDisplayIdentity Create(ulong value)
    {
        return new WindowDisplayIdentity(value);
    }
}
