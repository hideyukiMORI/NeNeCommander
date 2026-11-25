using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves each guarded boundary names the argument its own caller supplied. An unvalidated
/// absent value would otherwise surface as a collaborator's parameter, which hides which input
/// was missing.
/// </summary>
[TestClass]
public sealed class ArgumentGuardParameterTests
{
    /// <summary>Proves the category selection names its absent entry list.</summary>
    [TestMethod]
    public void BookmarkCategorySelectionWhenEntriesAreNullNamesEntries()
    {
        BookmarkCategoryName category = Assert.IsInstanceOfType<BookmarkCategoryNameAccepted>(
            BookmarkCategoryName.Parse("Work")).Name;

        TransferConflictTestContext.AssertNullParameter(
            () => _ = new BookmarkCategorySelection(category, null!),
            "entries");
    }

    /// <summary>Proves the effect summary names each absent collection.</summary>
    [TestMethod]
    public void FileOperationEffectSummaryWhenCollectionIsNullNamesThatCollection()
    {
        TransferConflictTestContext.AssertNullParameter(
            () => _ = FileOperationEffectSummary.Create(null!, [], []),
            "effects");
        TransferConflictTestContext.AssertNullParameter(
            () => _ = FileOperationEffectSummary.Create([], null!, []),
            "notTransferred");
        TransferConflictTestContext.AssertNullParameter(
            () => _ = FileOperationEffectSummary.Create([], [], null!),
            "providerSteps");
    }

    /// <summary>Proves the verified boundary names an absent target ancestor list.</summary>
    [TestMethod]
    public void VerifiedTreeBoundaryWhenTargetIsNullNamesTarget()
    {
        TransferConflictTestContext.AssertNullParameter(
            () => _ = VerifiedTreeBoundary.Create([], null!),
            "target");
    }

    /// <summary>Proves the pane state factory names an absent location.</summary>
    [TestMethod]
    public void PaneStateCreateWhenLocationIsNullNamesLocation()
    {
        VisiblePageCapacity capacity = Assert.IsInstanceOfType<VisiblePageCapacityAccepted>(
            VisiblePageCapacity.Create(2)).Capacity;

        TransferConflictTestContext.AssertNullParameter(
            () => _ = PaneState.Create(null!, Array.Empty<DirectoryEntry>(), capacity, HiddenItemVisibility.Hidden),
            "location");
    }

    /// <summary>Proves a sort toggle names the absent requested key rather than the order factory's key.</summary>
    [TestMethod]
    public void PaneSortOrderToggleWhenRequestedKeyIsNullNamesRequested()
    {
        TransferConflictTestContext.AssertNullParameter(
            () => _ = PaneSortOrder.Default.Toggle(null!),
            "requested");
    }
}
