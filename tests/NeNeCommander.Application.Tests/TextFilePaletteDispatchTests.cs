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

/// <summary>Proves accepted palette commands use the existing intent and scope owners.</summary>
[TestClass]
public sealed class TextFilePaletteDispatchTests
{
    /// <summary>Proves an available palette command closes and enters the existing modal route once.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenPaletteCreatesTextFileEntersExistingNameScopeAsync()
    {
        ScriptedDirectoryReadPort left = ScriptedDirectoryReadPort.Create();
        ScriptedDirectoryReadPort right = ScriptedDirectoryReadPort.Create();
        left.Enqueue(DirectoryReadOutcome.Succeeded(CommanderSessionTestValues.Listing("C:\\left", "item.txt"), null));
        using FileOperationGateway gateway = CommanderSessionTestValues.CreateGateway();
        CommanderSession session = new CommanderSessionTestComposition(left, right, gateway, new ScriptedSettingsStore(SettingsReadOutcome.Absent())).CreateSession();
        RecordingCommanderObserver observer = new();
        _ = await session.NavigateAsync(PaneSide.Left, CommanderSessionTestValues.ParsePath("C:\\left"), CancellationToken.None);
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await session.HandleAsync(
            UserIntent.OpenCommandPalette,
            observer,
            CancellationToken.None)).Scopes.CommandPalette);

        CommanderSnapshot dispatched = await session.HandleAsync(
            UserIntent.SubmitCommand(open, UserIntent.CreateTextFile),
            observer,
            CancellationToken.None);

        Assert.AreSame(CommandPaletteState.Closed, dispatched.Scopes.CommandPalette);
        OperationAwaitingName awaiting = Assert.IsInstanceOfType<OperationAwaitingName>(dispatched.Panes.Operation);
        Assert.AreSame(OperationKind.CreateTextFile, awaiting.Kind);
        Assert.AreEqual("C:\\left", awaiting.Subject.CanonicalText);
    }

}
