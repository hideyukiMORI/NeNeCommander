using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the menu visibly distinguishes focus, selection and folder targets captured by the session.</summary>
[TestClass]
public sealed class CommandMenuPresentationTests
{
    /// <summary>Proves focus outside the selected batch has independent target headings and shared canonical rows.</summary>
    [TestMethod]
    public async Task PresentWhenFocusOutsideSelectionKeepsBothTargetsAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.ToggleSelection);
        _ = await fixture.HandleAsync(UserIntent.MoveNext);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        IReadOnlyList<CommandMenuSection> sections = CommandMenuPresenter.Present(open, Localize);
        Assert.AreEqual(4, sections.Count);
        Assert.AreEqual("Focus: b", sections[0].Title);
        Assert.AreEqual("Selection: 1", sections[1].Title);
        Assert.AreEqual(@"New: C:\pointer", sections[2].Title);
        Assert.AreEqual(@"Folder: C:\pointer", sections[3].Title);
        Assert.AreSame(UserIntent.Copy, sections[1].Rows[0].Intent);
        Assert.AreSame(UserIntent.CreateTextFile, sections[2].Rows[1].Intent);
        CommandPaletteRow reveal = sections[0].Rows.Single(row => row.Intent == UserIntent.RevealFocused);
        Assert.AreEqual(string.Empty, reveal.Shortcut);
        Assert.IsTrue(sections.SelectMany(section => section.Rows).All(row => open.Candidates.Any(candidate => ReferenceEquals(candidate, row.Candidate))));
        Assert.AreEqual(open.Candidates.Count, sections.Sum(section => section.Rows.Count));
    }

    /// <summary>Proves the unmarked focus fallback is named instead of implying an empty selected batch.</summary>
    [TestMethod]
    public async Task PresentWhenNoExplicitSelectionNamesFocusFallbackAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        Assert.AreEqual("Focus fallback: a", CommandMenuPresenter.Present(open, Localize)[1].Title);
    }

    /// <summary>Proves right background context hides all item/batch groups but retains New and folder actions.</summary>
    [TestMethod]
    public async Task PresentWhenBackgroundContextOnlyNamesCapturedFolderAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        UserIntent request = mapper.MapSurfaceContext(PaneSide.Right, fixture.PresentRight());
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(request)).Scopes.CommandPalette);
        IReadOnlyList<CommandMenuSection> sections = CommandMenuPresenter.Present(open, Localize);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
        Assert.AreEqual(2, sections.Count);
        Assert.AreEqual(@"New: C:\pointer", sections[0].Title);
        Assert.AreEqual(@"Folder: C:\pointer", sections[1].Title);
        Assert.IsFalse(sections.SelectMany(section => section.Rows).Any(row => row.Intent == UserIntent.Copy || row.Intent == UserIntent.Rename));
    }

    /// <summary>Proves row context uses exact rendered ownership and does not interpret an equal foreign projection.</summary>
    [TestMethod]
    public async Task MapContextWhenRowOwnedPreservesSnapshotAndEntryAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePresentation presentation = fixture.PresentRight();
        PanePresentation foreign = fixture.PresentRight();
        PanePointerMapper mapper = new();
        Assert.IsNull(mapper.MapRowContext(PaneSide.Right, presentation, foreign.Rows[1]));
        PaneRowInteraction intent = Assert.IsInstanceOfType<PaneRowInteraction>(mapper.MapRowContext(PaneSide.Right, presentation, presentation.Rows[1]));
        Assert.AreSame(PaneSide.Right, intent.Side);
        PaneSnapshot expected = presentation.SourceSnapshot;
        Assert.AreSame(expected, intent.ExpectedSnapshot);
        Assert.AreSame(presentation.Rows[1].Entry, intent.Entry);
        Assert.AreSame(PaneEntryAction.OpenContextMenu, intent.Action);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(intent)).Scopes.CommandPalette);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
        Assert.AreEqual("Focus: b", CommandMenuPresenter.Present(open, Localize)[0].Title);
    }

    private static string Localize(string key)
    {
        return key switch
        {
            "CommandMenuFocusFormat" => "Focus: {0}",
            "CommandMenuSelectionFormat" => "Selection: {0}",
            "CommandMenuFocusFallbackFormat" => "Focus fallback: {0}",
            "CommandMenuNewFormat" => "New: {0}",
            "CommandMenuFolderFormat" => "Folder: {0}",
            _ => CommandPaletteTestContext.Localize(key),
        };
    }
}
