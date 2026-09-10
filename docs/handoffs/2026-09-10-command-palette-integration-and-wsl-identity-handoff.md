# 引き継ぎ書 — 2026-09-10（#101 統合、#105 実 WSL identity、#99 統合）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、Opus 5 のバックグラウンド NeNeリナが
実装・診断・ゲート運転を担当する。実装側には完全な仕様・停止規則・報告形式を渡し、判断と文書は
設計側が行う。読み取り専用の診断は別の診断リナに切り出すと、設計判断の前に事実が揃う。Opus の
session limit で止まった場合は、同じセッションを続きから再開して最終報告だけを受け取れる。

## main の状態

- baseline: `008c4f03`（#99 / PR #102）。その下に #105 `94079848`、#101 `7b8abe04`、docs `47bfb0c1`。
- 真の score（#99 の exact-head deep `34491927767`）: Domain 95.52%（余裕 1 mutant）/ Application
  95.87% / Infrastructure.Windows 90.86% / Presentation.WinUI 94.30%。
- main の open CodeQL alert はゼロ（#102 は #101 で、#101 は #99 で fixed）。
- ADR-0047（palette）、ADR-0049（WSL 9P identity）、ADR-0041（bookmarks、統合節付き）が accepted。

## 統合済み

| Issue | PR | 内容 | 証拠 | merge |
|---|---|---|---|---|
| #120 | #121 | 未明の日報・引き継ぎ書・PROJECT_STATE | 前日記録どおり | `47bfb0c1` |
| #101 | #113 | ADR-0047 command palette | dependency `34472244089`、exact-head deep `34472326634`、canonical `34474891436`、main deep `34475830281` | `7b8abe04` |
| #105 | #122 | ADR-0049 実 WSL の 9P entry identity（`wsl-v2`） | dependency `34484473296`、exact-head deep `34484497758`、canonical `34487983447`、main deep `34489001939` | `94079848` |
| #99 | #102 | ADR-0041 category bookmarks | dependency `34491933717`、exact-head deep `34491927767`、canonical `34494972324` | `008c4f03` |

## 確定した事実

- CodeQL alert は deep-review workflow の成否に影響せず、API で読み戻す。alert #102 は #107 の code
  由来で main にも open だった。
- 9P namespace には volume serial も 128bit id も `FileStatLx` も無い。inode は mount 間で衝突し、
  列挙由来 id と handle 由来 id は mount point で食い違い、`CreationTime` は `min(atime, mtime, ctime)`
  で読むだけで動く。`FileBasicInfo.ChangeTime` は Linux ctime と一致し読み取りでは不変。fs name は
  `9P`。drvfs は share 越しに `ERROR_ACCESS_DENIED`（`AccessDenied` に閉じる）。
- hide は mount 越し inode 衝突を identity の threat scope 外とすることを受理した（ADR-0049）。
- 実 WSL では `ReadWslFacts` が期待どおりの token を返す（read-only の参考観測。#93 の必須セルは
  未実施）。
- 大きく乖離した branch は `git merge origin/main` で 1 回に解消できる（#99 で 12 file 競合）。
  旧 SDK ピンの branch は main を merge するまで pre-commit hook が落ちる。
- `eng/verify-coverage.ps1` は `--no-build` なので、単独実行前に Release を明示 build する。

## 次の一手

1. #93 live WSL harness（Draft PR #106、`test/93-live-wsl-harness`、HEAD `9ed7b5e`、main から大きく
   後ろ）: blocker #105 が解けたので再開できる。branch 更新は `git merge origin/main`。harness は
   `WindowsWslFileSystem` の inspect/revalidate（= `ReadWslFacts`）だけを identity に使い、runner 側に
   identity を持たない。ADR-0049 Executable proof の live read-only セル（inode / ctime / nlink の
   `stat` 一致、link と target の token 差、読み取り後の token 不変）と live mutation セルを
   `NENE_COMMANDER_WSL_TEST_ROOT` の opt-in で実装する。必須セルの SKIP は PASS に数えない。
   Stopped な distribution は起動してしまうので Running のものだけを対象にする。
2. follow-up Issue 候補（hide の承認後に起票）:
   - `OpenBookmarks` を `CommandCatalog` に追加（ADR-0047 の ordered list 改訂）。
   - `CommanderWindow.RenderSession` の `_defaultFileListFocusSuppressedState` リセット条件を
     Settings だけでなく Bookmarks modal も含める（`SettingsEditorState.Closed` 以外）。
   - CS-013 の analyzer 強制と、sealed record の canonical constructor に対する引数上限の明文化。
   - `CommanderSession`（440 行・25 member）の bookmark navigation 調整役の抽出。
   - `eng/verify-coverage.ps1` が古い build を測らないようにする（build するか、出力が source より
     古ければ失敗する）。
   - 統合済み worktree・ローカル branch の後始末。
3. #94 Windows UI release matrix（command palette と bookmark manager の実 WinUI 確認を含む）、
   #100 window shortcuts（ADR 起草から）。

## 再開時の注意

- 統合済み branch の worktree（`-103` `-105` `-107` `-109` `-111` `-123` `-67` `-72` `-73` `-74`
  `-74-mutant-review` `-89` `-97` `-99` `-99-static-mutant-proof` `-120`）とローカル branch は設計側の
  権限では削除できないので hide が消す。`-101` は削除済み。
- `.gitattributes` で `*.ps1 *.json *.md` は CRLF 固定。`sed -i` 禁止。worktree が LF で checkout されて
  いたら index を変えずに renormalize する。
- Stryker の結果は `test-runner: vstest` の config で得たものだけを採用する。Domain の余裕は 1 mutant。
- 実装リナの commit の `Co-Authored-By` は実際に書いたモデル（Opus）を名乗る。設計リナの commit は
  Fable。`Claude-Session` の URL は共通。
