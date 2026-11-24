using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Names distinct captured targets while reusing the sole catalog and localized row projection.</summary>
internal static class CommandMenuPresenter
{
    internal static IReadOnlyList<CommandMenuSection> Present(CommandPaletteOpen open, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(localize);
        CommandPaletteViewState view = CommandPalettePresenter.Present(open, localize);
        PaneSnapshot snapshot = open.ActiveSide == PaneSide.Left ? open.Left : open.Right;
        PaneState pane = ((PaneContentListed)snapshot.Content).State;
        List<CommandMenuSection> sections = [];
        CommandMenuGroup[] groups = [CommandMenuGroup.FocusedItem, CommandMenuGroup.Selection, CommandMenuGroup.New, CommandMenuGroup.Folder];
        foreach (CommandMenuGroup group in groups)
        {
            List<CommandPaletteRow> rows = [.. view.Rows.Where(row => row.Candidate.Group == group)];
            if (rows.Count > 0)
            {
                sections.Add(new CommandMenuSection(Title(group, pane, localize), rows.AsReadOnly()));
            }
        }
        return sections.AsReadOnly();
    }

    private static string Title(CommandMenuGroup group, PaneState pane, Func<string, string> localize)
    {
        if (group == CommandMenuGroup.Selection && pane.Selection.Count > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, localize("CommandMenuSelectionFormat"), pane.Selection.Count);
        }
        if (group == CommandMenuGroup.FocusedItem || group == CommandMenuGroup.Selection)
        {
            string name = pane.VisibleEntries.First(entry => ReferenceEquals(entry.Path, pane.FocusItem)).Name;
            string format = group == CommandMenuGroup.Selection
                ? localize("CommandMenuFocusFallbackFormat") : localize("CommandMenuFocusFormat");
            return string.Format(CultureInfo.InvariantCulture, format, name);
        }
        string folderFormat = group == CommandMenuGroup.New
            ? localize("CommandMenuNewFormat") : localize("CommandMenuFolderFormat");
        return string.Format(CultureInfo.InvariantCulture, folderFormat, pane.Location.CanonicalText);
    }
}
