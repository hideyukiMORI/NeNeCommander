using System;

namespace NeNeCommander.Application.Bookmarks;

/// <summary>Carries session-derived defaults for a new bookmark draft.</summary>
internal sealed record BookmarkRegistrationDefaults
{
    internal BookmarkRegistrationDefaults(string name, string path)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(path);
        Name = name;
        Path = path;
    }

    internal string Name { get; }

    internal string Path { get; }
}
