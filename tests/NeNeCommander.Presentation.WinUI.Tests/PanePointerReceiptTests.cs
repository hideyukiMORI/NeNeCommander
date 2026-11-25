using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves double tap depends on actual admitted Application transitions, never native marks.</summary>
[TestClass]
public sealed class PanePointerReceiptTests
{
    /// <summary>Proves an accepted tap survives row replacement and is consumed by exactly one open.</summary>
    [TestMethod]
    public async Task MapRowDoubleTapWhenFirstTapAcceptedMapsCurrentOwnerOnceAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        PanePresentation original = fixture.PresentRight();
        await AcceptTapAsync(fixture, mapper);
        PanePresentation current = fixture.PresentRight();
        PaneSnapshot expectedSnapshot = fixture.Session.Current.Panes.Right;
        Assert.AreNotSame(original.Rows[1], current.Rows[1]);

        PaneRowInteraction intent = Assert.IsInstanceOfType<PaneRowInteraction>(
            mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.None));

        Assert.AreSame(PaneEntryAction.Open, intent.Action);
        Assert.AreSame(expectedSnapshot, intent.ExpectedSnapshot);
        Assert.AreSame(original.Rows[1].Entry, intent.Entry);
        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.None));
    }

    /// <summary>Proves an unadmitted first tap has no receipt regardless of the native focused row.</summary>
    [TestMethod]
    public async Task MapRowDoubleTapWhenFirstTapNotHandledHasNoMappingAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        PanePresentation current = fixture.PresentRight();
        _ = mapper.MapRowTap(PaneSide.Right, current, current.Rows[0], PanePointerModifier.None);

        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[0], PanePointerModifier.None));
    }

    /// <summary>Proves a stale request cannot mistake the newer same-focus snapshot for acceptance.</summary>
    [TestMethod]
    public async Task ObserveResultWhenStaleOwnerStillFocusesSameEntryDoesNotSeedCandidateAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        PanePointerMapper mapper = new();
        PanePresentation stale = fixture.PresentRight();
        UserIntent intent = Assert.IsInstanceOfType<PaneRowInteraction>(
            mapper.MapRowTap(PaneSide.Right, stale, stale.Rows[0], PanePointerModifier.None));
        _ = await fixture.HandleAsync(UserIntent.ToggleSelection);
        CommanderSnapshot before = fixture.Session.Current;
        Assert.AreNotSame(stale.SourceSnapshot, before.Panes.Right);
        CommanderSnapshot after = await fixture.HandleAsync(intent);

        _ = mapper.ObserveResult(intent, before, after);
        PanePresentation current = fixture.PresentRight();

        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[0], PanePointerModifier.None));
    }

    /// <summary>Proves a modified second tap consumes the candidate rather than deferring a later open.</summary>
    [TestMethod]
    public async Task MapRowDoubleTapWhenModifiedConsumesCandidateWithoutOpeningAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        await AcceptTapAsync(fixture, mapper);
        PanePresentation current = fixture.PresentRight();

        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.Control));
        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.None));
    }

    /// <summary>Proves another row cannot use the first row's accepted gesture.</summary>
    [TestMethod]
    public async Task MapRowDoubleTapWhenTargetDiffersConsumesCandidateWithoutOpeningAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        await AcceptTapAsync(fixture, mapper);
        PanePresentation current = fixture.PresentRight();

        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[0], PanePointerModifier.None));
        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.None));
    }

    /// <summary>Proves modal input ownership invalidates an accepted first tap before a second one.</summary>
    [TestMethod]
    public async Task ObserveSessionWhenSettingsOpenExpiresCandidateAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        await AcceptTapAsync(fixture, mapper);
        CommanderSnapshot modal = await fixture.HandleAsync(UserIntent.OpenSettings);

        mapper.ObserveSession(modal);
        _ = await fixture.HandleAsync(UserIntent.Escape);
        PanePresentation current = fixture.PresentRight();

        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.None));
    }

    /// <summary>Proves a fresh listing with identical names invalidates the previous entry identities.</summary>
    [TestMethod]
    public async Task ObserveSessionWhenListingReplacedExpiresCandidateAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        await AcceptTapAsync(fixture, mapper);
        await fixture.LoadAsync();

        mapper.ObserveSession(fixture.Session.Current);
        PanePresentation current = fixture.PresentRight();

        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, current, current.Rows[1], PanePointerModifier.None));
    }

    private static async Task AcceptTapAsync(PanePointerTestContext fixture, PanePointerMapper mapper)
    {
        PanePresentation original = fixture.PresentRight();
        UserIntent intent = Assert.IsInstanceOfType<PaneRowInteraction>(
            mapper.MapRowTap(PaneSide.Right, original, original.Rows[1], PanePointerModifier.None));
        CommanderSnapshot before = fixture.Session.Current;
        CommanderSnapshot after = await fixture.HandleAsync(intent);
        _ = mapper.ObserveResult(intent, before, after);
    }
}
