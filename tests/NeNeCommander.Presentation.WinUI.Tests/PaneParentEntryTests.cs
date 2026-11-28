using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves parent-button mapping, readiness and focus receipts stay with the canonical owners.</summary>
[TestClass]
public sealed class PaneParentEntryTests
{
    /// <summary>Proves parent clicks carry the rendered right owner, expire double tap and never return synchronous focus.</summary>
    [TestMethod]
    public async Task MapParentNavigationPreservesOwnerAndExpiresDoubleTapAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        PanePointerMapper mapper = new();
        PanePresentation first = fixture.PresentRight();
        UserIntent selected = Assert.IsInstanceOfType<UserIntent>(mapper.MapRowTap(PaneSide.Right, first, first.Rows[0], PanePointerModifier.None));
        CommanderSnapshot beforeSelection = fixture.Session.Current;
        CommanderSnapshot selectedSnapshot = await fixture.HandleAsync(selected);
        Assert.AreSame(PaneSide.Right, mapper.ObserveResult(selected, beforeSelection, selectedSnapshot));
        PanePresentation rendered = fixture.PresentRight();
        PaneSurfaceInteraction parent = Assert.IsInstanceOfType<PaneSurfaceInteraction>(mapper.MapParentNavigation(PaneSide.Right, rendered));
        Assert.AreSame(PaneSide.Right, parent.Side);
        PaneSnapshot expectedSnapshot = selectedSnapshot.Panes.Right;
        Assert.AreSame(expectedSnapshot, parent.ExpectedSnapshot);
        Assert.AreSame(PaneSurfaceAction.NavigateParent, parent.Action);
        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, rendered, rendered.Rows[0], PanePointerModifier.None));
        fixture.Right.Enqueue(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing(@"C:\", ["pointer"]), null));

        CommanderSnapshot after = await fixture.HandleAsync(parent);

        Assert.IsNull(mapper.ObserveResult(parent, selectedSnapshot, after));
        Assert.IsNull(mapper.MapRowDoubleTap(PaneSide.Right, fixture.PresentRight(), fixture.PresentRight().Rows[0], PanePointerModifier.None));
    }

    /// <summary>Proves root and unlisted panes disable only their own parent action; passive nested panes stay enabled.</summary>
    [TestMethod]
    public async Task CreateWhenPaneIsRootOrAbsentDisablesParentIndependentlyAsync()
    {
        using PanePointerTestContext fixture = new();
        Assert.IsFalse(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Right).IsParentEnabled);
        await fixture.LoadAsync();
        Assert.IsTrue(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Right).IsParentEnabled);
        DirectoryListing root = DualPaneTestContext.CreateListing(@"C:\", []);
        fixture.Left.Enqueue(DirectoryReadOutcome.Succeeded(root, null));
        _ = await fixture.Session.NavigateAsync(PaneSide.Left, root.Location, CancellationToken.None);

        Assert.IsFalse(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Left).IsParentEnabled);
        Assert.IsTrue(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Right).IsParentEnabled);
    }

    /// <summary>Proves current readiness refreshes across pending work and name-modal boundaries.</summary>
    [TestMethod]
    public async Task CreateWhenAnyReadOrOperationOwnsInputDisablesBothParentsAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        TaskCompletionSource<DirectoryReadOutcome> release = fixture.Right.EnqueuePendingInline();
        Task<CommanderSnapshot> reading = fixture.Session.NavigateAsync(PaneSide.Right, DualPaneTestContext.ParsePath(@"C:\pending"), CancellationToken.None);
        Assert.IsFalse(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Left).IsParentEnabled);
        Assert.IsFalse(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Right).IsParentEnabled);
        release.SetResult(DirectoryReadOutcome.Cancelled());
        _ = await reading;
        Assert.IsTrue(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Right).IsParentEnabled);
        _ = await fixture.HandleAsync(UserIntent.CreateDirectory);
        Assert.IsFalse(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Left).IsParentEnabled);
        Assert.IsFalse(PaneNavigationAction.Create(fixture.Session.Current.Panes, PaneSide.Right).IsParentEnabled);
    }

    /// <summary>Proves the complete tooltip resolves its exact name and canonical Alt+Up declaration.</summary>
    [TestMethod]
    public void ComposeParentNameUsesLocalizedCommandAndCanonicalShortcut()
    {
        Assert.AreEqual("Go to parent folder (Alt+↑)", PaneNavigationAction.ComposeParentName(PresentationResourceCatalog.English.Get));
    }

    /// <summary>Proves missing mapping or localization inputs fail at their declared boundary.</summary>
    [TestMethod]
    public void ParentEntryWhenBoundaryArgumentIsAbsentThrows()
    {
        using PanePointerTestContext fixture = new();
        PanePointerMapper mapper = new();
        MethodInfo map = typeof(PanePointerMapper).GetMethod(nameof(PanePointerMapper.MapParentNavigation)) ?? throw new AssertFailedException("Missing map.");
        MethodInfo name = typeof(PaneNavigationAction).GetMethod(nameof(PaneNavigationAction.ComposeParentName)) ?? throw new AssertFailedException("Missing name.");
        _ = Assert.IsInstanceOfType<ArgumentNullException>(Assert.ThrowsExactly<TargetInvocationException>(() => map.Invoke(mapper, [null, fixture.PresentRight()])).InnerException);
        _ = Assert.IsInstanceOfType<ArgumentNullException>(Assert.ThrowsExactly<TargetInvocationException>(() => map.Invoke(mapper, [PaneSide.Left, null])).InnerException);
        _ = Assert.IsInstanceOfType<ArgumentNullException>(Assert.ThrowsExactly<TargetInvocationException>(() => name.Invoke(null, [null])).InnerException);
    }
}
