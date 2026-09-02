namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Identifies one closed settings-document rejection.</summary>
public abstract record SettingsValidationFailureKind
{
    /// <summary>Gets the failure for a missing document.</summary>
    public static SettingsValidationFailureKind Empty { get; } = new EmptyFailure();

    /// <summary>Gets the failure for a document exceeding its fixed boundary.</summary>
    public static SettingsValidationFailureKind TooLarge { get; } = new TooLargeFailure();

    /// <summary>Gets the failure for malformed JSON or an invalid root shape.</summary>
    public static SettingsValidationFailureKind Malformed { get; } = new MalformedFailure();

    /// <summary>Gets the failure for an unsupported schema version.</summary>
    public static SettingsValidationFailureKind UnknownVersion { get; } = new UnknownVersionFailure();

    /// <summary>Gets the failure for an unknown or duplicate property.</summary>
    public static SettingsValidationFailureKind UnexpectedProperty { get; } = new UnexpectedPropertyFailure();

    /// <summary>Gets the failure for a missing required property.</summary>
    public static SettingsValidationFailureKind Incomplete { get; } = new IncompleteFailure();

    private SettingsValidationFailureKind()
    {
    }

    private sealed record EmptyFailure : SettingsValidationFailureKind;
    private sealed record TooLargeFailure : SettingsValidationFailureKind;
    private sealed record MalformedFailure : SettingsValidationFailureKind;
    private sealed record UnknownVersionFailure : SettingsValidationFailureKind;
    private sealed record UnexpectedPropertyFailure : SettingsValidationFailureKind;
    private sealed record IncompleteFailure : SettingsValidationFailureKind;
}
