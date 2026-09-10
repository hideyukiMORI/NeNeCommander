# 日報 — 2026-09-11（#99 統合の締め、docs 閉塞、#93 live WSL harness の再開 checkpoint）

Status: informational

体制は前日と同じ。Fable 5 セッションの NeNeリナが設計・判断・文書、Opus 5 のバックグラウンド
NeNeリナが実装・ゲート運転を担当した。未明に前日の締めを行い、そのまま #93 の再開に入り、hide の
指示で区切りの良い checkpoint で止めた。

## 未明に統合したもの

- [Issue #99](https://github.com/hideyukiMORI/NeNeCommander/issues/99) / PR #102（ADR-0041
  bookmarks）: 実装リナが Opus の session limit で PR 本文更新の直後に停止したが、head `c0c67eae` で
  dependency-review `34491933717`、exact-head deep `34491927767`、canonical `34494972324` がすべて
  緑だったので設計側で squash merge `008c4f03`。main deep run `34498373123` も success。
- [Issue #123](https://github.com/hideyukiMORI/NeNeCommander/issues/123) / PR #124（docs 閉塞）:
  2026-09-10 の日報・引き継ぎ書、PROJECT_STATE、ADR-0049 の drvfs 文言訂正、ADR-0041 の launch
  freeze 文言、KEYBOARD_MODEL の `Ctrl+B` / `Ctrl+1`〜`Ctrl+9` 行。dependency-review `34499085726`、
  canonical `34499096655`、squash merge `41010dca`。

## #93 live WSL harness の再開

### 設計

Draft PR #106（`test/93-live-wsl-harness`）は 2026-09-07 に `WindowsFileIdentifier.Describe`
（`FileIdInfo`）で止まっていた。ADR-0049 で blocker #105 が解けたので、設計側で ADR-0043 に
「Integration with ADR-0049」節を書き、実装リナが挿入した: harness の identity はすべて production
の `WindowsWslFileSystem.Find`（`wsl-v2`）から取り、launcher の assembly load と reflection を撤去、
deterministic test は NTFS の `ReadHandleFacts` seam を注入、directory の token は子の変化で変わる
ので owned mutation 直後に再 capture、ADR-0049 の read-only 3 cell（`stat` 一致、link と target の
token 相違、読み取り後の token 不変）を live tier に追加、launcher は宣言 cell 数と exact 一致の
Passed を要求する。live root は Running 中の Ubuntu-22.04 の `/tmp` 直下に `NeNeCommander-Live-`
prefix で新規作成した（harness 外の WSL 書き込み）。

### 実装と live 実証

`03489f3`（main `41010dca` の merge、競合 2 件は規則どおり）→ `bf2000c`（identity 経路）→ `f603743`
（launcher）→ `4c0c9c4`（cleanup が本体の例外を隠す不備の修正）→ `05c79af`（link fixture）→
`71aed39`（link cleanup）→ checkpoint `2b85bbe`（deterministic seam の 9P 形状）。deterministic
root-safety は 15 → 25 cell、Infrastructure.Windows の isolating mutation は 90.96%（config 不変、
`src` 無変更）、ローカル full `eng/check.ps1` PASS（289 tests + live 6 skip、branch coverage 100.00 /
100.00 / 92.88 / 92.51）。launcher の `--results-directory` の位置バグ（SDK 10.0.401 では live tier が
テスト実行前に必ず落ちていた）と TRX を消していた点も直した。

実 WSL（Windows 11 26200.9445、WSL 2.7.13.0、kernel 6.18.33.2、Ubuntu-22.04）での最終 run
（head `2b85bbe`、2026-09-11 02:42:52〜02:43:03 JST、snapshot clean、exit 0）:

| cell | 結果 |
|---|---|
| owned fixture の inode / nlink / ctime が `stat` と一致 | PASS |
| owned link と target の token が異なる | PASS |
| 読み取りを挟んでも token 不変 | PASS |
| nested copy が source と target を保つ | PASS |
| composite move が検証済み target の後に source を消す | PASS |
| source に owned link を含む転送を zero effect で拒否 | PASS |

**6/6 で未実行ゼロ。copy と composite move が初めて実環境で動いた。** ADR-0036 / ADR-0037 の WSL
mutation は ADR-0049 まで実環境で一度も動いていなかった。cleanup 後の configured root は空・非 link
で retained。未設定時の launcher は 0 executed / 6 skipped / exit 1。TRX と redacted evidence は
セッション scratchpad に保存し、PR #106 本文に記録した。

### 9P の symlink 制約と設計判断

- 非特権プロセスは `\\wsl.localhost` に symlink を作れない。`SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE`
  は local volume 限定で、9P は network redirector として扱われ `ERROR_ACCESS_DENIED` になる
  （Developer Mode 有効でも同じ）。判断: fixture の link 作成だけ owned run child 内で固定引数の
  `wsl.exe --exec ln -s` を使う。product path でも identity でも cleanup でもない setup で、実ユーザーが
  Linux 側で作った link を 9P 越しに観測する場面そのもの。elevated 実行と link cell の切り出しは却下。
- Windows 側から 9P の LX symlink は unlink できない。`File.Delete` は絶対・相対 target のどちらでも
  `ERROR_PATH_NOT_FOUND` で target は残り、`Directory.Delete(recursive)` は通常 entry を消してから link
  で失敗して部分削除になる。判断: cleanup は「owned link を `unlink` で消す → target の identity 不変を
  検証 → no-follow 再列挙で reparse ゼロを確認、残っていれば refusal → Windows 側で再帰削除」の固定順
  にする。`ln -s` と `unlink` が harness が行う唯一の `wsl.exe` 書き込み（`stat` / `ln` / `unlink` の
  3 動詞に固定した一つの runner）。製品側は link entry と link を含む tree の削除を事前に拒否している
  ので部分削除は起きないが、「WSL の link entry を削除する」は未実装 capability として別 ADR が要る。
- NTFS は `$I30` が非常駐化すると directory の `EndOfFile` を非ゼロで返し、ADR-0049 の token は
  それを anomaly として fail closed する。deterministic seam はこの 1 フィールドだけを 9P の形状（0）
  に整える。CI の NTFS でこれに当たった exact-head deep run `34507301168`（`71aed39`）が deterministic
  2 cell で失敗し、seam 修正 `2b85bbe` で解消した（production 無関係、deep 失敗は 1 回）。

### 追認した事後編集

launcher の identity capture 段落への superseding 文、ADR-0043 `Executable proof` の書き換え、
`docs/FILESYSTEM_BOUNDARIES.md` Test safety 段落の ADR-0049 準拠化、TRX の保持。harness 外の WSL
書き込みは専用 root 内で 6 操作（root の mkdir、失敗 run の run child 削除 2 回、unlink 挙動の診断で
probe link の作成と Windows 側削除試行、Windows から消せない link を含む run child の distribution 内
`rm -rf`）。いずれも専用 root の外には触れていない。

## checkpoint

PR #106 は Draft のまま head `2b85bbe`。dependency-review `34509941286` SUCCESS、exact-head deep
review `34509944881` は区切り時点で実行中。Ready・canonical gate・merge は未実施。

## やらなかったこと

#93 の deep 読み戻し・Ready・merge（checkpoint で停止）。#94、#100、follow-up Issue 候補の起票。
統合済み worktree・ローカル branch の後始末は hide に委ねる。
