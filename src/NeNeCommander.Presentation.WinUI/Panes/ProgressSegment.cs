namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed state of one segment of the running-operation progress bar. The number of
/// segments and which of them are filled are computed once by the presentation, so the view draws
/// a fixed list of closed values and performs no arithmetic.
/// </summary>
public abstract record ProgressSegment
{
    /// <summary>Gets the segment that represents completed work.</summary>
    public static ProgressSegment Filled { get; } = new FilledSegment();

    /// <summary>Gets the segment that represents work not yet completed.</summary>
    public static ProgressSegment Empty { get; } = new EmptySegment();

    private ProgressSegment()
    {
    }

    /// <summary>Gets the semantic brush resource key that fills the segment.</summary>
    public abstract string BrushResourceKey { get; }

    private sealed record FilledSegment : ProgressSegment
    {
        public override string BrushResourceKey => "OperationProgressBrush";
    }

    private sealed record EmptySegment : ProgressSegment
    {
        public override string BrushResourceKey => "OperationTrackBrush";
    }
}
