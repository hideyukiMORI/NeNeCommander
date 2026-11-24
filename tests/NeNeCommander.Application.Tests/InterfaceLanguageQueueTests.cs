using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves language choices share the existing complete-value queue and its lifecycle.</summary>
[TestClass]
public sealed class InterfaceLanguageQueueTests
{
    private static readonly string[] ExpectedIdentifiers = ["system", "ja-JP", "en-US", "zh-Hans"];

    /// <summary>Proves intervening preference, catalog and sort changes retain the selected language.</summary>
    [TestMethod]
    public async Task SelectWhenOtherPreferencesChangePreservesLanguageAndCompleteSettingsAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();
        BookmarkEntry entry = BookmarkEntry.Create(
            Assert.IsInstanceOfType<BookmarkDisplayNameAccepted>(BookmarkDisplayName.Parse("Saved")).Name,
            Assert.IsInstanceOfType<BookmarkPathAccepted>(BookmarkPath.Parse("C:\\saved")).Path, null, BookmarkShortcutSlot.One);
        BookmarkCatalog catalog = CommanderSessionTestValues.Catalog(entry);
        PaneSortOrder order = PaneSortOrder.Create(SortKey.Size, SortDirection.Descending);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await session.SelectInterfaceLanguageAsync(InterfaceLanguage.Japanese, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await session.SelectColorSchemeAsync(ColorScheme.Dracula, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await session.SelectLaunchHiddenItemVisibilityAsync(HiddenItemVisibility.Shown, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await session.SelectKeyHintVisibilityAsync(KeyHintVisibility.Hidden, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await session.SaveBookmarkCatalogAsync(catalog, observer, CancellationToken.None);
        store.PlanWrite().SetResult(SettingsWriteOutcome.Succeeded());
        await session.SavePaneSortOrderAsync(PaneSide.Right, order, observer, CancellationToken.None);

        Assert.HasCount(6, store.Writes);
        foreach (UserSettings written in store.Writes)
        {
            Assert.AreSame(InterfaceLanguage.Japanese, written.Display.InterfaceLanguage);
        }
        UserSettings latest = session.Current.Settings;
        Assert.AreSame(ColorScheme.Dracula, latest.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, latest.Display.HiddenItemVisibility);
        Assert.AreSame(KeyHintVisibility.Hidden, latest.Display.KeyHintVisibility);
        Assert.AreSame(catalog, latest.Bookmarks);
        Assert.AreEqual(order, latest.PaneSortOrders.Right);
    }

    /// <summary>Proves switching back queues the final value and an old failed write cannot replace it.</summary>
    [TestMethod]
    public async Task SelectWhenLanguageChangesOverlapPreservesLatestRevisionAndWriteOrderAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> firstWrite = store.PlanWrite();
        TaskCompletionSource<SettingsWriteOutcome> secondWrite = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        RecordingCommanderObserver observer = new();

        Task first = session.SelectInterfaceLanguageAsync(InterfaceLanguage.English, observer, CancellationToken.None);
        Task second = session.SelectInterfaceLanguageAsync(InterfaceLanguage.System, observer, CancellationToken.None);
        _ = session.Close();
        Assert.AreSame(InterfaceLanguage.System, session.Current.Settings.Display.InterfaceLanguage);
        Assert.HasCount(1, store.Writes);
        firstWrite.SetResult(SettingsWriteOutcome.Rejected(
            SettingsWriteFailureKind.IoFailure, SettingsDirectoryEffect.NotAttempted, SettingsWriteEffect.None));
        await first;
        await store.WaitForWriteCountAsync(2);

        Assert.AreSame(InterfaceLanguage.System, session.Current.Settings.Display.InterfaceLanguage);
        _ = Assert.IsInstanceOfType<SettingsPersistencePending>(session.Current.Persistence);
        Assert.AreSame(InterfaceLanguage.English, store.Writes[0].Display.InterfaceLanguage);
        Assert.AreSame(InterfaceLanguage.System, store.Writes[1].Display.InterfaceLanguage);
        secondWrite.SetResult(SettingsWriteOutcome.Succeeded());
        await second;
        _ = Assert.IsInstanceOfType<SettingsPersistenceSucceeded>(session.Current.Persistence);
    }

    /// <summary>Proves cancellation preserves the selected value and reports the existing cancelled state.</summary>
    [TestMethod]
    public async Task SelectWhenWriteIsCancelledRetainsLanguageAndCancelledPersistenceAsync()
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        TaskCompletionSource<SettingsWriteOutcome> write = store.PlanWrite();
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        Task pending = session.SelectInterfaceLanguageAsync(
            InterfaceLanguage.SimplifiedChinese, new RecordingCommanderObserver(), CancellationToken.None);

        write.SetCanceled();
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => pending);

        Assert.AreSame(InterfaceLanguage.SimplifiedChinese, session.Current.Settings.Display.InterfaceLanguage);
        _ = Assert.IsInstanceOfType<SettingsPersistenceCancelled>(session.Current.Persistence);
        await session.StopAsync();
    }

