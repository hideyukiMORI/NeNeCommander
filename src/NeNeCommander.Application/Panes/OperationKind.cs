namespace NeNeCommander.Application.Panes;

/// <summary>
/// Identifies the closed kind of file operation the dual-pane session started.
/// </summary>
public abstract record OperationKind
{
    /// <summary>Gets the kind for moving the active pane's items to the passive pane.</summary>
    public static OperationKind Move { get; } = new MoveKind();

    /// <summary>Gets the kind for copying the active pane's items to the passive pane.</summary>
    public static OperationKind Copy { get; } = new CopyKind();

    /// <summary>Gets the kind for deleting the active pane's items.</summary>
    public static OperationKind Delete { get; } = new DeleteKind();

    /// <summary>Gets the kind for creating a directory in the active pane's location.</summary>
    public static OperationKind CreateDirectory { get; } = new CreateDirectoryKind();

    private OperationKind()
    {
    }

    private sealed record MoveKind : OperationKind;
    private sealed record CopyKind : OperationKind;
    private sealed record DeleteKind : OperationKind;
    private sealed record CreateDirectoryKind : OperationKind;
}
