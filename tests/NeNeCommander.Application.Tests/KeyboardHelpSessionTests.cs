using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the keyboard help scope's admission, idempotent open and qualified close (ADR-0076).</summary>
[TestClass]
public sealed class KeyboardHelpSessionTests
{
    /// <summary>Proves a new scope is closed with no focus request.</summary>
    [TestMethod]
    public void CurrentWhenNothingHappenedIsClosedWithoutFocusRequest()
    {
        KeyboardHelpSession session = new();

        Assert.AreSame(KeyboardHelpState.Closed, session.Current);
        Assert.IsNull(Assert.IsInstanceOfType<KeyboardHelpClosed>(session.Current).FileListFocusSide);
    }

    /// <summary>Proves an owned open captures the active side.</summary>
    [TestMethod]
    public void OpenWhenScopeOwnsInputCapturesTheActiveSide()
    {
        KeyboardHelpSession session = new();

        KeyboardHelpOpen open = Assert.IsInstanceOfType<KeyboardHelpOpen>(
            session.Open(PaneSide.Right, InteractionOwnership.ScopeOwnsInput));

        Assert.AreSame(open, session.Current);
        Assert.AreSame(PaneSide.Right, open.ActiveSide);
    }

    /// <summary>Proves an open while another scope owns input leaves the help closed.</summary>
    [TestMethod]
    public void OpenWhenAnotherScopeOwnsInputStaysClosed()
    {
        KeyboardHelpSession session = new();

        KeyboardHelpState refused = session.Open(PaneSide.Left, InteractionOwnership.AnotherScopeOwnsInput);

        Assert.AreSame(KeyboardHelpState.Closed, refused);
        Assert.AreSame(KeyboardHelpState.Closed, session.Current);
    }

    /// <summary>Proves a second open keeps the current instance and side.</summary>
    [TestMethod]
    public void OpenWhenAlreadyOpenKeepsTheCurrentInstance()
    {
        KeyboardHelpSession session = new();
        KeyboardHelpState first = session.Open(PaneSide.Left, InteractionOwnership.ScopeOwnsInput);

        KeyboardHelpState again = session.Open(PaneSide.Right, InteractionOwnership.ScopeOwnsInput);

        Assert.AreSame(first, again);
        Assert.AreSame(PaneSide.Left, Assert.IsInstanceOfType<KeyboardHelpOpen>(again).ActiveSide);
    }

    /// <summary>Proves a qualified close requests focus on the side captured at entry, and a replayed close changes nothing.</summary>
    [TestMethod]
    public void CloseWhenExpectedIsCurrentRequestsFocusOnTheEntrySide()
    {
        KeyboardHelpSession session = new();
        KeyboardHelpOpen open = Assert.IsInstanceOfType<KeyboardHelpOpen>(
            session.Open(PaneSide.Right, InteractionOwnership.ScopeOwnsInput));

        KeyboardHelpState closed = session.Close(open);
        KeyboardHelpState replayed = session.Close(open);

        Assert.AreSame(PaneSide.Right, Assert.IsInstanceOfType<KeyboardHelpClosed>(closed).FileListFocusSide);
        Assert.AreSame(closed, replayed);
        Assert.AreSame(closed, session.Current);
    }

    /// <summary>Proves a stale close from an earlier open leaves the current open help untouched.</summary>
    [TestMethod]
    public void CloseWhenExpectedIsStaleChangesNothing()
    {
        KeyboardHelpSession session = new();
        KeyboardHelpOpen earlier = Assert.IsInstanceOfType<KeyboardHelpOpen>(
            session.Open(PaneSide.Left, InteractionOwnership.ScopeOwnsInput));
        _ = session.Close(earlier);
        KeyboardHelpOpen current = Assert.IsInstanceOfType<KeyboardHelpOpen>(
            session.Open(PaneSide.Left, InteractionOwnership.ScopeOwnsInput));

        KeyboardHelpState stale = session.Close(earlier);

        Assert.AreSame(current, stale);
        Assert.AreSame(current, session.Current);
    }

    /// <summary>Proves every public member rejects a missing argument.</summary>
    [TestMethod]
    public void MembersWhenArgumentIsNullThrow()
    {
        KeyboardHelpSession session = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Open(null!, InteractionOwnership.ScopeOwnsInput));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Open(PaneSide.Left, null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => session.Close(null!));
    }
}
