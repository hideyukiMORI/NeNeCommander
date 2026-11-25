using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Owns deterministic complete-session state for pointer receipt tests.</summary>
internal sealed class PanePointerTestContext : IDisposable
{
    private readonly FileOperationGateway _gateway;

    internal PanePointerTestContext()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left,
            out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        _gateway = gateway;
        Left = left;
        Right = right;
        SettingsSession bookmarkSettings = new(new SuccessfulStore(),
            SettingsReadOutcome.Absent(), static _ => { });
        Session = new CommanderSession(panes, bookmarkSettings, new TransientScopeOwners(new PaneScopeOwners(new AddressEditorSession(), new LocationsSession(new PendingDriveCatalog(), new PendingWslDistributionCatalog()), new PaneFilterSession()), new CommandPaletteSession(), new WindowAdjustmentSession(), new KeyboardHelpSession()), new CommanderBookmarkCommands(panes, bookmarkSettings));
    }

    internal ScriptedDirectoryReadPort Left { get; }

    internal ScriptedDirectoryReadPort Right { get; }

    internal CommanderSession Session { get; }

    internal PanePresentation PresentRight()
    {
        return PaneListingTestContext.Present(Session.Current.Panes.Right);
    }

    internal async Task LoadAsync()
    {
        DirectoryListing listing = DualPaneTestContext.CreateListing("C:\\pointer", ["a", "b"]);
        Left.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        Right.Enqueue(DirectoryReadOutcome.Succeeded(listing, null));
        _ = await Session.NavigateAsync(PaneSide.Left, listing.Location, CancellationToken.None);
        _ = await Session.NavigateAsync(PaneSide.Right, listing.Location, CancellationToken.None);
    }

    internal Task<CommanderSnapshot> HandleAsync(UserIntent intent)
    {
        return Session.HandleAsync(intent, new SilentObserver(), CancellationToken.None);
    }

    public void Dispose()
    {
        _gateway.Dispose();
    }

    private sealed class SuccessfulStore : ISettingsStore
    {
        public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(SettingsReadOutcome.Absent());
        }

        public Task<SettingsWriteOutcome> WriteAsync(UserSettings settings, CancellationToken cancellationToken)
        {
            return Task.FromResult(SettingsWriteOutcome.Succeeded());
        }
    }

    private sealed class SilentObserver : ICommanderProgressObserver
    {
        public void OperationProgressed(DualPaneSnapshot snapshot)
        {
        }

        public void SettingsProgressed(SettingsSnapshot snapshot)
        {
        }
    }
}
