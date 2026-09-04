namespace NeNeCommander.Application.Settings;

/// <summary>
/// Represents persisted text that named exactly one approved color scheme.
/// </summary>
public sealed record ColorSchemeAccepted : ColorSchemeParseOutcome
{
    internal ColorSchemeAccepted(ColorScheme scheme)
    {
        Scheme = scheme;
    }

    /// <summary>Gets the approved scheme the text named.</summary>
    public ColorScheme Scheme { get; }
}
