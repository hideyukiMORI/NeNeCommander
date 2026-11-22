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
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves opposite-pane reads compose with the real host work owner and session cancellation.</summary>
[TestClass]
public sealed class OtherPaneNavigationHostTests
{
    /// <summary>Proves admitted opposite navigation refuses ordinary overlap but Escape releases it before provider completion.</summary>
    [TestMethod]
    public async Task TryStartIntentWhenOtherPaneLoadsAcceptsEscapeAndPreservesSourceAsync()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        await ListAsync(panes, left, right);
        CommanderSession session = CreateSession(panes);
        SilentCommanderObserver observer = new();
        _ = await session.HandleAsync(UserIntent.ToggleSelection, observer, CancellationToken.None);
        CommanderSnapshot before = session.Current;
        TaskCompletionSource<DirectoryReadOutcome> provider = right.EnqueuePendingInline();
        AsyncWorkOwner owner = new(_ => Assert.Fail("No host fault expected."));
        Task<CommanderSnapshot>? command = null;
        bool accepted = owner.TryStartIntent(UserIntent.OpenCurrentLocationInOtherPane,
            token => command = session.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, observer, token));

        bool tab = owner.TryStartIntent(UserIntent.ActivateOtherPane,
            token => session.HandleAsync(UserIntent.ActivateOtherPane, observer, token));
        bool second = owner.TryStartIntent(UserIntent.OpenCurrentLocationInOtherPane,
            token => session.HandleAsync(UserIntent.OpenCurrentLocationInOtherPane, observer, token));
        bool escaped = owner.TryStartIntent(UserIntent.Escape,
            token => session.HandleAsync(UserIntent.Escape, observer, token));
        Assert.IsNotNull(command);
        CommanderSnapshot returned = await command.WaitAsync(TimeSpan.FromSeconds(5));
        await owner.StopAsync();

        Assert.IsTrue(accepted);
        Assert.IsFalse(tab);
        Assert.IsFalse(second);
        Assert.IsTrue(escaped);
        Assert.IsFalse(owner.HasOwnedWork);
        Assert.IsNull(owner.Fault);
        Assert.IsFalse(provider.Task.IsCompleted);
        Assert.IsTrue(right.Tokens[1].IsCancellationRequested);
        Assert.AreSame(before.Panes.Left, returned.Panes.Left);
        Assert.AreSame(before.Panes.Right.Content, returned.Panes.Right.Content);
        Assert.AreSame(PaneSide.Left, returned.Panes.ActiveSide);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(returned.Panes.Right.Activity);
        provider.SetResult(DirectoryReadOutcome.Succeeded(DualPaneTestContext.CreateListing(@"C:\source", ["late"]), null));
        Assert.AreSame(returned.Panes.Right, session.Current.Panes.Right);
    }

    /// <summary>Proves the old command's delayed finally cannot erase a newer command's cancellation ownership.</summary>
    [TestMethod]
    public async Task HandleAsyncWhenOldFinallyRunsAfterNewCapturePreservesNewEscapeRouteAsync()
    {
        DualPaneSession panes = DualPaneTestContext.CreatePanes(out ScriptedDirectoryReadPort left, out ScriptedDirectoryReadPort right, out FileOperationGateway gateway);
        using FileOperationGateway owned = gateway;
        await ListAsync(panes, left, right);
        TaskCompletionSource<DirectoryReadOutcome> firstProvider = right.EnqueuePending();
        ControlledSynchronizationContext context = new();
        Task<DualPaneSnapshot> first;
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            first = HandleAsync(panes, UserIntent.OpenCurrentLocationInOtherPane);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        _ = await HandleAsync(panes, UserIntent.Escape);
        Assert.IsFalse(first.IsCompleted);
        Assert.IsTrue(right.Tokens[1].IsCancellationRequested);
        TaskCompletionSource<DirectoryReadOutcome> secondProvider = right.EnqueuePending();
        Task<DualPaneSnapshot> second = HandleAsync(panes, UserIntent.OpenCurrentLocationInOtherPane);
        PaneSnapshot secondLoading = panes.Current.Right;
        Assert.IsFalse(second.IsCompleted);

        await context.Posted.WaitAsync(TimeSpan.FromSeconds(5));
        while (!first.IsCompleted)
        {
            context.ExecuteOne();
        }
        _ = await first;
        Assert.AreSame(secondLoading, panes.Current.Right);
        _ = await HandleAsync(panes, UserIntent.Escape);
        DualPaneSnapshot escaped = await second.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(firstProvider.Task.IsCompleted);
        Assert.IsFalse(secondProvider.Task.IsCompleted);
        Assert.IsTrue(right.Tokens[2].IsCancellationRequested);
        _ = Assert.IsInstanceOfType<PaneReadAbandoned>(escaped.Right.Activity);
        firstProvider.SetResult(DirectoryReadOutcome.Cancelled());
        secondProvider.SetResult(DirectoryReadOutcome.Cancelled());
    }

    private static async Task ListAsync(DualPaneSession panes, ScriptedDirectoryReadPort left, ScriptedDirectoryReadPort right)
    {
        DirectoryListing source = DualPaneTestContext.CreateListing(@"C:\source", ["selected"]);
        DirectoryListing target = DualPaneTestContext.CreateListing(@"D:\target", ["old"]);
        left.Enqueue(DirectoryReadOutcome.Succeeded(source, null));
        right.Enqueue(DirectoryReadOutcome.Succeeded(target, null));
        _ = await panes.NavigateAsync(PaneSide.Left, source.Location, CancellationToken.None);
        _ = await panes.NavigateAsync(PaneSide.Right, target.Location, CancellationToken.None);
    }

    private static Task<DualPaneSnapshot> HandleAsync(DualPaneSession panes, UserIntent intent)
    {
        return panes.HandleAsync(intent, new SilentCommanderObserver(), CancellationToken.None);
    }

    private static CommanderSession CreateSession(DualPaneSession panes)
    {
        return new CommanderSession(panes,
            new SettingsSession(new SuccessfulSettingsStore(), SettingsReadOutcome.Absent(), static _ => { }),
            new TransientScopeOwners(new AddressEditorSession(), new CommandPaletteSession(), new WindowAdjustmentSession(),
                new LocationsSession(new PendingDriveCatalog(), new PendingWslDistributionCatalog()), new KeyboardHelpSession()));
    }

    private sealed class SuccessfulSettingsStore : ISettingsStore
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

    private sealed class SilentCommanderObserver : ICommanderProgressObserver
    {
        public void OperationProgressed(DualPaneSnapshot snapshot)
        {
        }

        public void SettingsProgressed(SettingsSnapshot snapshot)
        {
        }
    }
}
