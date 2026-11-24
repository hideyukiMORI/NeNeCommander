using System;
using System.Collections.Generic;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Projects one explicit captured target and its catalog-owned menu rows.</summary>
internal sealed record CommandMenuSection
{
    internal CommandMenuSection(string title, IReadOnlyList<CommandPaletteRow> rows)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(rows);
        Title = title;
        Rows = rows;
    }

    internal string Title { get; }

    internal IReadOnlyList<CommandPaletteRow> Rows { get; }
}
