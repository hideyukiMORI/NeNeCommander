# 引き継ぎ書 — 2026-10-12後半（#190 再開と日次統合）

Status: informational

状態の基準時点はこの更新時。PR #215 の最終 head `6228ac6158fe1c2295d0e440d4b7f3d74a545a5c` は deep-review/canonical 成功後、squash `b216cb0c1c8e1d1198e559761f08bdbf640ed7c7` で `integrate/2026-10-12` にmerge済み。日次 [PR #217](https://github.com/hideyukiMORI/NeNeCommander/pull/217) は Draft で、main gate / merge / cleanup は未完了。#212 PR #214 も Draft で docs-only Commit-mode CI待ち。

## 最初に読むもの

1. [2026-10-12 後半の日報](../reports/2026-10-12-late-session-daily-report.md)
2. [Project state](../PROJECT_STATE.md) の Current checkpoint と Next focused work
3. [ADR-0060](../adr/0060-wsl-extended-length-win32-paths.md)、[Issue #190](https://github.com/hideyukiMORI/NeNeCommander/issues/190)、[Issue #212](https://github.com/hideyukiMORI/NeNeCommander/issues/212)、[日次統合 Issue #213](https://github.com/hideyukiMORI/NeNeCommander/issues/213)

## 体制と作業境界

今回 hide が指定した分担は Astra が設計、Sol と Luna が実装。NeNe Commanderサナが進行・統合を担う。

当日統合 Issue #213 と `integrate/2026-10-12` は main `c3eddd5` を起点に用意し、#190 PR #215 の squash `b216cb0c1c8e1d1198e559761f08bdbf640ed7c7` まで統合済み。#212 Draft PR #214 はこのbranch向け。#214 のCommit-mode CI logは未確認。main向けの日次PR #217 はDraftで、full gate・merge・cleanupを未完了。これらの最終結果はPR #217を正本として記録する。

## #190 状態

2026-10-12 11:01 JST の診断で末尾ドットの通常 Win32 path 正規化と extended-length path の保持を観察。末尾空白は初回 live fixture では直接試していない。main `c3eddd5` を merge commit `0d9489f` で取り込み、`09cf468` で Read/Enumerate callback を4引数経路へ是正。immediate callback exception、iterator exception、事前 cancellation の3 testsを追加し、NTFS identity を使う末尾空白 testも追加。独立 Sol review が49 logical lineのテストメソッドを指摘し、`fda8a8d` で27/30 lineの2メソッドへ分割。全 assertion 維持を確認し、分割後の `WindowsWslFileSystemTests` は13/13 PASS。

記録済み focused proof: Infrastructure.Windows test project の Release build 0 warning/error、8 classes 85/85 PASS、分割後の `WindowsWslFileSystemTests` 13/13 PASS。既に成功した他テストは再実行していない。Format、`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`（112 conformance / 22 security cases）、`git diff --check` PASS。build / test の正確なコマンドは日報を参照。実 WSL tier は `pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1` を clean `09cf468` で実行し、2026-10-12 13:54:47–13:55:15 JST、Ubuntu 専用 root `/tmp/NeNeCommander-Live-20261012-astra190`、宣言9/Passed9/failed0/skipped0、launcher exit 0。以後 production / launcher / live-case の diff が空なので `fda8a8d` に再利用した。永続証跡は `D:/NeNeCommander/evidence/issue-190/2026-10-12-live/`。`live.trx` SHA-256: `675D131A65B2D5D2E8F62EDD1AA93D5272274E7F0896321AE6369373C3E66FF2`。configured root が空なのを確認して保持。keepalive PID 47220 を停止し、Ubuntu instance も停止（running distro は `Ubuntu-22.04` のみ）。`Ubuntu-22.04` はこの tier に使っていない。`D:/NeNeCommander/outputs/diag-190/` は統合後に整理予定。

PR #215 の最終 head `6228ac6` では canonical run `38207301633` が `Gate depth: Commit mode (base=integrate/2026-10-12, documentOnly=False)` を記録して PASS（112 conformance / 22 security cases）。1つ前の code head `fda8a8d` での `38206742259` も初回の integration-target Commit-mode 分岐の成功例。ADR-0060の文言訂正以外に最終headとの差分はなく、src/tests/eng/.githubは不変。最終 head は15:36:50 JSTにsquash SHA `b216cb0c1c8e1d1198e559761f08bdbf640ed7c7` でintegration branchへmerge。

旧 security deep-review run `38205886174` は Application coverage 途中で14:47 JSTに中断され、`deep-review.json` は `conclusion=failed` / `stage=unknown` / `checks=[]`。通常 test 1299件は pass、live専用9件は skip、Domain coverage 100% の後に停止しており、deep成功ではない。同 run の CodeQL だけは成功し、analysis `1934079055` は0 results / errorなし / open alert 0。Application test DLL の直接局所診断は599/599 pass・branch coverage 100%（約5秒）だったが、停止原因は確定していない。新しい run `38209736588` はhead `6228ac6158fe1c2295d0e440d4b7f3d74a545a5c` で全体 SUCCESS（conclusion `passed`、commit一致）。本体は15:31:39 JSTに完了し、mutation Domain/Application/Infrastructure/Presentation 95.52/96.10/90.26/95.02、canonical tests 1299 pass + live専用9 skip、coverage 100.00/100.00/93.25/94.06。CodeQL analysis `1934256018` は15:34:53 JSTに作成され、results 0 / errorなし / warningなし / open branch alerts 0。全証跡は `D:/NeNeCommander/evidence/issue-190/2026-10-12-security/` に保存済み。中断runと局所診断の補助証跡は `D:/NeNeCommander/evidence/issue-190/2026-10-12-security-cancelled/`。詳細は日報を参照。

次段は [Issue #216](https://github.com/hideyukiMORI/NeNeCommander/issues/216) に記録された WSL → Windows 調査知見。全 batch / 全階層 pre-scan、copy と verify の同一 destination `Child` 導出、case reservation、KeepBoth/Skip/Replace の cross-provider pair、既存 gateway copy → verify → delete を扱う。9P PUA 名の逆方向転送での扱いと nested mount 拒否は未決・未実証。これは調査のみで ADR・実装はなく、#190 統合後に着手する。

## 既に統合済みの運用変更

- #208 / PR #209: GitHub Actions pins 更新。PR #209 squash `3039ad3`、canonical `38195579969` と dependency review `38195559955` pass。Dependabot #206 は置き換え済みで close。
- #210 / PR #211: canonical gate の深度選択を変更。PR #211 squash `c3eddd5`、canonical `38200079459` と dependency review `38200061379` pass。PR 自身は main 宛で workflow 変更を含んだため full gate が記録される。PR #215 の integration-target run `38207301633` は `documentOnly=False` の Commit mode を実証済み。#214 の docs-only `documentOnly=True` 分岐は別途 CI log で確認する。
- この運用の当日 integration issue は #213。#212 は文書のみの変更である。

## #94 と次の作業

#94 は開発 blocker ではなくリリース前の実機確認。NeNe Commander 製品UIの操作、host 設定、表示設定は変更していない。未実施の環境確認は release readiness に残る。

1. まず日次 PR #217 の現在状態を確認し、未完了ならそのPRと最新checkpointに記録された残件だけを進める。#214 の docs-only CI、#217 の full gate、main merge、cleanup は、既に完了しているものを再実行しない。
2. PR #217 が完了済みなら、次は [Issue #216](https://github.com/hideyukiMORI/NeNeCommander/issues/216) の WSL → Windows 調査へ進む。ここに書かれた Draft / pending は文書更新時点の記録であり、再開時の現況はPRとCIから読み直す。
3. #190 worktree / 一時出力は、必要な統合・証跡収載が完了した後、絶対パス・取込状態・未保存/未追跡/無視ファイル・唯一の成果物・稼働参照・リンクを確認して整理する。

#212 の文書更新では build と behavior tests を実行していない。#190 の focused build/test、live proof、deep-review、CodeQL は日報記載のとおり実施済み。今回の確認は Markdown 3ファイルの BOM/CRLF/末尾改行、相対リンク、`git diff --check`。
