using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves PaneResource behavior and contracts.</summary>
[TestClass]
public sealed class PaneResourceTests
{
    /// <summary>Proves the visibility projection rejects an absent entry visibility.</summary>
    [TestMethod]
    public void ForWhenEntryVisibilityIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(PaneRowVisibility).GetMethod(
            nameof(PaneRowVisibility.For),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The visibility projection was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    /// <summary>Proves every row mark and entry kind names the exact semantic design resources.</summary>
    [TestMethod]
    public void ResourceKeyWhenRowMarkOrKindIsReadNamesExactResources()
    {
        Assert.AreEqual("FocusRingBrush", PaneRowMark.FocusInActivePane.MarkerBrushResourceKey);
        Assert.AreEqual("FocusSurfaceBrush", PaneRowMark.FocusInActivePane.SurfaceBrushResourceKey);
        Assert.AreEqual("SelectionMarkBrush", PaneRowMark.Selected.MarkerBrushResourceKey);
        Assert.AreEqual("SelectionSurfaceBrush", PaneRowMark.Selected.SurfaceBrushResourceKey);
        Assert.AreEqual("BorderSubtleBrush", PaneRowMark.FocusInPassivePane.MarkerBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneRowMark.FocusInPassivePane.SurfaceBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneRowMark.Unmarked.MarkerBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneRowMark.Unmarked.SurfaceBrushResourceKey);
        Assert.AreEqual("EntryKindDirectoryLabel", PaneRowKind.Directory.LabelResourceKey);
        Assert.IsTrue(PaneRowKind.Directory.IsDirectory);
        Assert.IsFalse(PaneRowKind.Directory.IsFile);
        Assert.AreEqual("EntryKindFileLabel", PaneRowKind.File.LabelResourceKey);
        Assert.IsFalse(PaneRowKind.File.IsDirectory);
        Assert.IsTrue(PaneRowKind.File.IsFile);
        Assert.AreSame(PaneRowKind.Directory, PaneRowKind.For(DirectoryEntryKind.Directory));
        Assert.AreSame(PaneRowKind.File, PaneRowKind.For(DirectoryEntryKind.File));
        Assert.AreEqual("TextPrimaryBrush", PaneRowVisibility.Normal.NameBrushResourceKey);
        Assert.AreEqual("TextHiddenBrush", PaneRowVisibility.Hidden.NameBrushResourceKey);
        Assert.AreSame(PaneRowVisibility.Normal, PaneRowVisibility.For(EntryVisibility.Normal));
        Assert.AreSame(PaneRowVisibility.Hidden, PaneRowVisibility.For(EntryVisibility.Hidden));
        Assert.AreEqual("FocusRingBrush", PaneFrame.Active.NumberSurfaceBrushResourceKey);
        Assert.AreEqual("SurfacePaneBrush", PaneFrame.Active.NumberForegroundBrushResourceKey);
        Assert.AreEqual("BorderSubtleBrush", PaneFrame.Passive.NumberSurfaceBrushResourceKey);
        Assert.AreEqual("TextSecondaryBrush", PaneFrame.Passive.NumberForegroundBrushResourceKey);
    }

    /// <summary>Proves the presenter rejects an absent snapshot.</summary>
    [TestMethod]
    public void PresentWhenSnapshotIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(PaneListingPresenter).GetMethod(
            nameof(PaneListingPresenter.Present),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The present method was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null, PaneFrame.Active, TestMetadataFormats.Utc]));
        TargetInvocationException absentFrame = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [PaneSnapshot.Initial, null, TestMetadataFormats.Utc]));
        TargetInvocationException absentFormat = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [PaneSnapshot.Initial, PaneFrame.Active, null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
        _ = Assert.IsInstanceOfType<ArgumentNullException>(absentFrame.InnerException);
        _ = Assert.IsInstanceOfType<ArgumentNullException>(absentFormat.InnerException);
    }

    /// <summary>Proves the kind projection rejects an absent entry kind.</summary>
    [TestMethod]
    public void ForWhenEntryKindIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(PaneRowKind).GetMethod(
            nameof(PaneRowKind.For),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The kind projection was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    /// <summary>Proves every status names a distinct localization resource key.</summary>
    [TestMethod]
    public void ResourceKeyWhenStatusIsReadNamesExactResource()
    {
        Assert.AreEqual("PaneStatusNoListing", PaneStatus.NoListing.ResourceKey);
        Assert.AreEqual("PaneStatusLoading", PaneStatus.Loading.ResourceKey);
        Assert.AreEqual("PaneStatusComplete", PaneStatus.Complete.ResourceKey);
        Assert.AreEqual("PaneStatusBounded", PaneStatus.Bounded.ResourceKey);
        Assert.AreEqual("PaneStatusEntriesOmitted", PaneStatus.EntriesOmitted.ResourceKey);
        Assert.AreEqual("PaneStatusAccessDenied", PaneStatus.AccessDenied.ResourceKey);
        Assert.AreEqual("PaneStatusNotFound", PaneStatus.NotFound.ResourceKey);
        Assert.AreEqual("PaneStatusProviderUnavailable", PaneStatus.ProviderUnavailable.ResourceKey);
        Assert.AreEqual("PaneStatusCancelled", PaneStatus.Cancelled.ResourceKey);
        Assert.AreEqual("PaneStatusReadAbandoned", PaneStatus.ReadAbandoned.ResourceKey);
        Assert.AreEqual("PaneStatusLaunching", PaneStatus.Launching.ResourceKey);
        Assert.AreEqual("PaneStatusLaunchCancelled", PaneStatus.LaunchCancelled.ResourceKey);
        Assert.AreEqual("PaneStatusLaunchNotFound", PaneStatus.LaunchNotFound.ResourceKey);
        Assert.AreEqual("PaneStatusLaunchAccessDenied", PaneStatus.LaunchAccessDenied.ResourceKey);
        Assert.AreEqual("PaneStatusAssociationUnavailable", PaneStatus.AssociationUnavailable.ResourceKey);
        Assert.AreEqual("PaneStatusLaunchUnavailable", PaneStatus.LaunchUnavailable.ResourceKey);
        Assert.AreEqual("PaneStatusInvalidAddress", PaneStatus.InvalidAddress.ResourceKey);
    }
}
