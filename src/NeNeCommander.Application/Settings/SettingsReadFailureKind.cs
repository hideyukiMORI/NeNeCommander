namespace NeNeCommander.Application.Settings;

/// <summary>
/// Identifies one closed reason why a persisted settings document could not become settings.
/// Every reason keeps the default settings; none rewrites or repairs the stored document.
/// </summary>
public abstract record SettingsReadFailureKind
{
    /// <summary>Gets the failure for a document that is present but empty.</summary>
    public static SettingsReadFailureKind Empty { get; } = new EmptyFailure();

    /// <summary>Gets the failure for a document exceeding its fixed boundary.</summary>
    public static SettingsReadFailureKind TooLarge { get; } = new TooLargeFailure();

    /// <summary>Gets the failure for malformed text, an invalid root shape, or a wrongly typed value.</summary>
    public static SettingsReadFailureKind Malformed { get; } = new MalformedFailure();

    /// <summary>Gets the failure for an unsupported schema version.</summary>
    public static SettingsReadFailureKind UnknownVersion { get; } = new UnknownVersionFailure();

    /// <summary>Gets the failure for an unknown or duplicated property.</summary>
    public static SettingsReadFailureKind UnexpectedProperty { get; } = new UnexpectedPropertyFailure();

    /// <summary>Gets the failure for a missing required property.</summary>
    public static SettingsReadFailureKind Incomplete { get; } = new IncompleteFailure();

    /// <summary>Gets the failure for text that names no approved color scheme.</summary>
    public static SettingsReadFailureKind UnknownColorScheme { get; } = new UnknownColorSchemeFailure();

    /// <summary>Gets the failure for an expected input failure that prevented reading the document.</summary>
    public static SettingsReadFailureKind Unreadable { get; } = new UnreadableFailure();

    private SettingsReadFailureKind()
    {
    }

    private sealed record EmptyFailure : SettingsReadFailureKind;
    private sealed record TooLargeFailure : SettingsReadFailureKind;
    private sealed record MalformedFailure : SettingsReadFailureKind;
    private sealed record UnknownVersionFailure : SettingsReadFailureKind;
    private sealed record UnexpectedPropertyFailure : SettingsReadFailureKind;
    private sealed record IncompleteFailure : SettingsReadFailureKind;
    private sealed record UnknownColorSchemeFailure : SettingsReadFailureKind;
    private sealed record UnreadableFailure : SettingsReadFailureKind;
}
