namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Identifies one closed reason why untrusted path text was rejected.
/// </summary>
public abstract record PathParseFailureKind
{
    /// <summary>Gets the failure used for missing or whitespace-only input.</summary>
    public static PathParseFailureKind Empty { get; } = new EmptyFailure();

    /// <summary>Gets the failure used when the input exceeds the supported boundary.</summary>
    public static PathParseFailureKind TooLong { get; } = new TooLongFailure();

    /// <summary>Gets the failure used for a path that is not absolute.</summary>
    public static PathParseFailureKind Relative { get; } = new RelativeFailure();

    /// <summary>Gets the failure used for an incomplete or ambiguous root.</summary>
    public static PathParseFailureKind InvalidRoot { get; } = new InvalidRootFailure();

    /// <summary>Gets the failure used when a segment contains invalid data.</summary>
    public static PathParseFailureKind InvalidSegment { get; } = new InvalidSegmentFailure();

    /// <summary>Gets the failure used when parent traversal would cross the provider root.</summary>
    public static PathParseFailureKind ParentTraversal { get; } = new ParentTraversalFailure();

    /// <summary>Gets the failure used for Windows device namespace paths.</summary>
    public static PathParseFailureKind DeviceNamespace { get; } = new DeviceNamespaceFailure();

    /// <summary>Gets the failure used for an unsafe WSL distribution name.</summary>
    public static PathParseFailureKind InvalidDistribution { get; } = new InvalidDistributionFailure();

    private PathParseFailureKind()
    {
    }

    private sealed record EmptyFailure : PathParseFailureKind;

    private sealed record TooLongFailure : PathParseFailureKind;

    private sealed record RelativeFailure : PathParseFailureKind;

    private sealed record InvalidRootFailure : PathParseFailureKind;

    private sealed record InvalidSegmentFailure : PathParseFailureKind;

    private sealed record ParentTraversalFailure : PathParseFailureKind;

    private sealed record DeviceNamespaceFailure : PathParseFailureKind;

    private sealed record InvalidDistributionFailure : PathParseFailureKind;
}
