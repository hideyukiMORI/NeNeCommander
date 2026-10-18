using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves complete immutable palette row construction and copying.</summary>
[TestClass]
public sealed class CommandPaletteRowTests
{
    /// <summary>Proves a row cannot lose its candidate or any captured display responsibility.</summary>
    [TestMethod]
    public async Task ConstructWhenAnyCapturedPartIsNullThrowsAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandCandidate candidate = open.Candidates[0];
        CommandPaletteCaption caption = CommandPaletteCaption.Create(candidate.Intent,
            KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList), CommandPaletteTestContext.ProjectResource);
        CommandPaletteContext context = CreateContext();
        CommandPaletteFeedback feedback = CommandPaletteFeedback.Create(candidate.Availability,
            caption, context, CommandPaletteTestContext.ProjectResource);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new CommandPaletteRow(null!, caption, context, feedback));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new CommandPaletteRow(candidate, null!, context, feedback));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new CommandPaletteRow(candidate, caption, null!, feedback));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new CommandPaletteRow(candidate, caption, context, null!));
    }

    /// <summary>Proves caption construction requires the canonical intent, bindings and localization.</summary>
    [TestMethod]
    public void CreateCaptionWhenRequiredInputIsNullThrows()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteCaption.Create(null!,
            KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList), CommandPaletteTestContext.ProjectResource));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteCaption.Create(UserIntent.Refresh,
            null!, CommandPaletteTestContext.ProjectResource));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteCaption.Create(UserIntent.Refresh,
            KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList), null!));
    }

    /// <summary>Proves context construction requires a captured side and localization.</summary>
    [TestMethod]
    public void CreateContextWhenRequiredInputIsNullThrows()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteContext.Create(null!, CommandPaletteTestContext.ProjectResource));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteContext.Create(PaneSide.Left, null!));
    }

    /// <summary>Proves feedback construction cannot omit captured availability or display inputs.</summary>
    [TestMethod]
    public void CreateFeedbackWhenRequiredInputIsNullThrows()
    {
        CommandPaletteCaption caption = CommandPaletteCaption.Create(UserIntent.Refresh,
            KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList), CommandPaletteTestContext.ProjectResource);
        CommandPaletteContext context = CreateContext();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteFeedback.Create(null!, caption, context, CommandPaletteTestContext.ProjectResource));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteFeedback.Create(CommandAvailability.Available, null!, context, CommandPaletteTestContext.ProjectResource));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteFeedback.Create(CommandAvailability.Available, caption, null!, CommandPaletteTestContext.ProjectResource));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CommandPaletteFeedback.Create(CommandAvailability.Available, caption, context, null!));
    }

    /// <summary>Proves grouping display state retains record value equality and captured candidate identity.</summary>
    [TestMethod]
    public async Task WithWhenCopyingRowPreservesValueAndCandidateIdentityAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteRow row = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource).Rows[0];

        CommandPaletteRow copy = row with { };
        CommandPaletteRow equivalent = CommandPalettePresenter.Present(open, CommandPaletteTestContext.ProjectResource).Rows[0];

        Assert.AreNotSame(row, copy);
        Assert.AreEqual(row, copy);
        Assert.AreEqual(row, equivalent);
        Assert.AreEqual(row.GetHashCode(), copy.GetHashCode());
        Assert.AreSame(row.Candidate, copy.Candidate);
        Assert.AreEqual(row.AutomationName, copy.AutomationName);
        Assert.AreEqual(row.AutomationId, copy.AutomationId);
    }

    private static CommandPaletteContext CreateContext()
    {
        return CommandPaletteContext.Create(PaneSide.Left, CommandPaletteTestContext.ProjectResource);
    }
}
