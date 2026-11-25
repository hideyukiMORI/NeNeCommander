using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the captured pane owner and parsed query through the sole reducer route.</summary>
internal sealed record PaneFilterQueryAccepted : PaneFilterValidation
{
    internal PaneFilterQueryAccepted(PaneFilterEditing editor, PaneNameFilterQuery query)
    {
        Editor = editor;
        Query = query;
    }

    internal PaneFilterEditing Editor { get; }

    internal PaneNameFilterQuery Query { get; }
}
