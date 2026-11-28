using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves exact consent details, eligible-set disclosure and presentation refresh without changing the owner.</summary>
[TestClass]
public sealed class ConflictModalTextTests
{
    /// <summary>Qualified confirmation shows original facts and only the other eligible original pairs.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenConflictsAreMixedShowsOnlyEligibleSetWithOriginalFacts()
    {
        TransferConflict first = ConflictModalFixture.Conflict("first", null);
        TransferConflict unsupported = ConflictModalFixture.Conflict("unsupported", TransferOverwriteUnavailableReason.Folder);
        TransferConflict last = ConflictModalFixture.Conflict("last", null);
        DualPaneSnapshot snapshot = await ConflictModalFixture.AwaitAsync([first, unsupported, last]);
        ActiveConflictModal active = Assert.IsInstanceOfType<ActiveConflictModal>(
            DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).ConflictModal);

        ConflictModalText text = ConflictModalText.Create(active, ConflictModalFixture.Localize);

        Assert.IsTrue(active.CanOverwrite);
        Assert.IsTrue(active.CanOverwriteAll);
        Assert.AreSame(TransferConflictDecision.Cancel, active.InitialFocus);
        Assert.HasCount(1, active.OtherOverwriteConflicts);
        Assert.AreSame(last, active.OtherOverwriteConflicts[0]);
        Assert.AreEqual("Source: C:\\left\\first\nFile · 17 B · Modified: 2026-11-25 00:00", text.SourceText);
        Assert.AreEqual("Destination: D:\\right\\first\nFile · 17 B · Modified: 2026-11-25 00:00", text.TargetText);
        Assert.AreEqual("Overwrite contents of D:\\right\\first", text.OverwriteActionName);
        Assert.AreEqual("Also overwrite the 1 other eligible files listed below", text.OverwriteScopeText);
        Assert.HasCount(1, text.OtherOverwriteRows);
        Assert.AreEqual("Source: C:\\left\\last\nFile · 17 B · Modified: 2026-11-25 00:00", text.OtherOverwriteRows[0].SourceText);
        Assert.AreEqual(string.Empty, text.UnavailableText);
    }

    /// <summary>A changed metadata format refreshes details while retaining the exact confirmation state and original object.</summary>
    [TestMethod]
    public async Task PresentAsyncWhenFormatChangesRefreshesTextWithoutReplacingConsentOwner()
    {
        DualPaneSnapshot snapshot = await ConflictModalFixture.AwaitAsync([ConflictModalFixture.Conflict("first", null)]);
        DualPanePresentation first = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown);
        DualPanePresentation repeated = DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, first, KeyHintVisibility.Shown);
        EntryMetadataFormat nextFormat = TestMetadataFormats.Create(TestMetadataFormats.PlusNine);

        DualPanePresentation changed = DualPanePresenter.Present(snapshot, nextFormat, repeated, KeyHintVisibility.Shown);
        ActiveConflictModal active = Assert.IsInstanceOfType<ActiveConflictModal>(changed.ConflictModal);
        ConflictModalText text = ConflictModalText.Create(active, ConflictModalFixture.Localize);

        Assert.AreSame(first.ConflictModal, repeated.ConflictModal);
        Assert.AreNotSame(first.ConflictModal, active);
        Assert.AreSame(((ActiveConflictModal)first.ConflictModal).SourceState, active.SourceState);
        Assert.AreSame(((ActiveConflictModal)first.ConflictModal).Overwrite, active.Overwrite);
        Assert.IsFalse(active.CanOverwriteAll);
        Assert.AreEqual("Destination: D:\\right\\first\nFile · 17 B · Modified: 2026-11-25 09:00", text.TargetText);
    }

    /// <summary>Unavailable first rows cannot expose overwrite-all even when another row is eligible.</summary>
    [TestMethod]
    [DataRow("Folder")]
    [DataRow("UnsupportedLocation")]
    [DataRow("SameObject")]
    [DataRow("MultipleLinks")]
    [DataRow("SpecialAttributes")]
    [DataRow("StreamsUnconfirmed")]
    [DataRow("ReservedTarget")]
    [DataRow("Unconfirmed")]
    public async Task CreateAsyncWhenCurrentCapabilityIsUnavailableShowsItsReasonWithoutDestructiveScope(string kind)
    {
        TransferOverwriteUnavailableReason reason = Reason(kind);
        DualPaneSnapshot snapshot = await ConflictModalFixture.AwaitAsync([
            ConflictModalFixture.Conflict("first", reason), ConflictModalFixture.Conflict("last", null)]);
        ActiveConflictModal active = Assert.IsInstanceOfType<ActiveConflictModal>(
            DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).ConflictModal);

        ConflictModalText text = ConflictModalText.Create(active, ConflictModalFixture.Localize);

        Assert.IsFalse(active.CanOverwrite);
        Assert.IsFalse(active.CanOverwriteAll);
        Assert.IsEmpty(active.OtherOverwriteConflicts);
        Assert.IsEmpty(text.OtherOverwriteRows);
        Assert.AreEqual(ReasonText(kind), text.UnavailableText);
        Assert.AreEqual("Source: C:\\left\\first", text.SourceText);
        Assert.AreEqual("Destination: D:\\right\\first", text.TargetText);
    }

    /// <summary>Absent display inputs are rejected instead of producing incomplete consent text.</summary>
    [TestMethod]
    public async Task CreateAsyncWhenRequiredInputsAreAbsentRejectsThem()
    {
        DualPaneSnapshot snapshot = await ConflictModalFixture.AwaitAsync([ConflictModalFixture.Conflict("first", null)]);
        ActiveConflictModal active = Assert.IsInstanceOfType<ActiveConflictModal>(
            DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).ConflictModal);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ConflictModalText.Create(null!, ConflictModalFixture.Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => ConflictModalText.Create(active, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ActiveConflictModal(active.SourceState, null!));
    }

    private static TransferOverwriteUnavailableReason Reason(string kind)
    {
        return kind switch
        {
            "Folder" => TransferOverwriteUnavailableReason.Folder,
            "UnsupportedLocation" => TransferOverwriteUnavailableReason.UnsupportedLocation,
            "SameObject" => TransferOverwriteUnavailableReason.SameObject,
            "MultipleLinks" => TransferOverwriteUnavailableReason.MultipleLinks,
            "SpecialAttributes" => TransferOverwriteUnavailableReason.SpecialAttributes,
            "StreamsUnconfirmed" => TransferOverwriteUnavailableReason.StreamsUnconfirmed,
            "ReservedTarget" => TransferOverwriteUnavailableReason.ReservedTarget,
            "Unconfirmed" => TransferOverwriteUnavailableReason.Unconfirmed,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static string ReasonText(string kind)
    {
        return kind switch
        {
            "Folder" => "Contents overwrite is unavailable for folders.",
            "UnsupportedLocation" => "Contents overwrite requires files on a supported local NTFS drive.",
            "SameObject" => "Source and destination refer to the same file. Contents overwrite is unavailable.",
            "MultipleLinks" => "A file has multiple hardlinks. Contents overwrite is unavailable.",
            "SpecialAttributes" => "A file or folder has unsupported attributes or a link. Contents overwrite is unavailable.",
            "StreamsUnconfirmed" => "The file could not be confirmed to have only its ordinary contents. Additional data streams are not supported.",
            "ReservedTarget" => "Another selected item reserves this destination. No existing file has been confirmed for overwrite.",
            "Unconfirmed" => "The required file conditions could not be confirmed. Contents overwrite is unavailable.",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}
