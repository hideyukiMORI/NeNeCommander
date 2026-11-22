using System;
using System.Collections.Generic;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>
/// Creates localized palette rows without owning resources, catalog policy, or execution. The
/// separators that join already localized parts are palette design rather than language, so they
/// are literal formats here; a format that carries its own words stays a localized resource.
/// </summary>
public static class CommandPalettePresenter
{
    /// <summary>Creates one Presentation view state for an Application-owned open scope.</summary>
    public static CommandPaletteViewState Present(
        CommandPaletteOpen open,
        Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(localize);
        CommandPaletteContext activeContext = CommandPaletteContext.Create(open.ActiveSide, localize);
        CommandPaletteContext oppositeContext = CommandPaletteContext.Create(open.ActiveSide.Other, localize);
        IReadOnlyList<KeyBinding> bindings = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList);
        List<CommandPaletteRow> rows = [];
        foreach (CommandCandidate candidate in open.Candidates)
        {
            CommandPaletteContext context = candidate.Intent == UserIntent.OpenCurrentLocationInOtherPane
                ? oppositeContext : activeContext;
            CommandPaletteCaption caption = CommandPaletteCaption.Create(candidate.Intent, bindings, localize);
            CommandPaletteFeedback feedback = CommandPaletteFeedback.Create(candidate.Availability, caption, context, localize);
            rows.Add(new CommandPaletteRow(candidate, caption, context, feedback));
        }
        return new CommandPaletteViewState(open, rows.AsReadOnly());
    }

}
