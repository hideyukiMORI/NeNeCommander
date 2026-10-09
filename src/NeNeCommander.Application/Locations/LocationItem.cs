using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Locations;

/// <summary>
/// Represents one selectable entry of the Locations picker: a listed drive root or a listed WSL
/// distribution root. Selecting it sends the active pane to <see cref="Location"/> through the
/// single pane navigation route.
/// </summary>
public abstract record LocationItem
{
    private protected LocationItem()
    {
    }

    /// <summary>Gets the validated root the active pane navigates to when this entry is selected.</summary>
    public abstract FileSystemPath Location { get; }
}