    /// <summary>Proves the closed language list cannot be mutated by consumers.</summary>
    [TestMethod]
    public void AllWhenConsumerAttemptsMutationKeepsFourShippedChoices()
    {
        Assert.HasCount(4, InterfaceLanguage.All);
        if (InterfaceLanguage.All is System.Collections.Generic.IList<InterfaceLanguage> mutable)
        {
            _ = Assert.ThrowsExactly<NotSupportedException>(mutable.Clear);
        }
        CollectionAssert.AreEqual(
            ExpectedIdentifiers,
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(InterfaceLanguage.All, static language => language.Identifier)));
    }

    /// <summary>Proves collection synchronization metadata cannot expose writable language storage.</summary>
    [TestMethod]
    [DoNotParallelize]
    public void AllWhenSynchronizationRootIsExposedCannotReplaceLanguage()
    {
        InterfaceLanguage observed = InterfaceLanguage.All[0];
        if (InterfaceLanguage.All is System.Collections.ICollection collection &&
            collection.SyncRoot is InterfaceLanguage[] storage)
        {
            InterfaceLanguage original = storage[0];
            try
            {
                storage[0] = InterfaceLanguage.English;
                observed = InterfaceLanguage.All[0];
            }
            finally
            {
                storage[0] = original;
            }
        }

        Assert.AreSame(InterfaceLanguage.System, observed);
        Assert.AreSame(InterfaceLanguage.System, InterfaceLanguage.All[0]);
    }

    /// <summary>Proves both enumeration contracts and indexed access expose the same closed order.</summary>
    [TestMethod]
    public void AllWhenReadThroughCollectionContractsKeepsExactOrderAndBounds()
    {
        System.Collections.IEnumerator iterator = ((System.Collections.IEnumerable)InterfaceLanguage.All).GetEnumerator();
        int index = 0;
        while (iterator.MoveNext())
        {
            Assert.AreSame(InterfaceLanguage.All[index], iterator.Current);
            Assert.AreEqual(ExpectedIdentifiers[index], InterfaceLanguage.All[index].Identifier);
            index++;
        }

        Assert.AreEqual(4, index);
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => InterfaceLanguage.All[-1]);
        _ = Assert.ThrowsExactly<IndexOutOfRangeException>(() => InterfaceLanguage.All[4]);
    }

    /// <summary>Proves an incomplete language selection cannot reach persistence or alter state.</summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void SelectWhenRequiredArgumentIsNullRejectsBeforeMutation(int missingIndex)
    {
        ScriptedSettingsStore store = new(SettingsReadOutcome.Absent());
        SettingsSession session = new(store, SettingsReadOutcome.Absent(), static _ => { });
        object?[] arguments = [InterfaceLanguage.English, new RecordingCommanderObserver(), CancellationToken.None];
        arguments[missingIndex] = null;

        _ = NullGuardAssertions.AssertInternalMethodNullGuard(
            typeof(SettingsSession), nameof(SettingsSession.SelectInterfaceLanguageAsync), session, arguments);

        Assert.AreSame(UserSettings.Default, session.Current.Settings);
        Assert.IsEmpty(store.Writes);
    }

    /// <summary>Proves missing intent payloads cannot enter the settings route.</summary>
    [TestMethod]
    public void SelectIntentWhenLanguageIsNullRejectsIncompleteChoice()
    {
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(InterfaceLanguageSelection), [null]);
    }
}
