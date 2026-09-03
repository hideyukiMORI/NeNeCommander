namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Identifies one closed request-construction failure.
/// </summary>
public abstract record FileOperationRequestFailureKind
{
    /// <summary>Gets the failure for an empty source set.</summary>
    public static FileOperationRequestFailureKind EmptySources { get; } = new EmptySourcesFailure();

    /// <summary>Gets the failure for a source set exceeding the fixed operation boundary.</summary>
    public static FileOperationRequestFailureKind TooManySources { get; } = new TooManySourcesFailure();

    /// <summary>Gets the failure for a null source element.</summary>
    public static FileOperationRequestFailureKind NullSource { get; } = new NullSourceFailure();

    /// <summary>Gets the failure for a duplicate source.</summary>
    public static FileOperationRequestFailureKind DuplicateSource { get; } = new DuplicateSourceFailure();

    /// <summary>Gets the failure for a destination equal to a source.</summary>
    public static FileOperationRequestFailureKind DestinationIsSource { get; } = new DestinationIsSourceFailure();

    /// <summary>Gets the failure for a directory name the domain path rules reject.</summary>
    public static FileOperationRequestFailureKind InvalidName { get; } = new InvalidNameFailure();

    private FileOperationRequestFailureKind()
    {
    }

    private sealed record EmptySourcesFailure : FileOperationRequestFailureKind;
    private sealed record TooManySourcesFailure : FileOperationRequestFailureKind;
    private sealed record NullSourceFailure : FileOperationRequestFailureKind;
    private sealed record DuplicateSourceFailure : FileOperationRequestFailureKind;
    private sealed record DestinationIsSourceFailure : FileOperationRequestFailureKind;
    private sealed record InvalidNameFailure : FileOperationRequestFailureKind;
}
