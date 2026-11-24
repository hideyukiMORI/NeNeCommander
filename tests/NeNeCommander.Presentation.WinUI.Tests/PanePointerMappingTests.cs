using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;
using Windows.UI.Core;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the pointer map preserves exact rendered ownership and its declared gestures.</summary>
[TestClass]
public sealed class PanePointerMappingTests
{
    /// <summary>Supplies the four independently specified row gestures.</summary>
    public static IEnumerable<object[]> RowActions()
    {
        return [
            [PanePointerModifier.None, PaneEntryAction.Select],
            [PanePointerModifier.Control, PaneEntryAction.ToggleSelection],
            [PanePointerModifier.Shift, PaneEntryAction.SelectRange],
            [PanePointerModifier.ControlShift, PaneEntryAction.AddRange],
        ];
    }

    /// <summary>Proves a row request carries the exact projected owner and entry for either side.</summary>
    [TestMethod]
    [DynamicData(nameof(RowActions))]
    public async Task MapRowTapWhenOwnedPreservesOwnerAndExpectedActionAsync(PanePointerModifier modifier, PaneEntryAction expected)
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePresentation presentation = fixture.PresentRight();
        PanePointerMapper mapper = new();
        PaneSnapshot expectedSnapshot = fixture.Session.Current.Panes.Right;

        PaneRowInteraction intent = Assert.IsInstanceOfType<PaneRowInteraction>(
            mapper.MapRowTap(PaneSide.Right, presentation, presentation.Rows[1], modifier));

        Assert.AreSame(PaneSide.Right, intent.Side);
        Assert.AreSame(expectedSnapshot, intent.ExpectedSnapshot);
        Assert.AreSame(presentation.Rows[1].Entry, intent.Entry);
        Assert.AreSame(expected, intent.Action);
    }

    /// <summary>Proves an independently projected row is not accepted by equal entry or value.</summary>
    [TestMethod]
    public async Task MapRowTapWhenForeignProjectionOrUnsupportedModifierHasNoMappingAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePresentation presentation = fixture.PresentRight();
        PanePresentation foreign = fixture.PresentRight();
        PanePointerMapper mapper = new();
        Assert.AreNotSame(presentation.Rows[0], foreign.Rows[0]);

        Assert.IsNull(mapper.MapRowTap(PaneSide.Right, presentation, foreign.Rows[0], PanePointerModifier.None));
        Assert.IsNull(mapper.MapRowTap(PaneSide.Right, presentation, presentation.Rows[0], PanePointerModifier.Other));
    }

    /// <summary>Proves header and background retain distinct semantics and refuse modified clicks.</summary>
    [TestMethod]
    public void MapSurfaceTapWhenPassivePreservesDistinctHeaderAndBlankActions()
    {
        using PanePointerTestContext fixture = new();
        PanePresentation presentation = fixture.PresentRight();
        PanePointerMapper mapper = new();

        PaneSurfaceInteraction header = Assert.IsInstanceOfType<PaneSurfaceInteraction>(
            mapper.MapSurfaceTap(PaneSide.Right, presentation, PanePointerSurface.Header, PanePointerModifier.None));
        PaneSurfaceInteraction blank = Assert.IsInstanceOfType<PaneSurfaceInteraction>(
            mapper.MapSurfaceTap(PaneSide.Right, presentation, PanePointerSurface.ListBackground, PanePointerModifier.None));
        PaneSnapshot expectedSnapshot = fixture.Session.Current.Panes.Right;

        Assert.AreSame(PaneSurfaceAction.Activate, header.Action);
        Assert.AreSame(PaneSurfaceAction.ClearSelection, blank.Action);
        Assert.AreSame(expectedSnapshot, blank.ExpectedSnapshot);
        Assert.IsNull(mapper.MapSurfaceTap(PaneSide.Right, presentation, PanePointerSurface.Header, PanePointerModifier.Shift));
        Assert.IsNull(mapper.MapSurfaceTap(PaneSide.Right, presentation, PanePointerSurface.ListBackground, PanePointerModifier.Control));
    }

    /// <summary>Proves locked-only keys do not act as pressed modifiers and unsupported keys dominate.</summary>
    [TestMethod]
    public void TranslateWhenFrameworkModifierStatesChangeMapsOnlyPressedCombinations()
    {
        Assert.AreSame(PanePointerModifier.None, PanePointerModifier.Translate(CoreVirtualKeyStates.Locked, CoreVirtualKeyStates.Locked, CoreVirtualKeyStates.None, CoreVirtualKeyStates.None));
        Assert.AreSame(PanePointerModifier.Control, PanePointerModifier.Translate(CoreVirtualKeyStates.Down, CoreVirtualKeyStates.None, CoreVirtualKeyStates.None, CoreVirtualKeyStates.None));
        Assert.AreSame(PanePointerModifier.Shift, PanePointerModifier.Translate(CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down, CoreVirtualKeyStates.None, CoreVirtualKeyStates.None));
        Assert.AreSame(PanePointerModifier.ControlShift, PanePointerModifier.Translate(CoreVirtualKeyStates.Down, CoreVirtualKeyStates.Down, CoreVirtualKeyStates.None, CoreVirtualKeyStates.None));
        Assert.AreSame(PanePointerModifier.Other, PanePointerModifier.Translate(CoreVirtualKeyStates.Down, CoreVirtualKeyStates.Down, CoreVirtualKeyStates.Down, CoreVirtualKeyStates.None));
        Assert.AreSame(PanePointerModifier.Other, PanePointerModifier.Translate(CoreVirtualKeyStates.None, CoreVirtualKeyStates.None, CoreVirtualKeyStates.None, CoreVirtualKeyStates.Down));
    }
}
