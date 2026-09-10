namespace NeNeCommander.Application.Settings;

/// <summary>Identifies one closed expected reason that a settings write was rejected.</summary>
public abstract record SettingsWriteFailureKind
{
    /// <summary>Gets a rejection caused by denied access.</summary>
    public static SettingsWriteFailureKind Unauthorized { get; } = new UnauthorizedFailure();

    /// <summary>Gets a rejection caused by an expected filesystem failure.</summary>
    public static SettingsWriteFailureKind IoFailure { get; } = new IoFailureKind();

    /// <summary>Gets a rejection because the existing document did not validate.</summary>
    public static SettingsWriteFailureKind ExistingDocumentRejected { get; } =
        new ExistingDocumentRejectedFailure();

    /// <summary>Gets a rejection because the fixed sibling temporary path was already occupied.</summary>
    public static SettingsWriteFailureKind TemporaryArtifactCollision { get; } =
        new TemporaryArtifactCollisionFailure();

    /// <summary>Gets a rejection because the destination changed after write preflight.</summary>
    public static SettingsWriteFailureKind DestinationChanged { get; } =
        new DestinationChangedFailure();

    /// <summary>Gets a rejection because a settings ancestor is missing, changed, or a reparse point.</summary>
    public static SettingsWriteFailureKind UnsafeLocation { get; } =
        new UnsafeLocationFailure();

    /// <summary>Gets the rejection for a serialized document beyond the fixed byte boundary.</summary>
    public static SettingsWriteFailureKind TooLarge { get; } = new TooLargeFailure();

    private SettingsWriteFailureKind()
    {
    }

    private sealed record UnauthorizedFailure : SettingsWriteFailureKind;
    private sealed record IoFailureKind : SettingsWriteFailureKind;
    private sealed record ExistingDocumentRejectedFailure : SettingsWriteFailureKind;
    private sealed record TemporaryArtifactCollisionFailure : SettingsWriteFailureKind;
    private sealed record DestinationChangedFailure : SettingsWriteFailureKind;
    private sealed record UnsafeLocationFailure : SettingsWriteFailureKind;
    private sealed record TooLargeFailure : SettingsWriteFailureKind;
}
