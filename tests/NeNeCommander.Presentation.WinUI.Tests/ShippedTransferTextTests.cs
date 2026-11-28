using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Checks actual consent and uncertain-result wording against fixed expectations for all shipped locales.</summary>
[TestClass]
public sealed class ShippedTransferTextTests
{
    /// <summary>Source, destination, metadata and all-target scope keep their independent meanings.</summary>
    [TestMethod]
    [DataRow("en-US", "Source: C:\\left\\first\nFile · 17 B · Modified: 2026-11-25 00:00",
        "Destination: D:\\right\\first\nFile · 17 B · Modified: 2026-11-25 00:00",
        "Also overwrite the 1 other eligible files listed below")]
    [DataRow("ja-JP", "コピー元: C:\\left\\first\nファイル · 17 B · 更新日時: 2026-11-25 00:00",
        "保存先: D:\\right\\first\nファイル · 17 B · 更新日時: 2026-11-25 00:00",
        "下に表示した他の対象ファイル 1 件も内容を上書きする")]
    [DataRow("zh-Hans", "源：C:\\left\\first\n文件 · 17 B · 修改时间：2026-11-25 00:00",
        "目标：D:\\right\\first\n文件 · 17 B · 修改时间：2026-11-25 00:00",
        "同时覆盖下方列出的其他 1 个符合条件的文件的内容")]
    public async Task CreateAsyncWhenCatalogChangesPreservesExactConsentMeaning(string locale, string source, string target, string scope)
    {
        PresentationResourceCatalog catalog = new(locale);
        DualPaneSnapshot snapshot = await ConflictModalFixture.AwaitAsync([
            ConflictModalFixture.Conflict("first", null), ConflictModalFixture.Conflict("last", null)]);
        ActiveConflictModal active = Assert.IsInstanceOfType<ActiveConflictModal>(
            DualPanePresenter.Present(snapshot, TestMetadataFormats.Utc, KeyHintVisibility.Shown).ConflictModal);

        ConflictModalText text = ConflictModalText.Create(active, catalog.Get);

        Assert.AreEqual(source, text.SourceText);
        Assert.AreEqual(target, text.TargetText);
        Assert.AreEqual(scope, text.OverwriteScopeText);
        Assert.HasCount(1, text.OtherOverwriteRows);
    }

    /// <summary>Unverified destination contents never become a success or a current-path claim in translation.</summary>
    [TestMethod]
    [DataRow("en-US", "Destination contents may have changed. Not verified.\nLast known path: D:\\right\\source",
        "Source not removed by this operation. Original path: C:\\left\\source")]
    [DataRow("ja-JP", "保存先の内容が変わっている可能性があります（未確認）。\n最後に確認したパス: D:\\right\\source",
        "この操作ではコピー元を削除していません。元のパス: C:\\left\\source")]
    [DataRow("zh-Hans", "目标文件的内容可能已更改（未验证）。\n上次确认的路径：D:\\right\\source",
        "此操作未删除源文件。原路径：C:\\left\\source")]
    public async Task ComposeAsyncWhenCatalogChangesRetainsUncertaintyAndSource(string locale, string target, string source)
    {
        PresentationResourceCatalog catalog = new(locale);
        ProviderStepReceipt receipt = ProviderStepReceipt.Create([], TransferResultFixture.Target("uncertain", TransferPathObservation.LastKnownPath), []);
        TransferResultDetail detail = await TransferResultFixture.RunAsync(OperationKind.Copy,
            ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, receipt),
            ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));

        IReadOnlyList<string> rows = TransferResultFacts.Compose(detail, catalog.Get);

        Assert.HasCount(2, rows);
        Assert.AreEqual(target, rows[0]);
        Assert.AreEqual(source, rows[1]);
    }
}
