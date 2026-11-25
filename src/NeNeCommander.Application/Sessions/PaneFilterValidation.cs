namespace NeNeCommander.Application.Sessions;

/// <summary>Represents whether an admitted edit names one query to apply.</summary>
internal abstract record PaneFilterValidation
{
    internal static PaneFilterValidation NothingToApply { get; } = new NoChange();

    private protected PaneFilterValidation()
    {
    }

    private sealed record NoChange : PaneFilterValidation;
}
