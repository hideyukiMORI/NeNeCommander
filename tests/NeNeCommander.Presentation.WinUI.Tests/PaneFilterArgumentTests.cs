using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves missing caller ownership fails at the named filter presentation boundary even when the current editor is closed.</summary>
[TestClass]
public sealed class PaneFilterArgumentTests
{
    /// <summary>Projection requires both the current snapshot and an explicitly addressed pane.</summary>
    [TestMethod]
    public void PresentWhenCallerContextIsAbsentRejectsItsNamedArgument()
    {
        using PanePointerTestContext fixture = new();
        Rejects(() => PaneFilterPresenter.Present(null!, PaneSide.Left, null), "snapshot");
        Rejects(() => PaneFilterPresenter.Present(fixture.Session.Current, null!, null), "side");
    }

    /// <summary>A closed editor does not silently accept an absent command, state or committed text.</summary>
    [TestMethod]
    public void QualifyWhenEditorIsClosedStillRejectsMissingCallerInput()
    {
        Rejects(() => PaneFilterPresenter.Qualify(null!, PaneFilterState.Closed, string.Empty), "intent");
        Rejects(() => PaneFilterPresenter.Qualify(UserIntent.Confirm, null!, string.Empty), "state");
        Rejects(() => PaneFilterPresenter.Qualify(UserIntent.Confirm, PaneFilterState.Closed, null!), "rawText");
        Assert.AreSame(UserIntent.Confirm, PaneFilterPresenter.Qualify(UserIntent.Confirm, PaneFilterState.Closed, string.Empty));
    }

    /// <summary>External-owner capture requires the exact request and state even when neither would request a blur.</summary>
    [TestMethod]
    public void CaptureExternalOwnerWhenNoBlurWouldOccurStillRejectsMissingInputs()
    {
        Rejects(() => PaneFilterPresenter.CaptureExternalOwner(null!, PaneFilterState.Closed), "intent");
        Rejects(() => PaneFilterPresenter.CaptureExternalOwner(UserIntent.Copy, null!), "state");
        Assert.IsNull(PaneFilterPresenter.CaptureExternalOwner(UserIntent.Copy, PaneFilterState.Closed));
    }

    /// <summary>A closed state with no focus effect does not bypass validation of the native focus boundary.</summary>
    [TestMethod]
    public void FocusTargetWhenNoEffectExistsStillRequiresEveryCallerOwner()
    {
        using PanePointerTestContext fixture = new();
        PaneFilterClosed closed = Assert.IsInstanceOfType<PaneFilterClosed>(fixture.Session.Current.Scopes.NameFilter);
        Rejects(() => PaneFilterPresenter.FocusTarget(null!, fixture.Session.Current, KeyboardContext.FileList), "expected");
        Rejects(() => PaneFilterPresenter.FocusTarget(closed, null!, KeyboardContext.FileList), "snapshot");
        Rejects(() => PaneFilterPresenter.FocusTarget(closed, fixture.Session.Current, null!), "context");
        Assert.IsNull(PaneFilterPresenter.FocusTarget(closed, fixture.Session.Current, KeyboardContext.FileList));
    }

    private static void Rejects(Action action, string parameter)
    {
        ArgumentNullException failure = Assert.ThrowsExactly<ArgumentNullException>(action);
        Assert.AreEqual(parameter, failure.ParamName);
    }
}
