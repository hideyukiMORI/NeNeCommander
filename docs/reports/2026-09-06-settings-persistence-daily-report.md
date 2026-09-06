# 日報 — 2026-09-06（設定の永続化）

Status: informational

## 実装した境界

[Issue #74](https://github.com/hideyukiMORI/NeNeCommander/issues/74) の Draft 候補を
`feat/74-settings-persistence` に実装した。ADR-0040 が、完全な schema-v1 document を読み書きする
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

## 失敗から得た証拠

- 最初の Infrastructure mutation は 86.25% で失敗し、baseline、ancestor、temporary ownership、
  publish 後 baseline、queue fault observation の proof 不足を示した。意味のある survivor ごとに実 I/O
  と failure injection を追加した後も 88.69%、89.90% と段階的に不足を検出した。
- `File.Replace` 後も temporary と document の完全な `FileIdentity` が同一だという仮定は、
  move → replace → replace の実 NTFS test を失敗させた。stable Win32 file identifier と exact bytes の
  linkage に修正し、連続正常 write と foreign identical-bytes replacement rejection を別々に証明した。
- coverage の初回再実行は Stryker が残した旧 binary により 46 件の `MissingMethodException` で失敗した。
  `dotnet clean` 後の `--no-build` 実行も binary 不在で失敗したため、Release rebuild 後に同じ coverage
  command を成功させた。これは実装 failure ではなく local harness contamination として区別する。
- post-rebase の最初の conflict は #72 の `Ctrl+H` と #74 の `Ctrl+,` が同じ key-map proof table を変更
  した箇所だけだった。両 binding と FileList 26 / NavigationSurface 7 の closed count を保持した。

## 検証

- `dotnet stryker --config-file ..\..\stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --break-at 90 --output ..\..\artifacts\focused-mutation\NeNeCommander.Infrastructure.Windows-final4 --skip-version-check`
  （working directory: `tests/NeNeCommander.Infrastructure.Windows.Tests`）:
  90.04% PASS（Killed 612、Timeout 3、Survived 41、NoCoverage 27、Ignored 197、CompileError 134）。
  report は local artifact `artifacts/focused-mutation/NeNeCommander.Infrastructure.Windows-final4/reports/mutation-report.json`。
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

## 残る統合証拠

Draft の独立 review 後、#73 との統合順に従って latest `main` へ再度 rebase する。その final head で
exact-head deep review、dependency review、Draft-to-Ready canonical CI が必要であり、現時点の成功を
merge readiness とは扱わない。
