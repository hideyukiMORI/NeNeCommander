using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves native-query projection never selects, saves or loses the current theme.</summary>
[TestClass]
public sealed class SettingsSchemeViewStateTests
{
    /// <summary>Proves initial state contains every catalog row and the full selected description.</summary>
    [TestMethod]
    public void CreateWhenSettingsOpenKeepsCurrentNameAppearanceAndCanonicalOrder()
    {
        SettingsSchemeViewState state = Create();
        Assert.AreEqual(string.Empty, state.Query);
        Assert.AreEqual(129, state.TotalCount);
        Assert.HasCount(129, state.Rows);
        for (int index = 0; index < ColorScheme.All.Count; index++)
        {
            Assert.AreSame(ColorScheme.All[index], state.Rows[index].Scheme);
            Assert.AreEqual("SettingsScheme_" + ColorScheme.All[index].Identifier, state.Rows[index].AutomationId);
        }
        Assert.AreSame(ColorScheme.Dracula, state.CurrentRow.Scheme);
        Assert.AreEqual("ドラキュラ · Dark", state.CurrentRow.Description);
        Assert.AreSame(state.CurrentRow, state.TabEntry);
    }

    /// <summary>Proves the exact native query matches localized names or identifiers without case-sensitive aliases.</summary>
    [TestMethod]
    [DataRow("DRACULA", "dracula")]
    [DataRow("ドラキュラ", "dracula")]
    [DataRow("NEUTRAL-LIGHT", "neutral-light")]
    public void UpdateQueryWhenNameOrIdentifierMatchesKeepsOnlyOwnedResult(string query, string identifier)
    {
        SettingsSchemeViewState state = Create();
        SettingsSchemeRow current = state.CurrentRow;
        SettingsSchemeRow expected = state.Rows.Single(row => row.Scheme.Identifier == identifier);
        state.UpdateQuery(query);
        Assert.AreEqual(query, state.Query);
        Assert.HasCount(1, state.Rows);
        Assert.AreSame(expected, state.Rows[0]);
        Assert.AreSame(expected, state.TabEntry);
        Assert.AreSame(current, state.CurrentRow);
    }

    /// <summary>Proves filtering retains catalog order, and no-result search keeps the applied theme.</summary>
    [TestMethod]
    public void UpdateQueryWhenResultsChangeKeepsOrderAndUnfilteredCurrent()
    {
        SettingsSchemeViewState state = Create();
        state.UpdateQuery("solarized");
        Assert.HasCount(2, state.Rows);
        Assert.AreSame(ColorScheme.SolarizedDark, state.Rows[0].Scheme);
        Assert.AreSame(ColorScheme.SolarizedLight, state.Rows[1].Scheme);
        Assert.AreSame(state.Rows[0], state.TabEntry);
        state.UpdateQuery(" dracula ");
        Assert.IsEmpty(state.Rows);
        Assert.IsNull(state.TabEntry);
        Assert.AreSame(ColorScheme.Dracula, state.CurrentRow.Scheme);
        Assert.AreEqual(129, state.TotalCount);
    }

    /// <summary>Proves observed applied changes preserve native query, filtered storage and row identities.</summary>
    [TestMethod]
    public void ObserveAppliedWhenThemeChangesKeepsSearchAndRowsEvenOutsideFilter()
    {
        SettingsSchemeViewState state = Create();
        IReadOnlyList<SettingsSchemeRow> all = state.Rows;
        state.UpdateQuery("solarized");
        IReadOnlyList<SettingsSchemeRow> filtered = state.Rows;
        state.ObserveApplied(ColorScheme.SolarizedLight);
        Assert.AreSame(filtered, state.Rows);
        Assert.AreSame(filtered[1], state.TabEntry);
        state.ObserveApplied(ColorScheme.Ubuntu);
        Assert.AreSame(filtered, state.Rows);
        Assert.AreSame(ColorScheme.Ubuntu, state.CurrentRow.Scheme);
        Assert.AreSame(filtered[0], state.TabEntry);
        Assert.AreEqual("solarized", state.Query);
        state.UpdateQuery("solarized");
        Assert.AreSame(filtered, state.Rows);
        state.UpdateQuery(string.Empty);
        Assert.AreSame(all, state.Rows);
    }

