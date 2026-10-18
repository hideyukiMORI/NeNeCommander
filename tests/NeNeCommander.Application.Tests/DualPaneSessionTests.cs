using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves DualPaneSession behavior and boundaries.</summary>
[TestClass]
public sealed class DualPaneSessionTests
{
    /// <summary>Proves the left pane is active before any intent and both panes start empty.</summary>
    [TestMethod]
    public void CurrentWhenNothingHappenedReportsLeftActiveAndBothInitial()
    {
        using DualPaneSessionTestContext fixture = DualPaneSessionTestContext.Create();

        DualPaneSnapshot snapshot = fixture.Panes.Current;

        Assert.AreSame(PaneSide.Left, snapshot.ActiveSide);
        Assert.AreSame(PaneSnapshot.Initial, snapshot.Left);
        Assert.AreSame(PaneSnapshot.Initial, snapshot.Right);
        Assert.AreSame(OperationActivity.Idle, snapshot.Operation);
    }

    /// <summary>Proves one session cannot serve both sides.</summary>
    [TestMethod]
    public void ConstructWhenBothSidesShareOneSessionThrowsArgumentException()
    {
        ScriptedDirectoryReadPort port = ScriptedDirectoryReadPort.Create();
        PaneSession shared = new(port,
            new ScriptedFileLauncher(),
            PaneSessionConfiguration.Create(DualPaneSessionTestContext.Capacity(4), DirectoryListing.EntryBoundaryLimit, HiddenItemVisibility.Hidden, PaneSortOrder.Default));
        using FileOperationGateway gateway = new(ScriptedFileOperationPort.Create(null, null));

        ArgumentException failure = Assert.ThrowsExactly<ArgumentException>(() => new DualPaneSession(shared, shared, gateway));

        Assert.AreEqual("right", failure.ParamName);
        Assert.StartsWith("Each pane side requires its own session.", failure.Message);
    }
}
