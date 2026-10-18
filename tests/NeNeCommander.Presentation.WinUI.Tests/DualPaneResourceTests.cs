using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPaneResource behavior and contracts.</summary>
[TestClass]
public sealed class DualPaneResourceTests
{
    /// <summary>Proves every operation-bar tone names distinct semantic design resources.</summary>
    [TestMethod]
    public void ResourceKeysWhenOperationBarToneIsReadNameExactSemanticResources()
    {
        Assert.AreEqual("SurfacePaneBrush", OperationBarTone.Idle.SurfaceBrushResourceKey);
        Assert.AreEqual("TextPrimaryBrush", OperationBarTone.Idle.ForegroundBrushResourceKey);
        Assert.AreEqual("BorderSubtleBrush", OperationBarTone.Idle.BorderBrushResourceKey);
        Assert.AreSame(OperationBarIcon.None, OperationBarTone.Idle.Icon);
        Assert.AreEqual("FocusSurfaceBrush", OperationBarTone.AwaitingName.SurfaceBrushResourceKey);
        Assert.AreEqual("TextPrimaryBrush", OperationBarTone.AwaitingName.ForegroundBrushResourceKey);
        Assert.AreEqual("FocusRingBrush", OperationBarTone.AwaitingName.BorderBrushResourceKey);
        Assert.AreSame(OperationBarIcon.NameEntry, OperationBarTone.AwaitingName.Icon);
        Assert.AreEqual("StatusWarningSurfaceBrush", OperationBarTone.AwaitingConfirmation.SurfaceBrushResourceKey);
        Assert.AreEqual("StatusWarningBrush", OperationBarTone.AwaitingConfirmation.ForegroundBrushResourceKey);
        Assert.AreEqual("StatusWarningBrush", OperationBarTone.AwaitingConfirmation.BorderBrushResourceKey);
        Assert.AreSame(OperationBarIcon.Warning, OperationBarTone.AwaitingConfirmation.Icon);
        Assert.AreEqual("StatusDangerSurfaceBrush", OperationBarTone.Failure.SurfaceBrushResourceKey);
        Assert.AreEqual("StatusDangerBrush", OperationBarTone.Failure.ForegroundBrushResourceKey);
        Assert.AreEqual("StatusDangerBrush", OperationBarTone.Failure.BorderBrushResourceKey);
        Assert.AreSame(OperationBarIcon.Warning, OperationBarTone.Failure.Icon);
        Assert.AreEqual("OperationProgressBrush", ProgressSegment.Filled.BrushResourceKey);
        Assert.AreEqual("OperationTrackBrush", ProgressSegment.Empty.BrushResourceKey);
    }

    /// <summary>Proves each frame names distinct semantic border resources.</summary>
    [TestMethod]
    public void ResourceKeysWhenFrameIsReadNameExactSemanticResources()
    {
        Assert.AreEqual("FocusRingBrush", PaneFrame.Active.BrushResourceKey);
        Assert.AreEqual("BorderActivePaneThickness", PaneFrame.Active.ThicknessResourceKey);
        Assert.AreEqual("BorderSubtleBrush", PaneFrame.Passive.BrushResourceKey);
        Assert.AreEqual("BorderPassivePaneThickness", PaneFrame.Passive.ThicknessResourceKey);
    }

    /// <summary>Proves every operation status names a distinct localization resource key.</summary>
    [TestMethod]
    public void ResourceKeyWhenOperationStatusIsReadNamesExactResource()
    {
        Assert.AreEqual("OperationStatusIdle", OperationStatus.Idle.ResourceKey);
        Assert.AreEqual("OperationStatusMoving", OperationStatus.Moving.ResourceKey);
        Assert.AreEqual("OperationStatusMoveSucceeded", OperationStatus.MoveSucceeded.ResourceKey);
        Assert.AreEqual("OperationStatusMoveCancelled", OperationStatus.MoveCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusMovePartiallyCompleted", OperationStatus.MovePartiallyCompleted.ResourceKey);
        Assert.AreEqual("OperationStatusMoveRejected", OperationStatus.MoveRejected.ResourceKey);
        Assert.AreEqual("OperationStatusMoveRequestRejected", OperationStatus.MoveRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusMoveAwaitingConflict", OperationStatus.MoveAwaitingConflict.ResourceKey);
        Assert.AreEqual("OperationStatusCopying", OperationStatus.Copying.ResourceKey);
        Assert.AreEqual("OperationStatusCopySucceeded", OperationStatus.CopySucceeded.ResourceKey);
        Assert.AreEqual("OperationStatusCopyCancelled", OperationStatus.CopyCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusCopyPartiallyCompleted", OperationStatus.CopyPartiallyCompleted.ResourceKey);
        Assert.AreEqual("OperationStatusCopyRejected", OperationStatus.CopyRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCopyRequestRejected", OperationStatus.CopyRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCopyAwaitingConflict", OperationStatus.CopyAwaitingConflict.ResourceKey);
        Assert.AreEqual("OperationStatusCreatingDirectory", OperationStatus.CreatingDirectory.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryAwaitingName", OperationStatus.CreateDirectoryAwaitingName.ResourceKey);
        Assert.AreEqual("OperationStatusDirectoryCreated", OperationStatus.DirectoryCreated.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryCancelled", OperationStatus.CreateDirectoryCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryRejected", OperationStatus.CreateDirectoryRejected.ResourceKey);
        Assert.AreEqual("OperationStatusCreateDirectoryRequestRejected", OperationStatus.CreateDirectoryRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusRenaming", OperationStatus.Renaming.ResourceKey);
        Assert.AreEqual("OperationStatusRenameAwaitingName", OperationStatus.RenameAwaitingName.ResourceKey);
        Assert.AreEqual("OperationStatusRenamed", OperationStatus.Renamed.ResourceKey);
        Assert.AreEqual("OperationStatusRenameCancelled", OperationStatus.RenameCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusRenameRejected", OperationStatus.RenameRejected.ResourceKey);
        Assert.AreEqual("OperationStatusRenameRequestRejected", OperationStatus.RenameRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusDeleting", OperationStatus.Deleting.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteAwaitingConfirmation", OperationStatus.DeleteAwaitingConfirmation.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteSucceeded", OperationStatus.DeleteSucceeded.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteCancelled", OperationStatus.DeleteCancelled.ResourceKey);
        Assert.AreEqual("OperationStatusDeletePartiallyCompleted", OperationStatus.DeletePartiallyCompleted.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteRejected", OperationStatus.DeleteRejected.ResourceKey);
        Assert.AreEqual("OperationStatusDeleteRequestRejected", OperationStatus.DeleteRequestRejected.ResourceKey);
        Assert.AreEqual("OperationStatusMoveAwaitingConflict", OperationStatus.MoveAwaitingConflict.ResourceKey);
        Assert.AreEqual("OperationStatusCopyAwaitingConflict", OperationStatus.CopyAwaitingConflict.ResourceKey);
    }
}
