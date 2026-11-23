using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves settings state projects to one modal and one independent warning.</summary>
[TestClass]
public sealed class SettingsPresenterTests
{
    /// <summary>Proves all 129 schemes and the next-launch visibility project in canonical order.</summary>
    [TestMethod]
    public void PresentWhenEditorIsOpenProjectsEveryChoice()
    {
        SettingsSnapshot snapshot = Snapshot(
            UserSettings.Create(
                DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.System),
                BookmarkCatalog.Empty, PaneSortPreferences.Default),
            SettingsEditorState.Open,
            SettingsPersistenceState.Succeeded);

        SettingsPresentation presentation = SettingsPresenter.Present(snapshot);

        Assert.IsTrue(presentation.IsOpen);
        Assert.IsTrue(presentation.ShowHiddenItemsAtLaunch);
        Assert.IsTrue(presentation.ShowKeyHints);
        Assert.HasCount(129, presentation.Schemes);
        string[] expectedPrefix =
        [
            "ColorSchemeNameNeNeDark", "ColorSchemeNameUbuntu", "ColorSchemeNameNeNeBlack", "ColorSchemeNameNeNeLight",
            "ColorSchemeNameCatalogUbuntuAubergine", "ColorSchemeNameCatalogNeutralLight", "ColorSchemeNameSolarizedDark",
            "ColorSchemeNameSolarizedLight", "ColorSchemeNameMonokai", "ColorSchemeNameDracula",
        ];
        HashSet<string> nameKeys = [];
        for (int index = 0; index < ColorScheme.All.Count; index++)
        {
            Assert.AreSame(ColorScheme.All[index], presentation.Schemes[index].Scheme);
            Assert.IsTrue(nameKeys.Add(presentation.Schemes[index].NameResourceKey));
            Assert.AreEqual(ColorScheme.All[index] == ColorScheme.Dracula, presentation.Schemes[index].IsSelected);
            if (index < expectedPrefix.Length)
            {
                Assert.AreEqual(expectedPrefix[index], presentation.Schemes[index].NameResourceKey);
            }
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

        SettingsSnapshot source = Snapshot(UserSettings.Default, SettingsEditorState.Closed, SettingsPersistenceState.Succeeded);
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            null!,
            schemes,
            SettingsSaveStatus.Succeeded,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            source,
            null!,
            SettingsSaveStatus.Succeeded,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            source,
            schemes,
            null!,
            SettingsWarningPresentation.Hidden));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsPresentation(
            source,
            schemes,
            SettingsSaveStatus.Succeeded,
            null!));
    }

    /// <summary>Proves modal facts come from one source and its rendered options remain frozen.</summary>
    [TestMethod]
    public void SettingsPresentationWhenOptionSourceChangesKeepsOwnedSnapshot()
    {
        SettingsSnapshot source = Snapshot(
            UserSettings.Create(DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.System), BookmarkCatalog.Empty, PaneSortPreferences.Default),
            SettingsEditorState.Open,
            SettingsPersistenceState.Succeeded);
        SettingsColorSchemeOption option = new(ColorScheme.Dracula, "ColorSchemeNameDracula", ColorScheme.Dracula);
        System.Collections.Generic.List<SettingsColorSchemeOption> schemes = [option];
        SettingsPresentation presentation = new(source, schemes, SettingsSaveStatus.Succeeded, SettingsWarningPresentation.Hidden);

        schemes.Clear();

        Assert.IsTrue(presentation.IsOpen);
        Assert.IsTrue(presentation.ShowHiddenItemsAtLaunch);
        Assert.HasCount(1, presentation.Schemes);
        Assert.AreSame(option, presentation.Schemes[0]);
        Assert.IsFalse(presentation.Schemes is System.Collections.ICollection);
        Assert.IsFalse(presentation.Schemes is IList<SettingsColorSchemeOption>);
    }

    /// <summary>Proves hidden footer hints remain a display choice while a save warning stays visible.</summary>
    [TestMethod]
    public void PresentWhenHintsAreHiddenAndSaveFailsRetainsIndependentWarning()
    {
        UserSettings settings = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Hidden, InterfaceLanguage.System),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);
        SettingsWriteRejected rejection = Assert.IsInstanceOfType<SettingsWriteRejected>(SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));

        SettingsPresentation presentation = SettingsPresenter.Present(Snapshot(
            settings, SettingsEditorState.Open, SettingsPersistenceState.Failed(rejection)));

        Assert.IsFalse(presentation.ShowKeyHints);
        Assert.IsTrue(presentation.Warning.IsVisible);
        Assert.AreSame(SettingsSaveStatus.Failed, presentation.SaveStatus);
    }

    /// <summary>Proves all four options retain canonical order and exactly the selected next-launch value.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void PresentWhenLanguageIsSelectedExposesFourOwnedOptions(int selectedIndex)
    {
        InterfaceLanguage selected = InterfaceLanguage.All[selectedIndex];
        UserSettings settings = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Hidden, KeyHintVisibility.Shown, selected),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);

        SettingsPresentation presentation = SettingsPresenter.Present(Snapshot(
            settings, SettingsEditorState.Open, SettingsPersistenceState.Pending));

        Assert.HasCount(4, presentation.Languages);
        for (int index = 0; index < presentation.Languages.Count; index++)
        {
            SettingsLanguageOption option = presentation.Languages[index];
            Assert.AreSame(InterfaceLanguage.All[index], option.Language);
            Assert.AreEqual(index == selectedIndex, option.IsSelected);
            Assert.AreEqual("SettingsLanguage" + option.Language.Identifier, option.AutomationId);
            Assert.IsFalse(string.IsNullOrEmpty(option.NameResourceKey));
        }
        Assert.AreSame(SettingsSaveStatus.Pending, presentation.SaveStatus);
        Assert.IsFalse(presentation.Languages is System.Collections.ICollection);
        Assert.IsFalse(presentation.Languages is IList<SettingsLanguageOption>);
    }

    /// <summary>Proves language option names reference the shipped catalog keys.</summary>
    [TestMethod]
    [DataRow(0, "SettingsLanguageSystem")]
    [DataRow(1, "SettingsLanguageJapanese")]
    [DataRow(2, "SettingsLanguageEnglish")]
    [DataRow(3, "SettingsLanguageSimplifiedChinese")]
    public void LanguageOptionWhenProjectedUsesExactNameKey(int index, string expectedKey)
    {
        SettingsLanguageOption option = new(InterfaceLanguage.All[index], InterfaceLanguage.System);

        Assert.AreEqual(expectedKey, option.NameResourceKey);
    }

    private static SettingsSnapshot Snapshot(
        UserSettings settings,
        SettingsEditorState editor,
        SettingsPersistenceState persistence)
    {
        System.Reflection.ConstructorInfo constructor = typeof(SettingsSnapshot).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            [
                typeof(UserSettings),
                typeof(SettingsEditorState),
                typeof(BookmarksEditorState),
                typeof(SettingsPersistenceState),
            ],
            null) ?? throw new AssertFailedException("The settings snapshot constructor was not found.");
        return (SettingsSnapshot)constructor.Invoke(
            [settings, editor, BookmarksEditorState.Closed, persistence]);
    }
}
