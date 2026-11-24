using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves both theme-picker entrances share one modal owner without moving or modifying either pane.</summary>
[TestClass]
public sealed class CommanderThemePickerTests
{
    /// <summary>Proves settings/palette entry and cancellation retain each pane, active side and selection.</summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    public async Task HandleWhenEitherEntryOpensPickerFreezesPanesAndReturnsToOwnerAsync(int origin, int side)
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(side);
        CommanderSnapshot before = fixture.Session.Current;
        ColorSchemePickerOpen open = await fixture.OpenAsync(origin);
        _ = await fixture.HandleAsync(UserIntent.ActivateOtherPane);
        _ = await fixture.HandleAsync(UserIntent.ToggleSelection);
        _ = await fixture.HandleAsync(UserIntent.OpenSettings);
        _ = await fixture.Session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\blocked"), CancellationToken.None);
        Assert.AreSame(open, fixture.Session.Current.Settings.Editor);
        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.CancelColorSchemePicker(open));
        Assert.AreSame(origin == 0 ? SettingsEditorState.Open : SettingsEditorState.Closed, after.Settings.Editor);
        Assert.AreSame(before.Panes.Left, after.Panes.Left);
        Assert.AreSame(before.Panes.Right, after.Panes.Right);
        Assert.AreSame(before.Panes.ActiveSide, after.Panes.ActiveSide);
        Assert.HasCount(1, fixture.Left.Requests);
        Assert.HasCount(1, fixture.Right.Requests);
        Assert.IsEmpty(fixture.Store.Writes);
    }

    /// <summary>Proves palette admission still requires a qualified current palette submission.</summary>
    [TestMethod]
    public async Task HandleWhenThemeCommandIsUnqualifiedKeepsPaletteOwnerAsync()
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(0);
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.OpenCommandPalette);
        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.OpenColorSchemes);
        Assert.AreSame(before.Scopes.CommandPalette, after.Scopes.CommandPalette);
        Assert.AreSame(SettingsEditorState.Closed, after.Settings.Editor);
        Assert.IsEmpty(fixture.Store.Writes);
    }

    /// <summary>Proves an operation name modal refuses the new command just as it refuses settings entry.</summary>
    [TestMethod]
    public async Task HandleWhenOperationOwnsModalRefusesThemeEntryAsync()
    {
        using Fixture fixture = new();
        await fixture.PrepareAsync(0);
        CommanderSnapshot before = await fixture.HandleAsync(UserIntent.Rename);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(before.Panes.Operation);
        CommanderSnapshot after = await fixture.HandleAsync(UserIntent.OpenColorSchemes);
        Assert.AreSame(before.Panes.Operation, after.Panes.Operation);
        Assert.AreSame(SettingsEditorState.Closed, after.Settings.Editor);
        Assert.IsEmpty(fixture.Store.Writes);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly FileOperationGateway _gateway = CommanderSessionTestValues.CreateGateway();
        private readonly RecordingCommanderObserver _observer = new();

        internal Fixture()
        {
            Session = new CommanderSessionTestComposition(Left, Right, _gateway, Store).CreateSession();
        }

        internal ScriptedDirectoryReadPort Left { get; } = ScriptedDirectoryReadPort.Create();
        internal ScriptedDirectoryReadPort Right { get; } = ScriptedDirectoryReadPort.Create();
        internal ScriptedSettingsStore Store { get; } = new(SettingsReadOutcome.Absent());
        internal CommanderSession Session { get; }

        internal async Task PrepareAsync(int side)
        {
            Left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "left.txt"), null));
            Right.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\right", "right.txt"), null));
            _ = await Session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
            _ = await Session.NavigateAsync(PaneSide.Right, CommanderSessionTestValues.ParsePath("C:\\right"), CancellationToken.None);
            if (side == 1) { _ = await HandleAsync(UserIntent.ActivateOtherPane); }
            _ = await HandleAsync(UserIntent.ToggleSelection);
        }

        internal async Task<ColorSchemePickerOpen> OpenAsync(int origin)
        {
            if (origin == 0)
            {
                _ = await HandleAsync(UserIntent.OpenSettings);
                _ = await HandleAsync(UserIntent.OpenColorSchemes);
            }
            else
            {
                CommanderSnapshot palette = await HandleAsync(UserIntent.OpenCommandPalette);
                _ = await HandleAsync(UserIntent.SubmitCommand(
                    Assert.IsInstanceOfType<CommandPaletteOpen>(palette.Scopes.CommandPalette), UserIntent.OpenColorSchemes));
            }
            return Assert.IsInstanceOfType<ColorSchemePickerOpen>(Session.Current.Settings.Editor);
        }

        internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
        {
            return Session.HandleAsync(intent, _observer, CancellationToken.None);
        }

        public void Dispose()
        {
            _gateway.Dispose();
        }
    }
}
