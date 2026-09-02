namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Identifies one closed expected reason that a file operation did not complete.
/// </summary>
public abstract record FileOperationFailureKind
{
    /// <summary>Gets the failure for another operation already owning the gateway.</summary>
    public static FileOperationFailureKind Reentrant { get; } = new ReentrantFailure();

    /// <summary>Gets the failure for provider inspection rejection.</summary>
    public static FileOperationFailureKind Inspection { get; } = new InspectionFailure();

    /// <summary>Gets the failure for an explicit destination conflict.</summary>
    public static FileOperationFailureKind Conflict { get; } = new ConflictFailure();

    /// <summary>Gets the failure for copy rejection.</summary>
    public static FileOperationFailureKind Copy { get; } = new CopyFailure();

    /// <summary>Gets the failure for copy verification rejection.</summary>
    public static FileOperationFailureKind Verification { get; } = new VerificationFailure();

    /// <summary>Gets the failure for source or target deletion rejection.</summary>
    public static FileOperationFailureKind Delete { get; } = new DeleteFailure();

    /// <summary>Gets the failure for missing permanent-delete confirmation.</summary>
    public static FileOperationFailureKind ConfirmationRequired { get; } = new ConfirmationRequiredFailure();

    /// <summary>Gets the failure for changed provider identity.</summary>
    public static FileOperationFailureKind IdentityChanged { get; } = new IdentityChangedFailure();

    /// <summary>Gets the failure for unavailable or unsupported provider behavior.</summary>
    public static FileOperationFailureKind ProviderUnavailable { get; } = new ProviderUnavailableFailure();

    /// <summary>Gets the failure for provider access denial.</summary>
    public static FileOperationFailureKind AccessDenied { get; } = new AccessDeniedFailure();

    /// <summary>Gets the failure for an entry that no longer exists.</summary>
    public static FileOperationFailureKind NotFound { get; } = new NotFoundFailure();

    private FileOperationFailureKind()
    {
    }

    private sealed record ReentrantFailure : FileOperationFailureKind;
    private sealed record InspectionFailure : FileOperationFailureKind;
    private sealed record ConflictFailure : FileOperationFailureKind;
    private sealed record CopyFailure : FileOperationFailureKind;
    private sealed record VerificationFailure : FileOperationFailureKind;
    private sealed record DeleteFailure : FileOperationFailureKind;
    private sealed record ConfirmationRequiredFailure : FileOperationFailureKind;
    private sealed record IdentityChangedFailure : FileOperationFailureKind;
    private sealed record ProviderUnavailableFailure : FileOperationFailureKind;
    private sealed record AccessDeniedFailure : FileOperationFailureKind;
    private sealed record NotFoundFailure : FileOperationFailureKind;
}
