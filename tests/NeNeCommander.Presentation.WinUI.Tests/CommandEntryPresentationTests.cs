using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves command-entry appearance uses the existing owners, labels and shortcuts.</summary>
[TestClass]
public sealed class CommandEntryPresentationTests
{
    /// <summary>Proves the three buttons retain existing command identity, order and key-map labels.</summary>
    [TestMethod]
    public async Task PresentWhenIdleUsesExistingCommandAndShortcutCatalogsAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();

        CommandEntryPresentation presentation = CommandEntryPresenter.Present(fixture.Session.Current);

        Assert.IsTrue(presentation.IsEnabled);
        CollectionAssert.AreEqual(new[] { UserIntent.OpenBookmarks, UserIntent.OpenSettings, UserIntent.OpenHelp },
            presentation.Buttons.Select(button => button.Intent).ToArray());
        foreach (CommandEntryButton button in presentation.Buttons)
        {
            string expectedLabel = CommandLabelCatalog.LabelFor(button.Intent).ResourceKey;
            string expectedShortcut = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
                .First(binding => binding.Intent == button.Intent).KeyLabelResourceKey;
            Assert.AreEqual(expectedLabel, button.LabelResourceKey);
            Assert.AreEqual(expectedShortcut, button.ShortcutResourceKey);
            Assert.AreEqual(expectedLabel + " [" + expectedShortcut + "]",
                CommandEntryPresenter.ToolTipFor(button, key => key == "CommandEntryToolTipFormat" ? "{0} [{1}]" : key));
        }
    }

    /// <summary>Proves initial empty panes allow access to the three application-level surfaces.</summary>
    [TestMethod]
    public void PresentWhenNoListingExistsKeepsCommandEntryAvailable()
    {
        using PanePointerTestContext fixture = new();

        Assert.IsTrue(CommandEntryPresenter.Present(fixture.Session.Current).IsEnabled);
    }

    /// <summary>Proves each button's existing surface disables background entry until it closes.</summary>
    [TestMethod]
    public async Task PresentWhenExistingSurfaceOpensDisablesUntilItsOwnerClosesAsync()
    {
        UserIntent[] commands = [UserIntent.OpenBookmarks, UserIntent.OpenSettings, UserIntent.OpenHelp];
        foreach (UserIntent command in commands)
        {
            using PanePointerTestContext fixture = new();
            await fixture.LoadAsync();
            _ = await fixture.HandleAsync(command);
            CommandEntryPresentation opened = CommandEntryPresenter.Present(fixture.Session.Current);

            Assert.IsFalse(opened.IsEnabled);
            if (fixture.Session.Current.Scopes.KeyboardHelp is KeyboardHelpOpen help)
            {
                Assert.IsTrue(fixture.Session.CloseKeyboardHelp(help) is KeyboardHelpClosed);
            }
            else
            {
                Assert.IsTrue(CommandEntryPresenter.Present(await fixture.HandleAsync(UserIntent.Escape)).IsEnabled);
            }

            Assert.IsFalse(opened.IsEnabled);
            Assert.IsTrue(CommandEntryPresenter.Present(fixture.Session.Current).IsEnabled);
        }
    }

    /// <summary>Proves other existing transient scopes also disable background command entry.</summary>
    [TestMethod]
    public async Task PresentWhenAnotherScopeOwnsInputDisablesCommandEntryAsync()
    {
        UserIntent[] commands = [UserIntent.OpenColorSchemes, UserIntent.OpenCommandPalette,
            UserIntent.OpenWindowAdjustment, UserIntent.FocusAddress];
        foreach (UserIntent command in commands)
        {
            using PanePointerTestContext fixture = new();
            await fixture.LoadAsync();
            _ = await fixture.HandleAsync(command);

            Assert.IsFalse(CommandEntryPresenter.Present(fixture.Session.Current).IsEnabled);
        }
    }

    /// <summary>Proves reading either pane disables entry and a terminal cancellation restores it.</summary>
    [TestMethod]
    public async Task PresentWhenEitherPaneReadsDisablesUntilReadCompletesAsync()
    {
        foreach (PaneSide side in new[] { PaneSide.Left, PaneSide.Right })
        {
            using PanePointerTestContext fixture = new();
            await fixture.LoadAsync();
            TaskCompletionSource<DirectoryReadOutcome> release =
                (side == PaneSide.Left ? fixture.Left : fixture.Right).EnqueuePending();
            PaneContentListed listed = Assert.IsInstanceOfType<PaneContentListed>(fixture.Session.Current.Panes.Left.Content);
            Task<CommanderSnapshot> reading = fixture.Session.NavigateAsync(side, listed.Listing.Location, CancellationToken.None);

            Assert.IsFalse(CommandEntryPresenter.Present(fixture.Session.Current).IsEnabled);

            release.SetResult(DirectoryReadOutcome.Cancelled());
            _ = await reading;
            Assert.IsTrue(CommandEntryPresenter.Present(fixture.Session.Current).IsEnabled);
        }
    }

    /// <summary>Proves a pending file-name decision does not admit background entry.</summary>
    [TestMethod]
    public async Task PresentWhenOperationAwaitsNameDisablesCommandEntryAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        _ = await fixture.HandleAsync(UserIntent.CreateDirectory);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(fixture.Session.Current.Panes.Operation);

        Assert.IsFalse(CommandEntryPresenter.Present(fixture.Session.Current).IsEnabled);
    }

}
