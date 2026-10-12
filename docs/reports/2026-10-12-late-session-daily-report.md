# 日報 — 2026-10-12後半（#190 診断・修正の再開、gate 運用と分担の更新）

Status: informational

この記録は #212 の作業更新時点の状態を示す。再開時の修正 checkpoint `0216ade` から調査・実装・focused proof を進め、#190 の code head は `fda8a8df7160242a314894cea5a0ee4d32a0a81f`。PR #215 の最終 head `6228ac6158fe1c2295d0e440d4b7f3d74a545a5c` は canonical run `38207301633` と security deep-review run `38209736588` の成功後、15:36:50 JST に `integrate/2026-10-12` へ squash merge `b216cb0c1c8e1d1198e559761f08bdbf640ed7c7`。CodeQL analysis `1934256018` は結果0・error/warningなし・open branch alert 0。日次 PR #217 は Draft で、full gate と main merge は未実施。#212 PR #214 も Draft でdocs-only Commit-mode CI待ち。

## 作業体制と範囲

今回 hide が明示した分担は、Astra が設計、Sol と Luna が実装。NeNe Commanderサナが進行と統合を担う。

Issue #212 の文書変更は日報、引き継ぎ書、`docs/PROJECT_STATE.md` の3ファイル。Issue #213 の `integrate/2026-10-12` branch は main `c3eddd5` から作成され、#190 PR #215 の squash `b216cb0c1c8e1d1198e559761f08bdbf640ed7c7` を取り込み済み。#212 PR #214 は同branch向けのDraft。main向け日次PR #217 はDraftで、full canonical gate待ち。

## #190 の診断、修正、検証

Issue #190 の live 診断は 2026-10-12 11:01 JST に実施。Ubuntu の専用 root `/tmp/NeNeCommander-Live-20261012c` のみへ fixture を作成し、NeNe Commander 製品UIの操作なしで記録した。元ログ `D:/NeNeCommander/outputs/diag-190/diag.log` は、`D:/NeNeCommander/evidence/issue-190/2026-10-12-live/initial-diagnosis.log` に複製済み。元 output はPR #217の日次統合と必要証跡の収載後に整理予定。

- WSL UNC の通常表記では列挙時に `foo.` が見える一方、`File.Exists` / `FileInfo.Exists` は false。`Path.GetFullPath` は末尾ドットを落とし、`tree/sub./inner` を `tree/sub/inner` にする。
- 同じ path を `\\?\UNC\wsl.localhost\...` の extended-length 形式にすると末尾ドットを保って存在確認・読み取りができる。通常形式での `File.Copy` は `written.` を `written` として作るが、extended-length 形式では `written-ext.` を保つ。
- この診断の `a:b` fixture は、Windows 側で `:` が U+F03A に置換された形で見え、通常形式の存在確認と読み取りが成功した。これは当該 fixture での観察のみであり、逆方向転送を含む一般的な PUA 名の扱いは確定しない（Issue #216 で未決事項として記録）。
- 問題の範囲は `ResolveDirectChild` だけではなく、WSL provider から Win32 API に渡る通常形式の path 全般。存在確認と検証も同じ正規化を通るため、collision 確認を抜けて別名で成功扱いになる可能性がある。

末尾ドットを通常 Win32 path から落とさない ADR-0060 の修正を進めた。main `c3eddd5` を merge commit `0d9489f` で取り込んだ後、`09cf468` で enumeration を遅延 callback に変更し、Read/Enumerate の4引数経路をそろえた。即時 callback 例外、iterator 例外、事前 cancellation の3ケースを追加。実 NTFS identity で末尾空白も扱うテストを追加した（初回 live 診断で末尾空白 fixture は実行していない）。

