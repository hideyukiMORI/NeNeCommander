using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Help;

/// <summary>
/// The keyboard help content split into the two columns of the approved centered-panel design
/// (direction A): the file-list group first, every other group second. A narrow window stacks the
/// second column below the first.
/// </summary>
public sealed record KeyboardHelpPresentation
{
    internal KeyboardHelpPresentation(
        IReadOnlyList<KeyboardHelpGroup> primaryGroups,
        IReadOnlyList<KeyboardHelpGroup> secondaryGroups)
    {
        ArgumentNullException.ThrowIfNull(primaryGroups);
        ArgumentNullException.ThrowIfNull(secondaryGroups);
        PrimaryGroups = primaryGroups;
        SecondaryGroups = secondaryGroups;
    }

    /// <summary>Gets the groups of the first column.</summary>
    public IReadOnlyList<KeyboardHelpGroup> PrimaryGroups { get; }

    /// <summary>Gets the groups of the second column.</summary>
    public IReadOnlyList<KeyboardHelpGroup> SecondaryGroups { get; }
}
