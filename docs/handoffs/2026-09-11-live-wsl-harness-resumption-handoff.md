# 引き継ぎ書 — 2026-09-11（#93 live WSL harness の再開 checkpoint）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、Opus 5 のバックグラウンド NeNeリナが
実装・ゲート運転を担当する。実装側には完全な仕様・停止規則・報告形式を渡し、判断と文書は設計側が
行う。環境制約に当たった実装リナは停止して報告し、設計側が ADR の追記文を書いて渡す。

## main の状態

- baseline: `41010dca`（docs 閉塞 #123 / PR #124）。その下に #99 `008c4f03`、#105 `94079848`、
  #101 `7b8abe04`。
- 真の score（#99 の exact-head deep `34491927767`）: Domain 95.52%（余裕 1 mutant）/ Application
  95.87% / Infrastructure.Windows 90.86% / Presentation.WinUI 94.30%。main deep `34498373123` success。
- open CodeQL alert はゼロ。

## #93 live WSL harness（Draft PR #106、`test/93-live-wsl-harness`）

| 項目 | 値 |
|---|---|
| worktree | `C:\Users\info\WORKS\NeNeCommander-issue93` |
| checkpoint SHA | `2b85bbe`（push 済み、tree clean） |
| main との関係 | `41010dca` を merge 済み（force push 無し） |
| live root | Ubuntu-22.04 の `/tmp` 直下、`NeNeCommander-Live-` prefix（空・非 link で retained） |
| live 結果 | 6/6 PASS（最終 run 2026-09-11 02:42 JST、exit 0） |
| deterministic | root-safety 25 cell、ローカル full `eng/check.ps1` PASS、Infrastructure mutation 90.96% |
| 済み | dependency-review `34509941286` SUCCESS |
| 実行中 | exact-head deep review `34509944881`（`2b85bbe`。1 回目 `34507301168` は seam の NTFS 形状で失敗、修正済み） |
| 未実施 | deep 読み戻し、Ready、canonical gate、merge、main deep |

### 確定した事実

- ADR-0049 の `wsl-v2` token は実 distribution の `stat` と inode / nlink / ctime が一致し、読み取りを
  挟んでも不変。copy と composite move と link 拒否は実 WSL で PASS（初の実環境動作）。
- 非特権プロセスは `\\wsl.localhost` に symlink を作れない（`ERROR_ACCESS_DENIED`、Developer Mode でも
  同じ）。Windows 側から 9P の LX symlink は unlink できない（`ERROR_PATH_NOT_FOUND`、target は残る）。
  `Directory.Delete(recursive)` は link で止まり部分削除になる。
- 製品は link entry と link を含む tree の削除を事前に拒否するので部分削除は起きない。「WSL の link
  entry を削除する」は未実装 capability で、別 ADR が要る。
- NTFS は directory の `EndOfFile` を非ゼロで返すことがあり、ADR-0049 の token はそれを anomaly と
  して fail closed する。NTFS 上の deterministic seam はこの 1 フィールドだけを 0 に整える。
- SDK 10.0.401 の `dotnet test` では `--results-directory` を `--` の前に置く必要がある。
- `eng/verify-coverage.ps1` は `--no-build` なので、単独実行前（特に Stryker 実行直後）に Release を
  明示 build する。

### 設計判断（ADR-0043 に追記済み）

- harness の identity はすべて `WindowsWslFileSystem.Find`。launcher は reflection を持たない。
- fixture の link は owned run child 内で固定引数の `wsl.exe --exec ln -s`。cleanup は owned link を
  `unlink` → target 不変を検証 → no-follow 再列挙で reparse ゼロ → Windows 側で再帰削除の固定順。
  `ln -s` と `unlink` が harness の唯一の `wsl.exe` 書き込み。

### 再開手順

1. `gh run view 34509944881` で exact-head deep の結果を確認し、artifact から 4 層 score と CodeQL
   （open alert ゼロ、新規なし）を読み戻す。失敗ならテスト側だけで直し（production は触らない）、
   再実行。これが 2 回目の失敗になるので、原因が production 側なら停止して設計判断。
2. `NeNeCommander-issue93` で `git fetch origin`。main が進んでいれば `git merge origin/main` して
   証拠を取り直す。
3. `gh pr ready 106` → canonical gate → `gh pr merge 106 --squash --delete-branch` → main を
   `--ff-only` 同期 → `gh workflow run security-deep-review.yml --ref main`。
4. 統合後、日報・引き継ぎ書・PROJECT_STATE を更新し、#93 の必須 live セル 6/6 PASS を release
   evidence として記録する（root・distribution・user 名は公開文書では redact）。
5. live を再実行するときは `NENE_COMMANDER_WSL_TEST_ROOT` を launcher の process だけに設定し、
   Running の distribution だけを使う。Stopped な distribution には触れない。

## その後の焦点

1. #94 Windows UI release matrix（command palette と bookmark manager の実 WinUI 確認を含む）。
2. #100 window shortcuts（ADR 起草から）。
3. follow-up Issue 候補（hide の承認後に起票）: `OpenBookmarks` の `CommandCatalog` 追加、
   `_defaultFileListFocusSuppressedState` のリセット条件に Bookmarks modal を含める、CS-013 の
   analyzer 強制と record canonical constructor の明文化、`CommanderSession` の分割、
   `verify-coverage.ps1` の stale build 対策、WSL link entry 削除の capability ADR、統合済み
   worktree・ローカル branch の後始末。

## 再開時の注意

- 統合済み branch の worktree（`-103` `-105` `-107` `-109` `-111` `-123` `-125` `-67` `-72` `-73` `-74`
  `-74-mutant-review` `-89` `-97` `-99` `-99-static-mutant-proof` `-120`）とローカル branch は設計側の
  権限では削除できないので hide が消す。`-101` は削除済み。
- `.gitattributes` で `*.ps1 *.json *.md` は CRLF 固定。`sed -i` 禁止。worktree が LF で checkout されて
  いたら、未 commit の作業を先に commit してから index を変えずに renormalize する。
- Stryker の結果は `test-runner: vstest` の config で得たものだけを採用する。Domain の余裕は 1 mutant。
- Opus の session limit で実装リナが止まったら、同じセッションを続きから再開して最終報告を受け取る。
