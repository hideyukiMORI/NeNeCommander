# 2026-11-18 再開用の引き継ぎ

現在の指示と実際のIssue/PRを先に確認する。この日の並列許可を新しい作業の既定へ持ち越さない。
通常本体は`C:/Users/info/WORKS/NeNeCommander`。NeNeFolioのcheckoutでは作業しない。

この継続でhideは「おしまいと言うまでCommanderを続ける」「サブエージェントもいっぱい使う」と指定し、
公開できる区切りでタグ作成・releaseも依頼した。完了の宣言はまだ受けていない。

## 確定したこと

- #417 / PR423（`b22b0b1`）、#422 / PR425（`ca3bfd1`）、#421 / PR426（`1dcba46`）はmainへsquash統合済み。
- 恒久の変更はテスト分割と文書だけ。製品・依存・schema・gate・除外は不変。
- main `1dcba46`のInspectは30 findings。基点67から37件消失、追加0。製品8/テスト22。
- #417の既存850ケース・Architecture21・live13は入力一致を照合して再利用。#422は25件、#421は186件が成功。書式と通常hookも成功。詳細は[日報](../reports/2026-11-18-test-refactor-and-mutant-diagnosis-daily-report.md)。成功済み入力が変わらなければ再試験しない。
- #424は元版/単一source mutantともCI killer2件・coveredBy31件が成功。sourceとassemblyを正確に復帰済み。CIのKilled原因は未確定。Stryker再実行・注入制御の再現はしていない。
- その後の#429はpin済みStryker/VSTestでも2/31 exact IDを実行し、全phaseがPassed、対象1mutantは両方Survived。注入/active ID/実行ID/個別結果を保存した。CI環境・元batch履歴は未再現で原因未確定。[品質記録](../quality/2026-11-18-single-stryker-diagnosis.md)。
- #428 / PR432はmain `97d8a12`へ統合・整理済み。ヘルプの既存20ケースとRelease build成功、Inspectは30→28・追加0。format警告は記録済み。sourceは比較順と値を保つ局所変形。

## 次に選べる範囲

| 範囲 | 現状 / 条件 |
| --- | --- |
| #398 / Draft PR402 | hideのブックマーク画面の実機確認待ち。PRの手順を使う。確認前にmergeしない |
| CS-013 26件 | PR402が触るCommanderWindow、KeyboardHelpPresenter、KeyboardInputTranslator、BookmarkEditorSessionTests、CommanderSessionTests、NullGuardTests、WindowAdjustmentKeyboardTests。競合を避け今回保留 |
| CS-013 2件 | TransientScopeOwners / TransientScopeSnapshotの5引数。ADR-0051の形をhideに判断してもらう |
| #428 | KeyboardHelpViewの2件は統合・整理済み |
| #429の原因追跡 | ローカルStrykerまで完了。次は同じ424入力/1mutant/31 exact IDをCIで1回比較。全層mutationへ広げない |
| #386 / PR420 | Test SDK pinとDependabot ignoreの判断。依存更新を今回採用していない |
| #397 | パレットのタイトルtooltip、hide判断待ち |
| #94 | 配布前のDPI/high contrast等の実機確認。通常開発の阻害要因ではない |
| #430 / #431 | PR402のキー反復・focus修正とApplication3fixtureの分割を別枝で作業中。feature枝へ統合し、hide実機前にPR402をmergeしない |
| #433 | 初回公開前にCodeQLのowned品質指摘37件を修正中。解析成功とAPIゼロ確認を別々に行う |
| #434 | 初回0.1.0-beta.1のWin11 x64 self-contained ZIPを準備中。#94、#433、適用可能な96h以内のdeep review、配布物照合の後にタグ・GitHub prereleaseを公開する |

最新の依存修正版の状況は採用時に公式sourceで再確認し、この日の日報だけを更新根拠にしない。
#386はNuGet専用のignore処理がsecurity updateでも有効になり得るため、設定/pinを据え置いた。
hosted serviceが事前除去する証拠は未確認。原本は`D:/NeNeCommander/evidence/issue-386/`。
CIのmutation scoreはこの局所診断によって訂正していない。

## 証拠と保持領域

恒久証拠は`D:/NeNeCommander/evidence/issue-417/`、`issue-421/`、`issue-422/`、`issue-424/`。
各`completion.json`が統合commit・整理結果、quality文書が検証範囲を示す。
全試行のexact argvとraw log/TRX、source/assembly hash、生成変異、Inspect比較を保存している。

#417/#421/#422/#424/#428のworktreeとoutputsは削除済み。各completion.jsonが確定記録。
#429は文書統合後に同じ確認で整理し、completion.jsonへ結果を記録する。ブランチとcommitは保持する。
未統合の`D:/NeNeCommander/worktrees/issue-398`はPR402の実機待ちで保持する。
進行中の#430/#431/#433/#434の同名worktree/outputsは保持する。重い検証は一つずつ実行。
整理の拒否は発生しておらず、hideに実行してもらう削除コマンドはない。

## 11時台の統合とPreviewの指定

