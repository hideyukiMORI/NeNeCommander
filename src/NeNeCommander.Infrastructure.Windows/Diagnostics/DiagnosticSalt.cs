namespace NeNeCommander.Infrastructure.Windows.Diagnostics;

/// <summary>Represents validated per-installation entropy used only for diagnostic fingerprints.</summary>
public sealed record DiagnosticSalt
{
    private const int MinimumLength = 32;
    private const int MaximumLength = 256;
    private DiagnosticSalt(string value)
    {
        Value = value;
    }

    internal string Value { get; }

    /// <summary>Validates externally generated per-installation entropy.</summary>
    /// <param name="value">Opaque entropy encoded without transformation.</param>
    /// <returns>An accepted salt or a typed rejection.</returns>
    public static DiagnosticSaltCreation Parse(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
            value.Length < MinimumLength ||
            value.Length > MaximumLength
            ? new DiagnosticSaltRejected()
            : new DiagnosticSaltAccepted(new DiagnosticSalt(value));
    }
}
