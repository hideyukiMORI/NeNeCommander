# 2026-11-18 再開用の引き継ぎ

現在の指示と実際のIssue/PRを先に確認する。この日の並列許可を新しい作業の既定へ持ち越さない。
通常本体は`C:/Users/info/WORKS/NeNeCommander`。NeNeFolioのcheckoutでは作業しない。

## 確定したこと

- #417 / PR423（`b22b0b1`）、#422 / PR425（`ca3bfd1`）、#421 / PR426（`1dcba46`）はmainへsquash統合済み。
- 恒久の変更はテスト分割と文書だけ。製品・依存・schema・gate・除外は不変。
- main `1dcba46`のInspectは30 findings。基点67から37件消失、追加0。製品8/テスト22。
- #417の既存850ケース・Architecture21・live13は入力一致を照合して再利用。#422は25件、#421は186件が成功。書式と通常hookも成功。詳細は[日報](../reports/2026-11-18-test-refactor-and-mutant-diagnosis-daily-report.md)。成功済み入力が変わらなければ再試験しない。
- #424は元版/単一source mutantともCI killer2件・coveredBy31件が成功。sourceとassemblyを正確に復帰済み。CIのKilled原因は未確定。Stryker再実行・注入制御の再現はしていない。

## 次に選べる範囲

| 範囲 | 現状 / 条件 |
| --- | --- |
| #398 / Draft PR402 | hideのブックマーク画面の実機確認待ち。PRの手順を使う。確認前にmergeしない |
| CS-013 26件 | PR402が触るCommanderWindow、KeyboardHelpPresenter、KeyboardInputTranslator、BookmarkEditorSessionTests、CommanderSessionTests、NullGuardTests、WindowAdjustmentKeyboardTests。競合を避け今回保留 |
| CS-013 2件 | TransientScopeOwners / TransientScopeSnapshotの5引数。ADR-0051の形をhideに判断してもらう |
| CS-013 2件 | KeyboardHelpView。PR402外の独立候補、今回未着手 |
| #424の原因追跡 | 同じ1 mutant / test IDでpin済みStryker/VSTestを使い、注入コード・active ID・実行test ID・失敗stackを保全。全層mutationへ広げない |
| #386 / PR420 | Test SDK pinとDependabot ignoreの判断。依存更新を今回採用していない |
| #397 | パレットのタイトルtooltip、hide判断待ち |
| #94 | 配布前のDPI/high contrast等の実機確認。通常開発の阻害要因ではない |

最新の依存修正版の状況は採用時に公式sourceで再確認し、この日の日報だけを更新根拠にしない。
CIのmutation scoreはこの局所診断によって訂正していない。

## 証拠と保持領域

恒久証拠は`D:/NeNeCommander/evidence/issue-417/`、`issue-421/`、`issue-422/`、`issue-424/`。
各`completion.json`が統合commit・整理結果、quality文書が検証範囲を示す。
全試行のexact argvとraw log/TRX、source/assembly hash、生成変異、Inspect比較を保存している。

#417/#421/#422のworktreeとoutputsは削除済み。#424はこの文書のPR統合後に同じ確認で整理し、
completion.jsonへ結果を記録する。ブランチとcommitは保持する。
未統合の`D:/NeNeCommander/worktrees/issue-398`はPR402の実機待ちで保持する。
整理の拒否は発生しておらず、hideに実行してもらう削除コマンドはない。
