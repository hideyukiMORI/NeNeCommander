namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the closed numeric detail shown beside the operation status: nothing, the item count
/// a pending confirmation names, or the progress of a running operation. Numbers are rendered by
/// the host in their own controls; no user-facing text is assembled from them.
/// </summary>
public abstract record OperationDetail
{
    private protected OperationDetail()
    {
    }

    /// <summary>Gets the detail when there is nothing numeric to show.</summary>
    public static OperationDetail None { get; } = new OperationNoDetail();

    private sealed record OperationNoDetail : OperationDetail;
}
