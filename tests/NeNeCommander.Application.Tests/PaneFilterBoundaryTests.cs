using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves filter and composition boundaries reject invalid owner combinations.</summary>
[TestClass]
public sealed class PaneFilterBoundaryTests
{
    /// <summary>Proves bookmark extraction cannot be composed over a different pane or settings owner.</summary>
    [TestMethod]
    public void CommanderSessionWhenBookmarkOwnersDifferRejectsTheComposition()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        using CommanderContextTestContext other = CommanderContextTestContext.Create();
        SettingsSession settings = new(new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Absent(), static _ => { });
        SettingsSession otherSettings = new(new ScriptedSettingsStore(SettingsReadOutcome.Absent()),
            SettingsReadOutcome.Absent(), static _ => { });
        TransientScopeOwners scopes = new(new PaneScopeOwners(new AddressEditorSession(),
            new LocationsSession(new ScriptedDriveCatalog(), new ScriptedWslDistributionCatalog()), new PaneFilterSession()),
            new CommandPaletteSession(), new WindowAdjustmentSession(), new KeyboardHelpSession());

        _ = new CommanderSession(fixture.Panes.Panes, settings, scopes, new CommanderBookmarkCommands(fixture.Panes.Panes, settings));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new CommanderSession(fixture.Panes.Panes, settings,
            scopes, new CommanderBookmarkCommands(other.Panes.Panes, settings)));
        _ = Assert.ThrowsExactly<ArgumentException>(() => new CommanderSession(fixture.Panes.Panes, settings,
            scopes, new CommanderBookmarkCommands(fixture.Panes.Panes, otherSettings)));
    }

    /// <summary>Proves the internal edit capture cannot admit an unlisted pane.</summary>
    [TestMethod]
    public void PaneFilterEditingWhenPaneIsUnlistedRejectsTheCapture()
    {
        ArgumentException argument = Assert.ThrowsExactly<ArgumentException>(
            () => new PaneFilterEditing(PaneSide.Left, PaneSnapshot.Initial));

        Assert.AreEqual("sourcePane", argument.ParamName);
    }

    /// <summary>Proves defensive closed-owner validation cannot change the session.</summary>
    [TestMethod]
    public async Task ValidateWhenFilterIsClosedKeepsItClosedAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterSession filter = new();
        PaneFilterValidation result = filter.Validate(UserIntent.Escape,
            fixture.Session.Current.Panes, InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(PaneFilterValidation.NothingToApply, result);
        Assert.AreSame(PaneFilterState.Closed, filter.Current);
        Assert.HasCount(1, fixture.Panes.Left.Requests);
    }

    /// <summary>Proves defensive clear cannot close an existing edit owner or invent a focus request.</summary>
    [TestMethod]
    public async Task ClearWhenFilterIsEditingKeepsItsExactOwnerAsync()
    {
        using CommanderContextTestContext fixture = CommanderContextTestContext.Create();
        await fixture.ListBothAsync();
        PaneFilterSession filter = new();
        filter.Open(fixture.Session.Current.Panes, InteractionOwnership.ScopeOwnsInput);
        PaneFilterState owner = filter.Current;

        PaneFilterValidation result = filter.Clear(fixture.Session.Current.Panes, InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(PaneFilterValidation.NothingToApply, result);
        _ = Assert.IsInstanceOfType<PaneFilterEditing>(owner);
        Assert.AreSame(owner, filter.Current);
    }
}
