# 日報 — 2026-09-06（設定の永続化）

Status: informational

## 実装した境界

[Issue #74](https://github.com/hideyukiMORI/NeNeCommander/issues/74) を
`feat/74-settings-persistence` に実装し、[PR #92](https://github.com/hideyukiMORI/NeNeCommander/pull/92)
で統合した。ADR-0040 が、完全な schema-v1 document を読み書きする
一つの `ISettingsStore`、選択と ordered write queue を持つ session owner、`Ctrl+,` から開く
settings modal を正本化する。

modal は起動時の hidden-item default と既存 8 color schemes を save-on-change で更新する。
hidden default は現在の各 pane を変えず、`Ctrl+H` は pane ごとの session-only state のまま書き込まない。
scheme は既存 composition-root resource mapping だけを通って現在 session に即時反映される。
保存失敗時も選択を維持し、operation bar と独立した localized persistent warning を表示する。

Windows local adapter は同一 directory の固定 temporary file、flush、`Move` / `File.Replace` による
atomic publish を使う。既存 document、ancestor chain、temporary entry を Win32 file identifier と exact
bytes で再検証する。検出した foreign change、junction、temporary collision は拒否し、cleanup は当該
attempt が作成して identity を再確認できた temporary file だけを対象にする。directory creation と
temporary residue は別の closed effect として返す。最終 path reopen の race は ADR-0040 に残余として
明記した。

最終独立 review で、metadata を復元した同長 temporary rewrite、temporary close 直後と publish 後の
ancestor 差替え、document 自身の file symbolic link、startup read の caller-thread I/O、throwing host
defect observer の task 内吸収、selector 専用の第二 command route を検出した。旧 production に対する
failure-first 実行で各不足を再現し、temporary の exact bytes、承認済み ancestor anchor、direct
non-reparse document、既存 scheduler、raw completion callback、唯一の `CommanderSession.HandleAsync`
へ統一した。

store integrity の旧 production filter は 5 cases 中 4 failures で、途中の document symbolic-link
差替えだけは既存 identity revalidation が既に拒否した。追加修正後は全 5 cases が成功した。続く
open-handle ownership review では、close 後に同じ bytes の foreign temporary を置くと旧実装が成功する
ことも failure-first で再現し、`CreateNew` stream の handle から stable identifier を固定した。
shutdown review では `StopAsync` 後の selection が所有外 write queue を再始動できる不足を再現した。
shutdown と selection を同じ lock で線形化し、停止後の selection は state、通知、I/O を変える前に拒否する。
dangling ancestor junction、document symlink、temporary symlink は既存 entry boundary がすでに拒否することを
3 件の実 NTFS proof で確認し、production の entry 検出経路は増やしていない。

## 失敗から得た証拠

- 最初の Infrastructure mutation は 86.25% で失敗し、baseline、ancestor、temporary ownership、
  publish 後 baseline、queue fault observation の proof 不足を示した。意味のある survivor ごとに実 I/O
  と failure injection を追加した後も 88.69%、89.90%、88.90%、89.69% と段階的に不足を検出した。
  UTF-8 byte 上限、read cancellation、direct unsafe ancestor、復元される parent replacement、消失 temporary
  を独立 proof にし、等価な directory preclassification と到達不能な比較を単純化した後、90.58% に達した。
- `File.Replace` 後も temporary と document の完全な `FileIdentity` が同一だという仮定は、
  move → replace → replace の実 NTFS test を失敗させた。stable Win32 file identifier と exact bytes の
  linkage に修正し、連続正常 write と foreign identical-bytes replacement rejection を別々に証明した。
- coverage の初回再実行は Stryker が残した旧 binary により 46 件の `MissingMethodException` で失敗した。
  `dotnet clean` 後の `--no-build` 実行も binary 不在で失敗したため、Release rebuild 後に同じ coverage
  command を成功させた。これは実装 failure ではなく local harness contamination として区別する。
- post-rebase の最初の conflict は #72 の `Ctrl+H` と #74 の `Ctrl+,` が同じ key-map proof table を変更
  した箇所だけだった。両 binding と FileList 26 / NavigationSurface 7 の closed count を保持した。

## 検証

- `dotnet stryker --config-file ..\..\stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --break-at 90 --output ..\..\artifacts\focused-mutation\NeNeCommander.Infrastructure.Windows-final8 --skip-version-check`
  （working directory: `tests/NeNeCommander.Infrastructure.Windows.Tests`）:
  90.58% PASS（Killed 753、Timeout 7、Survived 42、NoCoverage 37、Ignored 225、CompileError 208）。
  report は local artifact `artifacts/focused-mutation/NeNeCommander.Infrastructure.Windows-final8/reports/mutation-report.json`。
- `dotnet build NeNeCommander.slnx -c Release --no-restore`: PASS、warning 0、error 0。
- post-rebase targeted tests: Application 205、Infrastructure.Windows 185、Presentation.WinUI 78、
  Architecture 5、すべて PASS。
- `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`: PASS、conformance 112 rules、security 18 cases。
- post-rebase `pwsh -NoProfile -File ./eng/verify-coverage.ps1`: Domain 100.00%、
  Application 100.00%、Infrastructure.Windows 92.78%、Presentation.WinUI 93.33%、すべて PASS。

Release executable を起動する ad-hoc low-level input check では、pane item を UI Automation で focus し、
raw VK_OEM_COMMA 188 の無修飾では modal が閉じたまま、Control 付きでは `SettingsOverlay` が開くことを
観測した。script は保存しておらず、後続の再試行は UIA `FindFirst` timeout で停止したため、再現可能な
正本 proof は translator / mapper test matrix とする。外部 settings や user data は変更していない。

## 統合結果と残る release 証拠

Draft の独立 review は、`Ctrl+,` の binding に対する generated hint が欠落していることを検出した。
#73 head 上で統合確認した後、#73 squash merge `495ee51f77ac597b5780df102b0920105e8706ee` を
明示的な base として #74 の 3 commits だけを rebase した。統合済み modifier-label helper に `Ctrl+,`
を追加し、file-list hint は正本順で 9 件となり、最後の `Escape`
より前に `Ctrl+, Settings` を表示する。`OperationAwaitingConflict` は settings entry を拒否する modal
owner として `CommanderSession` の既存 freeze predicate に統合した。
Conflict と settings overlay は同じ native-control modal deferral を使う。`Enter` / `Space` は focus された
button や selector に渡し、`Escape` だけは canonical mapper から各 session owner へ届く。settings の
初期 focus である Close button の `Enter` は、その既存 Click → Escape route で閉じる。

final integration candidate の Release build は warning 0 / error 0、Application 240、
Infrastructure.Windows 209、Presentation.WinUI 86、Architecture 5 が PASS。branch coverage は
Domain 100.00%、Application 100.00%、Infrastructure.Windows 92.77%、Presentation.WinUI 93.29% で PASS。
#73 の feature commits は重複していない。

最初の exact-head deep workflow `34029758777` は head `617b48c` で canonical gate、adversarial tests、
Domain 96.98%、Application 95.17%、Infrastructure.Windows 90.46% mutation を通過したが、
Presentation.WinUI が 88.99% で 90% break threshold を下回った。Draft を維持し、report の
settings presentation survivors を確認した。8 schemes の exact resource/selection、3 save resources、
3 warning resources/visibility、typed null/blank guards を直接観測する test を追加した。また、CodeQL が
test `FileStream` lifetime に報告した 3 threads を、未使用 local の実観測と `IDisposable` owner による
standard lifetime へ修正した。production code、threshold、suppression、waiver は変更していない。

tests-only 修正 head `ddec3badf02d933236b98b259c9f47d5e767c24e` は Presentation project mutation
90.67% を通過した。dependency review `34031503892`、fresh exact-head deep workflow
`34031541883`、Draft-to-Ready canonical workflow `34033250650` が同じ head と base `495ee51f`
で成功した。fresh deep の正式 mutation score は Domain 95.98%、Application 95.58%、
Infrastructure.Windows 90.46%、Presentation.WinUI 90.67%。CodeQL / dependency / secret open alerts と
unresolved review threads は 0。PR #92 は squash merge `b6096d6a1a1bdda24c04bd770ebd0d516ced9c11`
として統合され、Issue #74 は closed になった。

統合状態を正本化する docs-only PR #96 の最初の canonical workflow `34034018423` は、Application test
`HandleAsyncWhenSettingsSelectionsOverlapEnqueuesBothWithoutAwaitingIoAsync` が expected write count 2 に対して
actual 1 を観測して失敗した。production と test code が同一だった直前の PR #92 canonical は成功しており、
原因は先行 write 解放後の複数 asynchronous continuations を固定 20 回の `Task.Yield` で待つ、因果関係のない
test-local helper だった。blind retry は行わず、Issue #97 / PR #98 で両方の重複 helper と 4 呼び出しを、
共有 `ScriptedSettingsStore` の write-recorded generation signal へ置き換えた。count 確認と signal 取得、write
記録と次 generation への交換はそれぞれ同じ lock 内にあり、awaiter 登録前の発火も失わない。既存 test は
両 selection の即時 return、先行完了前の write count 1、完了後だけの後続 write 開始、write 順と最新 state
を引き続き検証する。production、security、workflow、dependency、threshold、suppression は変更していない。
PR #98 は dependency review `34034884120` と canonical Ready workflow `34034893392` に成功し、squash merge
`894b999dc92c574501d7f240ac901331b0c447da` として統合され、Issue #97 は closed になった。

未実施の live WSL release proof は [Issue #93](https://github.com/hideyukiMORI/NeNeCommander/issues/93)、
high contrast、100/150/200/300% DPI、狭幅、8 schemes、keyboard modal の actual-window matrix は
[Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94) が追跡する。必須 cell の skip や未実施は
release readiness の成功として扱わない。
