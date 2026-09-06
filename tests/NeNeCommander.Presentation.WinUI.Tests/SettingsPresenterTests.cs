using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves settings state projects to one modal and one independent warning.</summary>
[TestClass]
public sealed class SettingsPresenterTests
{
    /// <summary>Proves all eight schemes and the next-launch visibility project in canonical order.</summary>
    [TestMethod]
    public void PresentWhenEditorIsOpenProjectsEveryChoice()
    {
        SettingsSnapshot snapshot = Snapshot(
            UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown),
            SettingsEditorState.Open,
            SettingsPersistenceState.Succeeded);

        SettingsPresentation presentation = SettingsPresenter.Present(snapshot);

        Assert.IsTrue(presentation.IsOpen);
        Assert.IsTrue(presentation.ShowHiddenItemsAtLaunch);
        Assert.HasCount(8, presentation.Schemes);
        string[] expectedResourceKeys =
        [
            "ColorSchemeNameNeNeDark",
            "ColorSchemeNameUbuntu",
            "ColorSchemeNameMonokai",
            "ColorSchemeNameSolarizedDark",
            "ColorSchemeNameSolarizedLight",
            "ColorSchemeNameDracula",
            "ColorSchemeNameNeNeBlack",
            "ColorSchemeNameNeNeLight",
        ];
        for (int index = 0; index < ColorScheme.All.Count; index++)
        {
            Assert.AreSame(ColorScheme.All[index], presentation.Schemes[index].Scheme);
            Assert.AreEqual(expectedResourceKeys[index], presentation.Schemes[index].NameResourceKey);
            Assert.AreEqual(index == 5, presentation.Schemes[index].IsSelected);
        }
        Assert.AreSame(SettingsSaveStatus.Succeeded, presentation.SaveStatus);
        Assert.AreEqual("SettingsSaveStatusSucceeded", presentation.SaveStatus.ResourceKey);
        Assert.AreSame(SettingsWarningPresentation.Hidden, presentation.Warning);
        Assert.AreEqual("SettingsWarningHidden", presentation.Warning.ResourceKey);
        Assert.IsFalse(presentation.Warning.IsVisible);
    }

    /// <summary>Proves pending and failed persistence never reuse the operation-bar presentation.</summary>
    [TestMethod]
    public void PresentWhenPersistenceVariesProjectsTypedStatusAndWarning()
    {
        SettingsPresentation pending = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Closed,
            SettingsPersistenceState.Pending));
        SettingsPresentation startupRejected = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Closed,
            SettingsPersistenceState.StartupRejected(SettingsReadFailureKind.Malformed)));
        SettingsPresentation failed = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Closed,
            SettingsPersistenceState.Failed(Assert.IsInstanceOfType<SettingsWriteRejected>(
                SettingsWriteOutcome.Rejected(
                    SettingsWriteFailureKind.IoFailure,
                    SettingsDirectoryEffect.NotAttempted,
                    SettingsWriteEffect.None)))));
        SettingsPresentation cancelled = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Closed,
            SettingsPersistenceState.Cancelled));

        Assert.AreSame(SettingsSaveStatus.Pending, pending.SaveStatus);
        Assert.AreEqual("SettingsSaveStatusPending", pending.SaveStatus.ResourceKey);
        Assert.AreSame(SettingsWarningPresentation.Hidden, pending.Warning);
        Assert.AreSame(SettingsSaveStatus.Failed, startupRejected.SaveStatus);
        Assert.AreEqual("SettingsSaveStatusFailed", startupRejected.SaveStatus.ResourceKey);
        Assert.AreSame(SettingsWarningPresentation.StartupRejected, startupRejected.Warning);
        Assert.AreEqual("SettingsWarningStartupRejected", startupRejected.Warning.ResourceKey);
        Assert.IsTrue(startupRejected.Warning.IsVisible);
        Assert.AreSame(SettingsWarningPresentation.SaveFailed, failed.Warning);
        Assert.AreEqual("SettingsWarningSaveFailed", failed.Warning.ResourceKey);
        Assert.IsTrue(failed.Warning.IsVisible);
        Assert.AreSame(SettingsWarningPresentation.SaveFailed, cancelled.Warning);
    }

    /// <summary>Proves save progress never releases the open editor's modal ownership.</summary>
    [TestMethod]
    public void PresentWhenOpenPersistenceVariesKeepsTheEditorOpen()
    {
        SettingsPresentation pending = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Open,
            SettingsPersistenceState.Pending));
        SettingsPresentation succeeded = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Open,
            SettingsPersistenceState.Succeeded));
        SettingsPresentation failed = SettingsPresenter.Present(Snapshot(
            UserSettings.Default,
            SettingsEditorState.Open,
            SettingsPersistenceState.Failed(Assert.IsInstanceOfType<SettingsWriteRejected>(
                SettingsWriteOutcome.Rejected(
                    SettingsWriteFailureKind.IoFailure,
                    SettingsDirectoryEffect.NotAttempted,
                    SettingsWriteEffect.None)))));

        Assert.IsTrue(pending.IsOpen);
        Assert.IsTrue(succeeded.IsOpen);
        Assert.IsTrue(failed.IsOpen);
    }

    /// <summary>Proves the public projection rejects an absent application snapshot.</summary>
    [TestMethod]
    public void PresentWhenSnapshotIsNullRejectsTheCall()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => SettingsPresenter.Present(null!));
    }

    /// <summary>Proves each scheme option requires complete typed and localized input.</summary>
    [TestMethod]
    public void ColorSchemeOptionWhenRequiredInputIsInvalidRejectsTheCall()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new SettingsColorSchemeOption(null!, "ColorSchemeNameNeNeDark", ColorScheme.NeNeDark));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new SettingsColorSchemeOption(ColorScheme.NeNeDark, null!, ColorScheme.NeNeDark));
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
            new SettingsColorSchemeOption(ColorScheme.NeNeDark, " ", ColorScheme.NeNeDark));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            new SettingsColorSchemeOption(ColorScheme.NeNeDark, "ColorSchemeNameNeNeDark", null!));
    }

    /// <summary>Proves the modal presentation cannot contain an absent typed field.</summary>
    [TestMethod]
    public void SettingsPresentationWhenRequiredInputIsNullRejectsTheCall()
    {
        SettingsColorSchemeOption[] schemes =
        [
            new SettingsColorSchemeOption(
                ColorScheme.NeNeDark,
                "ColorSchemeNameNeNeDark",
                ColorScheme.NeNeDark),
        ];

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            null!,
            HiddenItemVisibility.Hidden,
            schemes,
            SettingsSaveStatus.Succeeded,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            SettingsEditorState.Closed,
            null!,
            schemes,
            SettingsSaveStatus.Succeeded,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            SettingsEditorState.Closed,
            HiddenItemVisibility.Hidden,
            null!,
            SettingsSaveStatus.Succeeded,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            SettingsEditorState.Closed,
            HiddenItemVisibility.Hidden,
            schemes,
            null!,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            SettingsEditorState.Closed,
            HiddenItemVisibility.Hidden,
            schemes,
            SettingsSaveStatus.Succeeded,
            null!));
    }

    private static SettingsSnapshot Snapshot(
        UserSettings settings,
        SettingsEditorState editor,
        SettingsPersistenceState persistence)
    {
        System.Reflection.ConstructorInfo constructor = typeof(SettingsSnapshot).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            [typeof(UserSettings), typeof(SettingsEditorState), typeof(SettingsPersistenceState)],
            null) ?? throw new AssertFailedException("The settings snapshot constructor was not found.");
        return (SettingsSnapshot)constructor.Invoke([settings, editor, persistence]);
    }
}
