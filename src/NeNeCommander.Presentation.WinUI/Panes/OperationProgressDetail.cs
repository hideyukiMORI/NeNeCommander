using System;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents the completed and total source counts of a running operation.</summary>
public sealed record OperationProgressDetail : OperationDetail
{
    internal OperationProgressDetail(FileOperationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        Completed = progress.Completed;
        Total = progress.Total;
    }

    /// <summary>Gets the number of sources completed so far.</summary>
    public int Completed { get; }

    /// <summary>Gets the number of sources in the request.</summary>
    public int Total { get; }
}
