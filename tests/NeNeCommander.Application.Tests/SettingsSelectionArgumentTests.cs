using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves recent settings and picker boundaries reject missing values before publishing or saving.</summary>
[TestClass]
public sealed class SettingsSelectionArgumentTests
{
    /// <summary>Proves a picker intent cannot lack the owner that qualifies its action.</summary>
    [TestMethod]
    [DataRow(nameof(UserIntent.CancelColorSchemePicker))]
    [DataRow(nameof(UserIntent.SubmitColorScheme))]
    public void CreateWhenPickerOwnerIsAbsentNamesExpectedState(string method)
    {
        object?[] arguments = method == nameof(UserIntent.SubmitColorScheme)
            ? [null, ColorScheme.Dracula] : [null];

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(UserIntent), method, null, arguments);

        Assert.AreEqual("expectedState", error.ParamName);
    }

    /// <summary>Proves an owned picker still requires an explicit accepted scheme.</summary>
    [TestMethod]
    public void SubmitColorSchemeWhenSchemeIsAbsentNamesParameter()
    {
        ColorSchemePickerOpen open = new(SettingsEditorState.Closed);

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(UserIntent), nameof(UserIntent.SubmitColorScheme), null, [open, null]);

        Assert.AreEqual("scheme", error.ParamName);
        Assert.AreSame(SettingsEditorState.Closed, open.ReturnTo);
    }

    /// <summary>Proves the key-hint selection intent cannot carry an absent choice.</summary>
    [TestMethod]
    public void SelectKeyHintVisibilityWhenChoiceIsAbsentNamesParameter()
    {
        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(UserIntent), nameof(UserIntent.SelectKeyHintVisibility), null, [null]);

        Assert.AreEqual("visibility", error.ParamName);
    }

    /// <summary>Proves the session rejects at its own boundary without changing accepted settings or writing.</summary>
    [TestMethod]
    [DataRow(nameof(SettingsSession.SelectKeyHintVisibilityAsync), "visibility")]
    [DataRow(nameof(SettingsSession.SelectInterfaceLanguageAsync), "language")]
    public void SelectWhenChoiceIsAbsentPreservesSettings(string method, string parameter)
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        SettingsSnapshot before = session.Current;

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(
            typeof(SettingsSession), method, session, [null, observer, CancellationToken.None]);

        Assert.AreEqual(parameter, error.ParamName);
        Assert.AreEqual(before, session.Current);
        Assert.IsEmpty(store.Writes);
        Assert.IsEmpty(observer.Settings);
    }

    /// <summary>Proves an unobservable preference write is rejected before accepting the choice.</summary>
    [TestMethod]
    public void SelectKeyHintVisibilityWhenObserverIsAbsentPreservesSettings()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        SettingsSnapshot before = session.Current;

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(
            typeof(SettingsSession), nameof(SettingsSession.SelectKeyHintVisibilityAsync), session,
            [KeyHintVisibility.Hidden, null, CancellationToken.None]);

        Assert.AreEqual("observer", error.ParamName);
        Assert.AreEqual(before, session.Current);
        Assert.IsEmpty(store.Writes);
    }

    /// <summary>Proves even a no-change placement save validates its required observer.</summary>
    [TestMethod]
    public void SaveWindowPlacementWhenObserverIsAbsentPreservesSettings()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        SettingsSnapshot before = session.Current;

        ArgumentNullException error = NullGuardAssertions.AssertInternalMethodNullGuard(
            typeof(SettingsSession), nameof(SettingsSession.SaveWindowPlacementAsync), session,
            [null, null, CancellationToken.None]);

        Assert.AreEqual("observer", error.ParamName);
        Assert.AreEqual(before, session.Current);
        Assert.IsEmpty(store.Writes);
    }
}
