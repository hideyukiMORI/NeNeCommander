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
