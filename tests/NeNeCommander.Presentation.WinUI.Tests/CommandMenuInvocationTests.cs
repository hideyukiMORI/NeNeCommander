using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Commands;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves invocation-turn ordering against the actual existing command-selection owner.</summary>
[TestClass]
public sealed class CommandMenuInvocationTests
{
    /// <summary>Proves Click then Closed submits once before deferred dismissal, entering the existing name scope.</summary>
    [TestMethod]
    public async Task CloseWhenClickPrecedesNativeCloseSubmitsOnceAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        CommandMenuInvocation invocation = new(open);
        List<Action> queue = [];
        List<UserIntent> forwarded = [];
        CommandCandidate candidate = open.Candidates.Single(row => row.Intent == UserIntent.CreateTextFile);
        UserIntent? submit = invocation.Submit(candidate);
        Assert.IsNotNull(submit);
        invocation.Close(queue.Add, forwarded.Add);
        _ = await fixture.HandleAsync(submit);
        foreach (Action callback in queue)
        {
            callback();
        }
        Assert.AreEqual(0, forwarded.Count);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(fixture.Session.Current.Panes.Operation);
        Assert.IsNull(invocation.Submit(candidate));
    }

    /// <summary>Proves Closed then Click submits once before deferred dismissal, entering the existing name scope.</summary>
    [TestMethod]
    public async Task CloseWhenClickFollowsNativeCloseSubmitsBeforeFinalizerAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>(
            (await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        CommandMenuInvocation invocation = new(open);
        List<Action> queue = [];
        List<UserIntent> forwarded = [];
        invocation.Close(queue.Add, forwarded.Add);
        CommandCandidate candidate = open.Candidates.Single(row => row.Intent == UserIntent.CreateTextFile);
        UserIntent? submit = invocation.Submit(candidate);
        Assert.IsNotNull(submit);
        _ = await fixture.HandleAsync(submit);
        foreach (Action callback in queue)
        {
            callback();
        }
        Assert.AreEqual(0, forwarded.Count);
        _ = Assert.IsInstanceOfType<OperationAwaitingName>(fixture.Session.Current.Panes.Operation);
        Assert.IsNull(invocation.Submit(candidate));
    }

    /// <summary>Proves genuine dismissal forwards exactly one qualified cancellation and rejects a late invocation.</summary>
    [TestMethod]
    public async Task CloseWhenFinalizedCancelsOnceAndRejectsLateClickAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        CommandPaletteOpen open = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        CommandMenuInvocation invocation = new(open);
        List<Action> queue = [];
        List<UserIntent> forwarded = [];
        invocation.Close(queue.Add, forwarded.Add);
        invocation.Close(queue.Add, forwarded.Add);
        Assert.AreEqual(0, forwarded.Count);
        Assert.AreEqual(1, queue.Count);
        queue[0]();
        queue[0]();
        Assert.AreEqual(1, forwarded.Count);
        Assert.IsNull(invocation.Submit(open.Candidates[0]));
        CommandPaletteClosed closed = Assert.IsInstanceOfType<CommandPaletteClosed>((await fixture.HandleAsync(forwarded[0])).Scopes.CommandPalette);
        Assert.AreSame(PaneSide.Left, closed.FileListFocusSide);
    }

    /// <summary>Proves an old native close cannot cancel or request focus over its replacement owner.</summary>
    [TestMethod]
    public async Task CloseWhenApplicationHasReplacementDoesNotChangeItAsync()
    {
        using PanePointerTestContext fixture = new();
        await fixture.LoadAsync();
        CommandPaletteOpen first = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        CommandMenuInvocation old = new(first);
        List<Action> queue = [];
        List<UserIntent> forwarded = [];
        old.Close(queue.Add, forwarded.Add);
        CommandPaletteOpen replacement = Assert.IsInstanceOfType<CommandPaletteOpen>((await fixture.HandleAsync(UserIntent.OpenContextMenu)).Scopes.CommandPalette);
        Assert.AreNotSame(first, replacement);
        queue[0]();
        CommanderSnapshot unchanged = await fixture.HandleAsync(forwarded.Single());
        Assert.AreSame(replacement, unchanged.Scopes.CommandPalette);
    }

    /// <summary>Proves a departing/stopping adapter invalidates all queued and subsequent callbacks.</summary>
    [TestMethod]
    public async Task DetachWhenCloseIsQueuedForwardsNothingAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandMenuInvocation invocation = new(open);
        List<Action> queue = [];
        List<UserIntent> forwarded = [];
        invocation.Close(queue.Add, forwarded.Add);
        invocation.Detach();
        queue[0]();
        Assert.AreEqual(0, forwarded.Count);
        Assert.IsNull(invocation.Submit(open.Candidates[0]));
        invocation.Close(queue.Add, forwarded.Add);
        Assert.AreEqual(1, queue.Count);
    }

    /// <summary>Proves equal/foreign candidates and unavailable copy cannot claim a native invocation.</summary>
    [TestMethod]
    public async Task SubmitWhenCandidateForeignOrUnavailablePreservesDismissalAsync()
    {
        CommandPaletteOpen open = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandPaletteOpen foreign = await CommandPaletteTestContext.OpenPaletteAsync(null);
        CommandMenuInvocation invocation = new(open);
        Assert.IsNull(invocation.Submit(foreign.Candidates[0]));
        Assert.IsNull(invocation.Submit(open.Candidates.Single(row => row.Intent == UserIntent.Copy)));
        List<Action> queue = [];
        List<UserIntent> forwarded = [];
        invocation.Close(queue.Add, forwarded.Add);
        queue[0]();
        Assert.AreEqual(1, forwarded.Count);
    }
}