    /// <summary>Proves catalog and filtered views expose no mutable synchronization-root storage.</summary>
    [TestMethod]
    public void RowsWhenReadThroughCollectionContractsKeepPrivateStorage()
    {
        SettingsSchemeViewState state = Create();
        AssertPrivateRows(state.Rows);
        state.UpdateQuery("solarized");
        AssertPrivateRows(state.Rows);
        IEnumerator iterator = ((IEnumerable)state.Rows).GetEnumerator();
        int index = 0;
        while (iterator.MoveNext())
        {
            Assert.AreSame(state.Rows[index], iterator.Current);
            index++;
        }
        Assert.AreEqual(2, index);
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => state.Rows[-1]);
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => state.Rows[2]);
    }

    /// <summary>Proves a new open scope resets query and keeps the supplied settings untouched.</summary>
    [TestMethod]
    public void CreateWhenReopenedResetsQueryWithoutChangingSettingsProjection()
    {
        SettingsPresentation presentation = Presentation();
        SettingsSchemeViewState first = SettingsSchemeViewState.Create(presentation, Localize);
        first.UpdateQuery("nothing-matches-this-query");
        first.ObserveApplied(ColorScheme.NeNeLight);
        SettingsSchemeViewState reopened = SettingsSchemeViewState.Create(presentation, Localize);
        Assert.AreEqual(string.Empty, reopened.Query);
        Assert.AreSame(ColorScheme.Dracula, reopened.CurrentRow.Scheme);
        Assert.HasCount(129, reopened.Rows);
        Assert.AreSame(ColorScheme.Dracula, presentation.Schemes.Single(option => option.IsSelected).Scheme);
        Assert.AreSame(InterfaceLanguage.English, presentation.Languages.Single(option => option.IsSelected).Language);
    }

    /// <summary>Proves incomplete view input fails before a query or applied state changes.</summary>
    [TestMethod]
    public void CreateAndUpdateWhenInputIsNullRejectWithoutChangingState()
    {
        SettingsSchemeViewState state = Create();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => SettingsSchemeViewState.Create(null!, Localize));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => SettingsSchemeViewState.Create(Presentation(), null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => state.UpdateQuery(null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => state.ObserveApplied(null!));
        Assert.AreEqual(string.Empty, state.Query);
        Assert.AreSame(ColorScheme.Dracula, state.CurrentRow.Scheme);
    }

    private static void AssertPrivateRows(IReadOnlyList<SettingsSchemeRow> rows)
    {
        Assert.IsFalse(rows is ICollection);
        Assert.IsFalse(rows is IList<SettingsSchemeRow>);
    }

    /// <summary>Proves candidate motion previews without changing the separately accepted row.</summary>
    [TestMethod]
    public void MoveWhenPickerOpensPreviewsCandidatesWithoutAcceptingThem()
    {
        SettingsSchemeViewState state = Create();
        Assert.AreSame(state.CurrentRow, state.SelectedRow);
        Assert.AreSame(ColorScheme.Dracula, state.PreviewScheme);
        state.UpdateQuery("solarized");
        Assert.AreSame(ColorScheme.SolarizedDark, state.PreviewScheme);
        state.MovePrevious();
        Assert.AreSame(ColorScheme.SolarizedDark, state.PreviewScheme);
        state.MoveNext();
        state.MoveNext();
        Assert.AreSame(ColorScheme.SolarizedLight, state.PreviewScheme);
        state.MovePrevious();
        Assert.AreSame(ColorScheme.SolarizedDark, state.PreviewScheme);
        Assert.AreSame(ColorScheme.Dracula, state.CurrentRow.Scheme);
    }

    /// <summary>Proves progress preserves candidate/rows/query and empty results restore the latest accepted theme.</summary>
    [TestMethod]
    public void ObserveAppliedWhenCandidateIsPreviewedKeepsPreviewUntilQueryHasNoResults()
    {
        SettingsSchemeViewState state = Create();
        state.UpdateQuery("solarized");
        state.MoveNext();
        SettingsSchemeRow? candidate = state.SelectedRow;
        IReadOnlyList<SettingsSchemeRow> rows = state.Rows;
        state.ObserveApplied(ColorScheme.Ubuntu);
        state.UpdateQuery("solarized");
        Assert.AreSame(candidate, state.SelectedRow);
        Assert.AreSame(rows, state.Rows);
        Assert.AreSame(ColorScheme.SolarizedLight, state.PreviewScheme);
        Assert.AreSame(ColorScheme.Ubuntu, state.CurrentRow.Scheme);
        state.UpdateQuery("nothing-matches");
        state.MoveNext();
        state.MovePrevious();
        Assert.IsNull(state.SelectedRow);
        Assert.AreSame(ColorScheme.Ubuntu, state.PreviewScheme);
    }

    /// <summary>Proves a clicked row must still belong to this exact filtered view.</summary>
    [TestMethod]
    public void SelectWhenRowIsFilteredOrForeignRefusesItWithoutChangingCandidate()
    {
        SettingsSchemeViewState state = Create();
        SettingsSchemeRow hidden = state.CurrentRow;
        state.UpdateQuery("solarized");
        SettingsSchemeRow? before = state.SelectedRow;
        SettingsSchemeViewState foreign = Create();
        foreign.UpdateQuery("solarized");
        Assert.IsFalse(state.Select(hidden));
        Assert.IsFalse(state.Select(foreign.Rows[0]));
        Assert.AreSame(before, state.SelectedRow);
        Assert.IsTrue(state.Select(state.Rows[1]));
        Assert.AreSame(ColorScheme.SolarizedLight, state.PreviewScheme);
        Assert.AreSame(ColorScheme.Dracula, state.CurrentRow.Scheme);
    }

    private static SettingsSchemeViewState Create()
    {
        return SettingsSchemeViewState.Create(Presentation(), Localize);
    }

    private static string Localize(string key)
    {
        return key switch
        {
            "ColorSchemeNameDracula" => "ドラキュラ",
            "SettingsSchemeDark" => "Dark",
            "SettingsSchemeLight" => "Light",
            "SettingsSchemeDescriptionFormat" => "{0} · {1}",
            _ => key,
        };
    }

    private static SettingsPresentation Presentation()
    {
        UserSettings settings = UserSettings.Create(
            DisplayPreferences.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, KeyHintVisibility.Shown, InterfaceLanguage.English),
            BookmarkCatalog.Empty, PaneSortPreferences.Default);
        System.Reflection.ConstructorInfo constructor = typeof(SettingsSnapshot).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            [typeof(UserSettings), typeof(SettingsEditorState), typeof(BookmarksEditorState), typeof(SettingsPersistenceState)],
            null) ?? throw new AssertFailedException("The settings snapshot constructor was not found.");
        SettingsSnapshot snapshot = (SettingsSnapshot)constructor.Invoke(
            [settings, SettingsEditorState.Open, BookmarksEditorState.Closed, SettingsPersistenceState.Succeeded]);
        return SettingsPresenter.Present(snapshot);
    }
}
