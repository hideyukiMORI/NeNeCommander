using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the source, header, and row ownership of a single pane projection.</summary>
[TestClass]
public sealed class PanePresentationTests
{
    /// <summary>Proves grouping preserves owned rows, focus, snapshot, frame, and copied record equality.</summary>
    [TestMethod]
    public async Task ConstructWhenInputsAreCompleteRetainsRowAndSourceIdentity()
    {
        PanePresentation original = (await DualPaneTestContext.CreateListedPresentation()).Left;
        PaneProjectionSource source = PaneProjectionSource.Create(original.SourceSnapshot, original.SourceFrame);
        PaneHeaderPresentation header = PaneHeaderPresentation.Create(original.SourceSnapshot);
        PanePresentation presentation = new(original.OwnedRows, original.FocusRow, source, header);
        PanePresentation copy = presentation with { };

        Assert.AreSame(original.Rows, presentation.Rows);
        Assert.AreSame(original.OwnedRows, presentation.OwnedRows);
        Assert.AreSame(original.FocusRow, presentation.FocusRow);
        Assert.AreSame(original.SourceSnapshot, presentation.SourceSnapshot);
        Assert.AreSame(original.SourceFrame, presentation.SourceFrame);
        Assert.AreEqual(original.Status, presentation.Status);
        Assert.AreEqual(original.AddressText, presentation.AddressText);
        Assert.AreEqual(original.SortStatus, presentation.SortStatus);
        Assert.AreEqual(original.LocationStatus, presentation.LocationStatus);
        Assert.AreEqual(presentation, copy);
        Assert.AreSame(presentation.Rows, copy.Rows);
    }

    /// <summary>Proves required pane components and source inputs cannot be absent.</summary>
    [TestMethod]
    public async Task ConstructWhenRequiredPartIsAbsentRejectsTheCall()
    {
        PanePresentation original = (await DualPaneTestContext.CreateListedPresentation()).Left;
        PaneProjectionSource source = PaneProjectionSource.Create(original.SourceSnapshot, original.SourceFrame);
        PaneHeaderPresentation header = PaneHeaderPresentation.Create(original.SourceSnapshot);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PanePresentation(null!, original.FocusRow, source, header));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PanePresentation(original.OwnedRows, original.FocusRow, null!, header));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new PanePresentation(original.OwnedRows, original.FocusRow, source, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneProjectionSource.Create(null!, original.SourceFrame));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneProjectionSource.Create(original.SourceSnapshot, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => PaneHeaderPresentation.Create(null!));
    }
}
