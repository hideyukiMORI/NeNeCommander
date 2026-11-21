using System;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves settings entries reject absent required values and name rejected selections.</summary>
[TestClass]
public sealed class SettingsSessionNullGuardTests
{
    /// <summary>Proves settings and application-session boundaries reject absent required values.</summary>
    [TestMethod]
    public void SettingsSessionWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsReadOutcome outcome = SettingsReadOutcome.Absent();
        ConstructorInfo settingsConstructor = typeof(SettingsSession).GetConstructor(
            [typeof(ISettingsStore), typeof(SettingsReadOutcome), typeof(Action<Exception>)]) ??
            throw new AssertFailedException("The public settings-session constructor was not found.");
        SettingsSession settings = new(store, outcome, static _ => { });

        NullGuardAssertions.AssertConstructorNullGuard(settingsConstructor, [null, outcome, new Action<Exception>(_ => { })]);
        NullGuardAssertions.AssertConstructorNullGuard(settingsConstructor, [store, null, new Action<Exception>(_ => { })]);
        NullGuardAssertions.AssertConstructorNullGuard(settingsConstructor, [store, outcome, null]);
        NullGuardAssertions.AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectColorSchemeAsync),
            [null, new RecordingCommanderObserver(), CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectColorSchemeAsync),
            [ColorScheme.NeNeDark, null, CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectLaunchHiddenItemVisibilityAsync),
            [null, new RecordingCommanderObserver(), CancellationToken.None]);
        NullGuardAssertions.AssertInstanceNullGuard(settings, nameof(SettingsSession.SelectLaunchHiddenItemVisibilityAsync),
            [HiddenItemVisibility.Hidden, null, CancellationToken.None]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [null, SettingsEditorState.Closed, BookmarksEditorState.Closed, SettingsPersistenceState.Succeeded]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [UserSettings.Default, null, BookmarksEditorState.Closed, SettingsPersistenceState.Succeeded]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [UserSettings.Default, SettingsEditorState.Closed, null, SettingsPersistenceState.Succeeded]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsSnapshot),
            [UserSettings.Default, SettingsEditorState.Closed, BookmarksEditorState.Closed, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsPersistenceStartupRejected), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsPersistenceFailed), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsWriteRejected),
            [null, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsWriteRejected),
            [SettingsWriteFailureKind.IoFailure, null, SettingsWriteEffect.None]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(SettingsWriteRejected),
            [SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(ColorSchemeSelection), [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(LaunchHiddenItemVisibilitySelection), [null]);
    }

    /// <summary>
    /// Proves each settings-session entry rejects its own absent argument and names that
    /// parameter, so the caller sees the argument it supplied rather than an internal
    /// collaborator's parameter name.
    /// </summary>
    [TestMethod]
    public void SettingsSessionWhenSelectionArgumentIsNullNamesTheRejectedParameter()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession settings = new(store, SettingsReadOutcome.Absent(), static _ => { });

        ArgumentNullException scheme = Assert.ThrowsExactly<ArgumentNullException>(
            () => settings.SelectColorSchemeAsync(
                null!,
                new RecordingCommanderObserver(),
                CancellationToken.None));
        ArgumentNullException visibility = Assert.ThrowsExactly<ArgumentNullException>(
            () => settings.SelectLaunchHiddenItemVisibilityAsync(
                null!,
                new RecordingCommanderObserver(),
                CancellationToken.None));

        Assert.AreEqual("scheme", scheme.ParamName);
        Assert.AreEqual("visibility", visibility.ParamName);
        Assert.IsEmpty(store.Writes);
    }
}
