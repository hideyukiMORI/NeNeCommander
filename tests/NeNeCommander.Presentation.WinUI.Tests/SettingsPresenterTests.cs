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
        for (int index = 0; index < ColorScheme.All.Count; index++)
        {
            Assert.AreSame(ColorScheme.All[index], presentation.Schemes[index].Scheme);
        }
        Assert.IsTrue(presentation.Schemes[5].IsSelected);
        Assert.AreEqual("ColorSchemeNameDracula", presentation.Schemes[5].NameResourceKey);
        Assert.AreSame(SettingsSaveStatus.Succeeded, presentation.SaveStatus);
        Assert.AreSame(SettingsWarningPresentation.Hidden, presentation.Warning);
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
        Assert.AreSame(SettingsWarningPresentation.Hidden, pending.Warning);
        Assert.AreSame(SettingsSaveStatus.Failed, startupRejected.SaveStatus);
        Assert.AreSame(SettingsWarningPresentation.StartupRejected, startupRejected.Warning);
        Assert.AreSame(SettingsWarningPresentation.SaveFailed, failed.Warning);
        Assert.AreSame(SettingsWarningPresentation.SaveFailed, cancelled.Warning);
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
