using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the completed and total source counts of a running operation together with the fixed
/// list of progress segments that visualizes them. The segment count never changes with the number
/// of sources, so a one-item and a thousand-item operation are read the same way.
/// </summary>
public sealed record OperationProgressDetail : OperationDetail
{
    /// <summary>The fixed number of segments the progress bar always draws.</summary>
    public const int SegmentCount = 12;

    internal OperationProgressDetail(FileOperationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        Completed = progress.Completed;
        Total = progress.Total;
        Segments = CreateSegments(progress.Completed, progress.Total);
    }

    /// <summary>Gets the number of sources completed so far.</summary>
    public int Completed { get; }

    /// <summary>Gets the number of sources in the request.</summary>
    public int Total { get; }

    /// <summary>
    /// Gets the <see cref="SegmentCount"/> segments in drawing order. A segment is filled once the
    /// whole proportion it represents has completed, so the bar never claims more than is done.
    /// </summary>
    public IReadOnlyList<ProgressSegment> Segments { get; }

    private static ReadOnlyCollection<ProgressSegment> CreateSegments(int completed, int total)
    {
        int filled = completed * SegmentCount / total;
        List<ProgressSegment> segments = [];
        for (int index = 0; index < SegmentCount; index++)
        {
            segments.Add(index < filled ? ProgressSegment.Filled : ProgressSegment.Empty);
        }
        return segments.AsReadOnly();
    }
}
