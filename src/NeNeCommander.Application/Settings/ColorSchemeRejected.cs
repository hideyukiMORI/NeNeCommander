namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents persisted color-scheme text that named no approved scheme. The caller keeps the
/// default scheme; it never widens the closed set to accept the rejected text.
/// </summary>
public sealed record ColorSchemeRejected : ColorSchemeParseOutcome
{
    internal ColorSchemeRejected(ColorSchemeFailureKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets the closed rejection reason.</summary>
    public ColorSchemeFailureKind Kind { get; }
}
