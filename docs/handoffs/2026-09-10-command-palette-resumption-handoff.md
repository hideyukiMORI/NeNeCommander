# 引き継ぎ書 — 2026-09-10（ADR-0048 の後始末と #101 の再開 checkpoint）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、Opus 5 のバックグラウンド NeNeリナが
実装・診断・ゲート運転を担当する。実装側には完全な仕様・停止規則・報告形式を渡し、判断と文書は
設計側が行う。Opus の session limit で停止することがあるので、実装側には区切りごとの commit を
指示する。

## main の状態

- baseline: `f25cfb26`（docs 閉塞 #118 / PR #119）。その下に ADR-0048 の `0b663c51`、SDK ピン
  `ca992888`。
- mutation tier は isolating VSTest host（`stryker-config.json` の `test-runner: vstest`、TST-008 が
  保護）。真の score は Domain 95.52%（余裕 1 mutant）/ Application 95.65% / Infrastructure.Windows
  90.62% / Presentation.WinUI 92.27%。MTP 時代の score は証拠にしない。

## #101 command palette（Draft PR #113、`feat/101-command-palette`）

| 項目 | 値 |
|---|---|
| rebase 先 | main `f25cfb2b` |
| checkpoint SHA | `41bbdf9b`（push 済み） |
| 真の score（checkpoint） | Presentation.WinUI 94.14%、Application 95.72% |
| ローカル full `eng/check.ps1` | `PASS` |
| Release build / focused tests | 0 警告、Application 296/296、Presentation.WinUI 115/115 |
| 未実施 | exact-head deep review（CodeQL alert #102 の再解析を含む）、Ready、canonical gate、merge |

branch の `docs/reports/2026-09-09-command-palette-daily-report.md` と
`docs/handoffs/2026-09-09-command-palette-handoff.md` の「2026-09-10 checkpoint」節に、追加テストと
殺した mutant（file:line）、等価変異として残した 6 件の理由、rebase の競合解消が記録されている。

### 再開手順

1. `C:\Users\info\WORKS\NeNeCommander-101` で `git fetch origin` し、main が進んでいれば
   `git rebase origin/main`（docs のみなら競合しない）。
2. PR #113 本文の Checkpoint / Verification / Status 節を checkpoint の事実に更新する（末尾の
   attribution 2 行を保つ）。
3. dependency-review 合格後、`gh workflow run security-deep-review.yml --ref feat/101-command-palette`
   で exact-head deep review を起動し、artifact から 4 層の score と CodeQL alert #102 の状態を確認する。
   失敗はテスト側で解決し、閾値・ルール・除外は変えない。
4. `gh pr ready 113` → canonical gate → `gh pr merge 113 --squash --delete-branch`（main が worktree に
   占有されていると exit 1 になるが merge は成立している。`gh pr view --json state` で確認し worktree と
   ローカル branch を削除）→ main を `--ff-only` で同期。
5. 統合後、日報・引き継ぎ書・PROJECT_STATE を更新する。

## その後の焦点

1. #99 bookmarks（Draft PR #102、`feat/99-bookmark-manager`、HEAD `615f47f`、main から 8 commits
   先・6 commits 後ろ、111 files）: #101 統合後に rebase（`KeyboardIntentMapper` / `UserIntent` /
   `KeyBinding` が両方で変わるため競合が見込まれる）。Presentation / Application / Infrastructure の
   真の生存に契約テストを足し、記録にある CodeQL alert #101 を再解析し、exact-head deep → Ready →
   merge。#99 の記録にある「単独 run 35/35 Killed」は偽合格なので根拠にしない。
2. #93 live WSL（blocker #105: 実 WSL UNC で `FileIdInfo` が `ERROR_NOT_SUPPORTED`、legacy query は
   volume serial 0 で識別強度が保てない）。identity の単一 owner を保ったまま解く設計判断が必要。
3. #94 Windows UI release matrix、#100 window shortcuts。

## 注意

- `.gitattributes` で `*.ps1 *.json *.md` は CRLF 固定。`sed -i` は使わない。
- Domain の mutation 余裕は 1 mutant。Domain を触る変更は同時に生存を減らす。
- 実装リナの Stryker 結果は必ず `test-runner: vstest` の config で得たものだけを採用する。
