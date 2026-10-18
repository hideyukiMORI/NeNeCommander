using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves DualPanePresentation behavior and contracts.</summary>
[TestClass]
public sealed class DualPanePresentationTests
{
    /// <summary>Proves each render projection rejects an absent required value and each count rejects a negative value.</summary>
    [TestMethod]
    public async Task PresentationContractsWhenRequiredInputIsAbsentRejectTheCall()
    {
        DualPanePresentation presentation = await DualPaneTestContext.CreateListedPresentation();
        PanePresentation left = presentation.Left;
        PaneRow row = left.Rows[0];

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PaneRow(
            null!, row.Mark, TestMetadataFormats.Utc));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PaneRow(
            row.Entry, null!, TestMetadataFormats.Utc));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PaneRow(
            row.Entry, row.Mark, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => row.WithMark(null!));

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PaneRows(null!, TestMetadataFormats.Utc));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PaneRows([row], null!));
        PaneRows rows = new([row], TestMetadataFormats.Utc);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => rows.Replace(0, null!));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TransferResultDetail(-1, 0, 0, SourceDeletionCounts.Create(0, 0)));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TransferResultDetail(0, -1, 0, SourceDeletionCounts.Create(0, 0)));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TransferResultDetail(0, 0, -1, SourceDeletionCounts.Create(0, 0)));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SourceDeletionCounts.Create(-1, 0));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new OperationItemCountDetail(0));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new OperationProgressDetail(null!));
    }

    /// <summary>
    /// Proves the complete dual-pane projection rejects every absent part, so no render-ready
    /// presentation can reach the shell with a missing pane, frame, status, detail, tone, key
    /// hint set, name entry, conflict modal, or input context.
    /// </summary>
    [TestMethod]
    public async Task ConstructDualPanePresentationWhenAnyPartIsNullThrowsArgumentNullException()
    {
        DualPanePresentation complete = await DualPaneTestContext.CreateListedPresentation();
        PanePairPresentation panes = PanePairPresentation.Create(complete.Left, complete.Right, complete.ActiveSide);
        OperationBarPresentation bar = OperationBarPresentation.Create(
            complete.OperationStatus, complete.Detail, complete.Tone, complete.KeyHints);
        OperationInteractionPresentation interaction = OperationInteractionPresentation.Create(
            complete.NameEntry, complete.ConflictModal, complete.InputContext);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new DualPanePresentation(null!, bar, interaction));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new DualPanePresentation(panes, null!, interaction));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new DualPanePresentation(panes, bar, null!));
    }

    /// <summary>
    /// Proves a keyboard input value rejects every absent translated part, so no event with an
    /// unknown key, modifier, repeat state, or focus context can reach the intent mapper.
    /// </summary>
    [TestMethod]
    public void CreateKeyboardInputWhenAnyPartIsNullThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => KeyboardInput.Create(
            null!,
            KeyboardModifier.None,
            KeyRepeatState.Initial,
            KeyboardContext.FileList));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => KeyboardInput.Create(
            KeyboardKey.J,
            null!,
            KeyRepeatState.Initial,
            KeyboardContext.FileList));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => KeyboardInput.Create(
            KeyboardKey.J,
            KeyboardModifier.None,
            null!,
            KeyboardContext.FileList));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => KeyboardInput.Create(
            KeyboardKey.J,
            KeyboardModifier.None,
            KeyRepeatState.Initial,
            null!));
    }
    /// <summary>Proves the pane pair derives frames from the exact projected panes and guards its inputs.</summary>
    [TestMethod]
    public async Task CreatePanePairWhenInputsAreCompleteRetainsPanesAndTheirFrames()
    {
        DualPanePresentation complete = await DualPaneTestContext.CreateListedPresentation();
        PanePairPresentation pair = PanePairPresentation.Create(complete.Left, complete.Right, complete.ActiveSide);

        Assert.AreSame(complete.Left, pair.Left);
        Assert.AreSame(complete.Right, pair.Right);
        Assert.AreSame(complete.Left.SourceFrame, pair.LeftFrame);
        Assert.AreSame(complete.Right.SourceFrame, pair.RightFrame);
        Assert.AreSame(complete.ActiveSide, pair.ActiveSide);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PanePairPresentation.Create(null!, complete.Right, complete.ActiveSide));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PanePairPresentation.Create(complete.Left, null!, complete.ActiveSide));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PanePairPresentation.Create(complete.Left, complete.Right, null!));
    }

    /// <summary>Proves every required operation bar input is retained and guarded.</summary>
    [TestMethod]
    public async Task CreateOperationBarWhenInputsAreCompleteRetainsValuesAndRejectsAbsentParts()
    {
        DualPanePresentation complete = await DualPaneTestContext.CreateListedPresentation();
        OperationBarPresentation bar = OperationBarPresentation.Create(
            complete.OperationStatus, complete.Detail, complete.Tone, complete.KeyHints);

        Assert.AreSame(complete.OperationStatus, bar.Status);
        Assert.AreSame(complete.Detail, bar.Detail);
        Assert.AreSame(complete.Tone, bar.Tone);
        Assert.AreSame(complete.KeyHints, bar.KeyHints);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationBarPresentation.Create(null!, bar.Detail, bar.Tone, bar.KeyHints));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationBarPresentation.Create(bar.Status, null!, bar.Tone, bar.KeyHints));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationBarPresentation.Create(bar.Status, bar.Detail, null!, bar.KeyHints));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationBarPresentation.Create(bar.Status, bar.Detail, bar.Tone, null!));
    }

    /// <summary>Proves modal projections and keyboard context retain their identities and reject absent inputs.</summary>
    [TestMethod]
    public async Task CreateInteractionWhenInputsAreCompleteRetainsValuesAndRejectsAbsentParts()
    {
        DualPanePresentation complete = await DualPaneTestContext.CreateListedPresentation();
        OperationInteractionPresentation interaction = OperationInteractionPresentation.Create(
            complete.NameEntry, complete.ConflictModal, complete.InputContext);

        Assert.AreSame(complete.NameEntry, interaction.NameEntry);
        Assert.AreSame(complete.ConflictModal, interaction.ConflictModal);
        Assert.AreSame(complete.InputContext, interaction.InputContext);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationInteractionPresentation.Create(null!, interaction.ConflictModal, interaction.InputContext));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationInteractionPresentation.Create(interaction.NameEntry, null!, interaction.InputContext));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => OperationInteractionPresentation.Create(interaction.NameEntry, interaction.ConflictModal, null!));
    }

    /// <summary>Proves copied records retain the projected identities and value equality.</summary>
    [TestMethod]
    public async Task CopyWhenPresentationIsCompletePreservesBindingsAndEquality()
    {
        DualPanePresentation complete = await DualPaneTestContext.CreateListedPresentation();
        DualPanePresentation copy = complete with { };

        Assert.AreEqual(complete, copy);
        Assert.AreSame(complete.Left, copy.Left);
        Assert.AreSame(complete.Right, copy.Right);
        Assert.AreSame(complete.KeyHints, copy.KeyHints);
        Assert.AreSame(complete.NameEntry, copy.NameEntry);
        Assert.AreSame(complete.ConflictModal, copy.ConflictModal);
    }
}
