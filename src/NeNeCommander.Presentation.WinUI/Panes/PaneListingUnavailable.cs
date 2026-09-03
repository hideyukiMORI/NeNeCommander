using System;
using System.Collections.Generic;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Represents a pane with no listing because the read was cancelled or failed.</summary>
public sealed record PaneListingUnavailable : PanePresentation
{
    internal PaneListingUnavailable(PaneStatus status)
    {
        Status = status;
    }

    /// <inheritdoc />
    public override PaneStatus Status { get; }

    /// <inheritdoc />
    public override IReadOnlyList<DirectoryEntry> Entries => Array.Empty<DirectoryEntry>();

    /// <inheritdoc />
    public override DirectoryEntry? FocusEntry => null;
}