Sol の独立レビューで49 logical lineのテストメソッドを指摘され、`fda8a8d` で27/30 logical lineの2メソッドへ分割。全 assertion が保持されたことを read-only review した。記録済み proof は Infrastructure.Windows test project の Release build 0 warning/error、8 test classes 85/85 PASS、分割後の `WindowsWslFileSystemTests` 13/13 PASS。既に成功した他のテストは再実行していない。Format、`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`（112 conformance / 22 security cases）、`git diff --check` は PASS。実行コマンドは次のとおり。

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore

dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsDirectoryReadOperationTests|FullyQualifiedName~WindowsLocalDirectoryReaderTests|FullyQualifiedName~WindowsUncDirectoryReaderTests|FullyQualifiedName~WslDirectoryReaderTests|FullyQualifiedName~WindowsDirectoryEnumeratorTests|FullyQualifiedName~WindowsWslFileSystemTests|FullyQualifiedName~WslWin32PathTextTests|FullyQualifiedName~WslTrailingDotGatewayTests' --results-directory D:/NeNeCommander/outputs/wt-190/astra-resume/test-results --report-trx --report-trx-filename focused.trx

dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsWslFileSystemTests' --results-directory D:/NeNeCommander/outputs/wt-190/astra-resume/test-results --report-trx --report-trx-filename split-test.trx
```

live WSL は clean head `09cf468` で次を実行。2026-10-12 13:54:47–13:55:15 JST、専用 Ubuntu root `/tmp/NeNeCommander-Live-20261012-astra190` で宣言9/Passed9/failed0/skipped0、launcher exit 0。新しい末尾ドット階層 cell も PASS。以後 production、launcher、live-case の差分が空のためこの live proof は code head `fda8a8d` に再利用した。

```powershell
$env:TEMP = 'D:/NeNeCommander/outputs/wt-190/astra-live-20261012'
$env:TMP = $env:TEMP
$env:NENE_COMMANDER_WSL_TEST_ROOT = '\\wsl.localhost\Ubuntu\tmp\NeNeCommander-Live-20261012-astra190'
pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1
```

永続 evidence `D:/NeNeCommander/evidence/issue-190/2026-10-12-live/` には `live.trx`、`live.log`、`focused.trx`、`split-test.trx`、`initial-diagnosis.log`、`receipt.json` がある。`live.trx` SHA-256 は `675D131A65B2D5D2E8F62EDD1AA93D5272274E7F0896321AE6369373C3E66FF2`。configured root が空であることを確認して保持。keepalive PID 47220 は停止し、その後 Ubuntu instance の停止も確認（running distro は `Ubuntu-22.04` のみ）。`Ubuntu-22.04` はこの tier に使っていない。末尾空白は NTFS test で identity を確認し、WSL live run で実測したとは記載しない。

PR #215 の最終 head `6228ac6` では canonical run `38207301633` が `Gate depth: Commit mode (base=integrate/2026-10-12, documentOnly=False)` を記録して成功し、112 conformance / 22 security cases も pass した。前 head `fda8a8d` の run `38206742259` も初回の integration-target Commit mode 実証として成功している。PRをDraftに戻してADR-0060の文言を訂正したため、最終headには新しいcanonical runを取得した。PR #215 はこの最終headから15:36:50 JSTにsquash SHA `b216cb0c1c8e1d1198e559761f08bdbf640ed7c7` で `integrate/2026-10-12` へmerge済み。

security deep-review run `38205886174` は code head `fda8a8d` で長時間無出力となり、14:47 JST に中断した。通常 test 1299件は成功し、専用 live WSL 9件はこの通常 gate では skip、Domain coverage 100% の後、Application coverage で停止した。最終 `deep-review.json` は `conclusion=failed` / `stage=unknown` / `checks=[]` であり、deep-review成功とは扱わない。同 run の CodeQL step 自体は成功し、analysis `1934079055` は code head `fda8a8d` に対して結果0、error空、open alert 0。

停止後の局所診断では test DLL を直接 `--coverage --timeout 60s` で実行し、Application 599/599 pass・branch coverage 100% を約5秒で確認。Application src/test と coverage script は main 比で変更なしだった。これは停止原因を確定する証拠ではなく、実行環境依存の可能性を残す。中断証跡、局所診断、CodeQL JSON は `D:/NeNeCommander/evidence/issue-190/2026-10-12-security-cancelled/` に保存済み。

再試行の deep-review run `38209736588` は最終 head `6228ac6158fe1c2295d0e440d4b7f3d74a545a5c` で全体 SUCCESS。deep-review.json の conclusion は `passed` で commit も一致。本体は15:31:39 JSTに完了。mutation score は Domain 95.52%、Application 96.10%、Infrastructure 90.26%、Presentation 95.02%。通常 canonical test は1299 pass、9件の live WSL 専用 test は skip され、計1308件。coverage は Domain 100%、Application 100%、Infrastructure 93.25%、Presentation 94.06%。CodeQL analysis `1934256018` は15:34:53 JSTに作成され、results 0 / error空 / warning空 / open branch alerts 0。成果物 `workflow.log`, `deep-review.json`, mutation JSON 4件、CodeQL analysis/openalerts JSON は `D:/NeNeCommander/evidence/issue-190/2026-10-12-security/` に保存済み。

## WSL → Windows 方向の設計メモ

次段 [Issue #216](https://github.com/hideyukiMORI/NeNeCommander/issues/216) に調査知見を記録済み。copy 前の全 batch / 全階層 pre-scan、copy/verify で同一 destination `Child` 導出、case-insensitive reservation、KeepBoth / Skip / Replace の cross-provider pair、既存 gateway copy → verify → delete の再利用を扱う。9P private-use character 名と nested mount 拒否は未決・未実証。これは調査のみで、ADR・実装はなく、#190 統合後に着手する。

## Actions pin の置き換え #208/#209

Dependabot PR #206 の提案を Issue #208 と PR #209 で置き換えた。`security-deep-review.yml` の CodeQL action 2箇所を v4.38.3 (`24c54180a607b1449ed407dd24f251e4e9147c8d`)、upload-artifact を v7.0.2 (`cf430e030ddbb5b0abf93d22962f4752f3646cd9`) に更新。PR #209 は canonical run `38195579969` と dependency review `38195559955` が pass し、squash SHA `3039ad3` で merge。置き換え対象の #206 は閉じられた。

## Gate 運用変更 #210/#211

hide の 2026-10-12 決定により、required check `canonical-gate` は常に実行し、main 向けで docs 以外を含む変更は full gate、それ以外（当日 integration branch 向け Issue PR と docs-only PR）は Commit mode を選ぶ。check 名・ruleset は維持される。PR #211 は canonical run `38200079459` と dependency review `38200061379` が pass し、squash SHA `c3eddd5` で main に merge。PR #211 自身は workflow / gate 変更を含む main 向け PR であり、run log にはfull gateの実行が記録されている。PR #215 の run `38207301633` は integration branch 向け・変更を含む PR の `Commit mode (documentOnly=False)` 分岐を実証した。文書のみの PR #214 で `documentOnly=True` が選ばれるかは、Draft-to-Ready後の CI log で別途確認する。

## #94 と次の手順

Issue #94 の実機確認は開発 blocker ではなくリリース前の環境確認。今回の文書・#190 の作業では NeNe Commander 製品UIの操作や表示設定変更を行っていない。Required なリリース環境セルが未実施であることは維持して記録し、開発の次作業を止める理由にはしない。

次の統合状態の正本は [日次 PR #217](https://github.com/hideyukiMORI/NeNeCommander/pull/217)。PR #217 は Draft で、main向け full canonical gate・main merge・cleanup は未実施。#212 の PR #214 も Draft で、docs-only `documentOnly=True` Commit-mode CIは未確認。これらの結果と最終main SHAは、成功が実際に確認できた後に記録する。

#212 の文書更新では build と behavior tests を実行していない。#190 の focused build/test と live proof は上記のとおり実施済み。今回の文書確認は Markdown 3ファイルの BOM/CRLF/末尾改行、相対リンク、`git diff --check`。
