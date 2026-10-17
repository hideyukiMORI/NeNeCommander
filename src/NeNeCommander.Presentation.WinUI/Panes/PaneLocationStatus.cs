namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Names a closed indication of one observed pane location.</summary>
public sealed record PaneLocationStatus
{
    /// <summary>Gets the indication for a mapped network drive observed during the read.</summary>
    public static PaneLocationStatus NetworkDrive { get; } = new("PaneLocationNetworkDrive");

    private PaneLocationStatus(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the localization resource key naming this indication.</summary>
    public string ResourceKey { get; }
}
