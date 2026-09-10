# 日報 — 2026-09-09（SDK ピン更新と mutation runner の真正性）

Status: informational

この日から体制を分けた。Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、実装・診断・
ゲート運転は Opus 5 のバックグラウンド NeNeリナが担当して相互に報告した。

## 前提の崩れと SDK ピン更新（Issue #114）

18:15 にこのマシンの .NET SDK が 10.0.401 に入れ替わり 10.0.400 が消えた。`global.json` は
`rollForward: disable` で 10.0.400 を固定していたため、ローカルの `dotnet` は全て
「A compatible .NET SDK was not found」で止まった。10.0.401 は .NET 10.0.12（2026-09-08、security
servicing、CVE-2026-69439 / 71328 / 69522 / 69304 / 58649）の SDK である。ピン更新は gate の弱体化
ではなく security law に沿う保守と判断した。

[Issue #114](https://github.com/hideyukiMORI/NeNeCommander/issues/114) を
`build/114-dotnet-sdk-10-0-401` で実装し、[PR #115](https://github.com/hideyukiMORI/NeNeCommander/pull/115)
で統合した。変更は `global.json`、`eng/bootstrap.ps1`、`eng/check.ps1`、`eng/conformance.ps1`
（CFG-001 のピン検証と quality workflow の期待テキスト）、`.github/workflows/quality.yml`、
`.github/workflows/security-deep-review.yml`、ADR-0002 への事実追記の 7 ファイルで、
`rollForward: disable` と `allowPrerelease: false` は不変、lock ファイルに差分は無い。

証拠: ローカル full `eng/check.ps1` PASS（669 tests、branch coverage Domain 100.00% /
Application 100.00% / Infrastructure.Windows 92.81% / Presentation.WinUI 93.67%）、
dependency-review run `34366942079`、canonical Ready run `34366988886`、commit `7c18ca00`、
squash merge `ca992888`。新ピンでの scheduled deep review は未実行。

## mutation tier の診断（Issue #99 / #101 の共通ブロッカー）

Draft PR #113（#101）と PR #102（#99）はいずれも Presentation.WinUI mutation の閾値未達で止まって
いた。引き継ぎ書は「小さな修正で回復するか、runner を診断するか」を次の判断として残していた。

設計側の解析で、前回合格 deep run `34239166946`（head `43b60a9`）では static 初期化子の mutant
69 件が全て killed だったのに、#101 の run では同じファイルの同種 mutant が交互に survive/kill に
振れることを確認した。診断リナが trace ログと ground truth で機構を確定した:

- Stryker 4.16.0 の MTP runner は test host を 4 本起こして使い回し、mutant の切替はメモリマップド
  ファイル `stryker-mutant-<slot>.txt` への ID 書き込みだけで行う。isolation も restart も無い。static
  初期化子はプロセスごとに一度しか走らないため、2 件目以降の static mutant は発現しない。
- MTP runner は per-test coverage も取れておらず、4 層で 200/201、871/873、816/816、492/561 の
  mutant が「全テストが cover」と記録される。kill の帰属は再利用 host で落ちたテストにそのまま付く。
- 同一ソース・同一 mutant 集合で MTP は static 50 Survived / 43 Killed、VSTest runner は 90 Killed /
  3 Survived。両方 Killed の 41 件のうち 29 件で MTP の killedBy は無関係な mutant のものだった。
- ground truth: `OperationStatus.MoveAwaitingConflict` / `CopyAwaitingConflict` の `ResourceKey` と
  `KeyboardInput.Create` の null guard を検証するテストは存在しない。VSTest の Survived が正しく、
  MTP の Killed は偽。#99 で記録した「単独 run なら 35/35 Killed」も偽合格だった。
- main `ca99288` を VSTest runner で測った真の score は Domain 95.02% / Application 94.85% /
  Infrastructure.Windows 90.74% / Presentation.WinUI 89.10%。Application と Presentation は現行閾値を
  満たしていない。過去の MTP による合格は証拠にならない。
- 設計側の当初仮説「killedBy が一律 = false kill」は誤りで、一律 killedBy 自体は正常（同じテスト
  クラスが正当に全滅する）。異常サインは static の大量 Survived と無関係な killedBy の誤帰属である。

診断は commit を伴わず、`global.json` の一時変更、csproj の一時変更、一時 worktree はすべて復元・
削除した。診断レポートはセッション scratchpad に保存し、E6 の 4 層レポート JSON も退避した。

## 設計判断: ADR-0048

mutation tier を isolating VSTest host で実行する。canonical な `dotnet test`、coverage、commit
hook、canonical gate、CI は ADR-0006 どおり MTP のまま。`eng/security-policy.json` の
`mutationProjects` に対応する 4 つの test project に `Microsoft.NET.Test.Sdk` 18.9.0（MSTest.Sdk
4.4.0 自身が解決する版）を central pin で常時参照させ、`UseVSTest` は設定しない。これにより VSTest
adapter と testhost が常に出力に同梱され、lock ファイルは gate build と mutation build で同一に
なる。runner の宣言は `stryker-config.json` の `test-runner: vstest` 一箇所で、TST-008 の security
rule は `vstest` と参照の存在を保護する。閾値・mutation level・baseline と exclusion の禁止は不変。
真の score が閾値未達の層は同じ変更で行動テストを足して閾値を守る。

却下した案: MTP のまま static survivor にテストを足す（発現しない mutant は殺せない）、static
初期化子から literal を排する code shape 変更（tool の欠陥に製品を合わせ、非 static の汚染は残る）、
upstream の修正待ち（stryker-net #3742 / PR #3695 未リリース）、未リリース PR の自前ビルド、条件付き
`UseVSTest`（`dotnet test` が壊れ lock も食い違う）、deep-review.ps1 だけに runner 引数を渡す（宣言が
二重になる）、全面 VSTest 化。

## 実装結果（Issue #116、日付上は 2026-09-10 未明に統合）

[Issue #116](https://github.com/hideyukiMORI/NeNeCommander/issues/116) を
`test/116-vstest-mutation-host` に実装し、[PR #117](https://github.com/hideyukiMORI/NeNeCommander/pull/117)
で統合した（commit `bca791ed`、squash merge `0b663c51`）。変更は 20 ファイル:
`Directory.Packages.props` の central pin、4 つの mutation test project の `PackageReference` と
`packages.lock.json`（各 3 エントリ追加・削除ゼロ: `Microsoft.NET.Test.Sdk` / `Microsoft.CodeCoverage` /
`Microsoft.TestPlatform.TestHost` 18.9.0）、`eng/architecture.json` の CS-024 allowlist（ADR 記載漏れ、
本 docs 変更で補記）、`stryker-config.json` の `test-runner: vstest`、`eng/security-check.ps1` の
TST-008 rule と `eng/prove-security.ps1` の negative proof 2 件、行動テスト、ADR-0048 / ADR-0006 /
TST-008 の文書。`eng/deep-review.ps1`、閾値、production code は不変。

追加した行動テストと殺した mutant: Presentation は `OperationStatus` の AwaitingConflict 2 件（static）、
`DualPanePresentation` の null guard 12 件、`KeyboardInput.Create` の null guard 4 件。Application は
`CommanderSession` の引数検証が modal ルーティングより先に起きる契約で 4 件、`SettingsSession` の
`ParamName` 契約で 2 件、`AtomicMoveCapabilityFailed` の guard 1 件。Domain は ADR-0042 の
「raw と canonical を独立に制限する」契約で `FileSystemPath` の 1 件。`CopyRequest` / `MoveRequest` /
`PaneState` / `TransferResolution` の guard 4 件は下流が同名の例外を投げる等価変異として残した。

証拠: ローカル full `eng/check.ps1` PASS（674 tests、branch coverage 100.00 / 100.00 / 92.81 / 93.67）、
ローカル `eng/deep-review.ps1` PASS、dependency-review run `34374571737`、exact-head deep run
`34374617932`、canonical Ready run `34377550574`。CI deep の真の score は Domain 95.52%（192/201、
余裕 1 件）/ Application 95.65% / Infrastructure.Windows 90.62% / Presentation.WinUI 92.27%。
Presentation の static mutant 69 件は Survived 0（killedBy は 63 件が 1 テスト）。Infrastructure は
ローカル 90.74% と CI で 1 mutant ぶれたが原因は未調査。isolating host の CI 実行はこれが初回。

## 当日にやらなかったこと

#101 と #99 の再開、PROJECT_STATE の更新、9/8 分の日報・引き継ぎ書（遡及作成し docs 閉塞で統合）。
実 WinUI・実 WSL の環境証拠（#93 / #94）は未実施のまま。