- #429 / PR435はmain `c938274`へ統合し、証拠保全後に作業木と一時出力を整理済み。
- #433 / PR437はmain `860e61d`へ統合。CodeQLの37件に対応する局所変形、対象build・試験・format・通常hookと独立レビューが成功。Inspectの28 findingsは前後同一。CodeQL APIゼロは未確認なので#433は再openして保持する。
- #430 / PR436は`d31248c`、#431 / PR438は`e3c8f4f`としてブックマークfeatureへ統合。101ケースと分割した110ケース、独立レビューが成功。mainを取り込んだfeature `275b464`のquality run 44217730539は成功（全2087、成功2071、live opt-in skip16、失敗0）。coverageはDomain/Application100%、Infrastructure94.48%、Presentation94.49%。PR402は引き続きDraftでhideの画面確認前にmergeしない。
- #434の候補1はglobal RIDがlibrary lockと不一致でNU1004、候補2はpublishにApp PRIが欠けて停止。SDK既存のPRI publish経路を有効にした候補3は586ファイルのZIP/原本hash、1294入力、10lock、51原文notice、11 embedded XBF、日英各348文字列を照合して成功。全失敗ログと試行を保存。版は0.1.0-beta.1、未署名Win11 x64 self-contained。最終配布はmain統合後の正典publishで作る。
- 11:03、hideは実機確認が帰宅後になるためPreviewとして先に公開するよう依頼した。ADR0079にその範囲を記録し、#94は未実施のまま公開説明へ明記する。公開前の自動deep review、CodeQL API照合、配布物照合は維持する。この時点では公開/タグ作成は未実施。
- #439の固定424入力・1mutant・31 IDをCIで比較するmanual workflowは実装・tool38チェック・保存済み実ログ照合・独立レビュー済み。実CIは未実行。今回のPreview sourceから分離し、main受理後1回だけ比較する。元のKilled原因を断定せず、全層mutationへ広げない。

恒久証拠: `D:/NeNeCommander/evidence/issue-430/`、`issue-431/`、`issue-433/`、`issue-434/`、`issue-439/`。
#398/#430/#431はPR402がmain未統合のため、#433/#434/#439は最終受理/証拠収載前のため作業木を保持。
配布候補3つと実機用fixtureも保持する。追加waiverなし、製品設定schema変更なし。

## 公開用sourceの確定とCI比較結果

#434 / PR440はmain `46f1a82`へ統合、依存review44221630000は成功。
最終shippingはこのsourceで正典publishを実行してexit0、警告/エラーなし。
586 payload/ZIP・1295入力・10lock・51原文noticeを照合し、PRIと日英source blobの一致で既存の各348文字列の証拠を再利用。
ZIPは113730832 bytes、SHA256 `c4dad8d8007948ba5455950b9f653853e5971512dcb093e83c73622961910e69`。
場所は `D:/NeNeCommander/releases/0.1.0-beta.1/shipping-20261118/`。
アプリは起動していない。公開用deep review44221900660は同sourceで1回実行中、CodeQL照合と公開は未完了。
#94のIssue本文もADR0079へ合わせ、未実施/openを維持した。

#439 / PR441はその後のmain `43e857e`へ統合、依存review44221977999は成功。
固定source `b5d9af8`のmanual比較44222206602を1回実行し、対象1813はローカル同様Survived。
初期/coverage/mutantの各31 exact IDが全Passed、15注入は正規化一致、424入力と2DLLの復帰が一致。
raw子exit2・wrapper記録final2、外側shell表示exit1/workflow failureを区別する。成功gateとは呼ばない。
artifact14391551154のraw ZIPはservice digest `ad7bf1d6a86a6220cbcc0ae1da5f67dad74a188b5cb0ff7311c3cc4690caec21` と一致し、35内容hash（隠しfile含む36files）も全一致。
環境allowlist JSONは同名のcommand journalで上書きされたためCPU/culture/timezoneは未保存。OSも異なり、全環境一致を主張しない。
固定条件比較は成立し、過去full batchのKilled原因は未確定。再実行・全層展開・score訂正はしない。
最終比較の独立レビューと原本は `D:/NeNeCommander/evidence/issue-439/`、公開用証拠は `issue-434/`。
PR402は引き続きDraftの実機確認待ちで今回のPreviewに含まない。

## ! hide実行待ち：後片付け

同じ日のFolio作業整理で、サナ所有物の削除は自動承認審査に起動前に拒否された。
返された理由は `blocked by policy` のみ。別手段で再試行しない。10:25の読み取り確認では以下6対象が残る。
Commanderの正常完了した整理とは別件。Folioのmain・ブランチ・Issue935・リナの作業・実data・通常exeを対象にしない。

- `D:/NeNeFolio/worktrees/935-window-width`
- `D:/NeNeFolio/outputs/935-window-width`
- `D:/NeNeFolio/agents/935-window-width`
- `D:/NeNeFolio/agents/window-width-20261118`
- `D:/NeNeFolio/design/20261118-search-explanation`
- `D:/NeNeFolio/agents/session-20261118-sana`

準備済みの検査付きコマンド（対象が既に無い場合も扱う。サナは実行していない）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/session-20261118-sana/folio-cleanup-requested.ps1
```

保全と拒否の原本は `D:/NeNeCommander/evidence/session-20261118-sana/`。
後片付けはhideの実行報告と不在確認まで未完了とする。
