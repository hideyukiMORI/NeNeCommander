using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Input;

/// <summary>Carries a session-resolved bookmark path through the existing pane intent route.</summary>
internal sealed record ResolvedBookmarkNavigation : UserIntent
{
    internal ResolvedBookmarkNavigation(FileSystemPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Path = path;
    }

    internal FileSystemPath Path { get; }
}
