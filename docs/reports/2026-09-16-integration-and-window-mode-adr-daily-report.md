# 日報 — 2026-09-16（#93 統合、dependabot 置換、#100 の ADR-0050 / ADR-0051 起草）

Status: informational

体制は 09-11 と同じ。Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、Opus 5 のバックグラウンド
NeNeリナ 3 名（統合担当・調査/置換担当・レビュー担当）が実装・ゲート運転・診断・批判的レビューを担当
した。日付をまたいで 09-17 未明まで続けた。

## #93 live WSL harness の統合（PR #106）

09-11 checkpoint `2b85bbe` の exact-head deep `34509944881` は success（Domain 95.52 / Application
95.87 / Infrastructure.Windows 90.96 / Presentation.WinUI 94.30）。main の docs commit `f7d06c1` を merge
（`9b0fd5a`）し dependency-review `35103241402`、exact-head deep `35103244223`、canonical `35106893640`
が緑になったが、squash merge は ruleset に拒否された。原因は check ではなく
`required_review_thread_resolution`: CodeQL の note 級品質指摘 `cs/linq/missed-select`（alert #103、
`tests/.../LiveWslRootFileSystem.cs`、`71aed39` 由来）が github-advanced-security の review thread として
未解決だった。branch スコープの open alert は 1 件で、main スコープの「ゼロ」とは別物だった。

判断: PR #102 の前例（11 thread すべて `isOutdated`、つまりコード修正で解消）に倣い、resolve で済ませず
テスト側 1 箇所を `.Select` で写像する形に修正した（`64616b5`、動作不変、3 行）。head が変わったので
Draft に戻し、dependency-review `35108623945`、exact-head deep `35108628030`（Domain 95.52 /
Application 95.87 / Infrastructure.Windows 90.86 / Presentation.WinUI 94.19、branch スコープ alert 0）、
canonical `35112483190` を取り直した。alert #103 は `fixed` になり thread は自動で resolved、
`mergeStateStatus` は CLEAN。squash merge `e7c89d2`（2026-09-16T15:11Z）、Issue #93 は自動 close、main
deep `35113714046` success（同 score）。これで ADR-0043 の live harness と、実 WSL での copy /
composite move / link 拒否 6/6 PASS の証拠が main に入った。

## dependabot PR の置換

- #127（codeql-action v4.37.9 → v4.38.0）: tag v4.38.0（annotated `4bd7200e`）→ commit `b96794f0` を
  設計側で照合、allowlist 済み。Issue #129 / PR #131 で置換し squash merge `0867971`。
- #128（dotnet-stryker 4.16.0 → 5.0.0）: 実装リナの probe（main `f7d06c1`、同一 mutant 集合で 4.16.0 と
  5.0.0 を同一マシン比較）で、vstest runner は存続（default）、config は無改変で互換、4 層とも threshold
  内（Presentation は Timeout 再分類で 94.30 → 94.19）、MTP の static 分離は未解決（Presentation static
  13 Killed / 104 Survived、新 `perTestInIsolation` でも 25 / 87、isolation は coverage 取得にしか掛からず
  upstream PR #3695 / Issue #3742 は open）。coverage 帰属の欠陥は解消していた。判断: ADR-0048 は維持し
  pin のみ 5.0.0 に上げる。Issue #130 / PR #132 で置換し、TST-008 の version と ADR-0048 の Pin review
  節、PROJECT_STATE / TEST_STRATEGY の現在形の記述を揃えた。head `374c1ff` で dependency-review
  `35115416276`、exact-head deep `35115416386`（5.0.0 vstest: Domain 95.52 / Application 95.87 /
  Infrastructure.Windows 90.86 / Presentation.WinUI 94.30）、canonical `35118984842` が緑、squash merge
  `9c96f58`。main deep `35120031829` success（同 score、CodeQL 0）。#131 は dependency `35113954455`、
  canonical `35114005723`、squash `0867971`。#127 / #128 は置換済みとして close した。CI では Presentation
  が 94.30 で、probe の 94.19 との差は Timeout 再分類の非決定性による（score の分子は同じ扱い）。

## #100 window shortcuts の ADR-0050 / ADR-0051 起草

#94（UI release matrix）は DPI / high-contrast の専用環境と hide の在席（入力自動化）が要るので、#100
の ADR を先行させた。承認済み方向 A の helper を実トークン（Direction C）で 4 artboard（idle / planned /
refused / 2× 寸法図）に描き、engineering handoff の草案を書いた。

ADR-0050 は実装リナの批判的レビューを 3 巡受けて第 4 版になった。主な設計変更:

- snap 状態の検出（`IsWindowArranged`）は SEC-014 のため却下。maximized / minimized だけ拒否する。
- Application は port を持たず、host が読む → `CommanderSession.AdjustWindow` が同期に計画を返す →
  host が一度だけ適用する。plan を状態に載せると `RenderAfterAsync` の二重 render で二重適用になり、
  `AsyncWorkOwner` 経由では key repeat が落ちるため。
- 全キー消費は `Map` の専用分岐、chord 判定は「context で宣言済みか」に変更（新キーで `gg` を壊さない）。
- 境界規則は caption 規則一本（top edge 行に min(step, width) の可視線分 + 下方向 1 step）、enlarge /
  shrink は左上固定、最小値は `OverlappedPresenter.PreferredMinimum*` を唯一の機構にする。
- `=` 別名は JIS で反転するため削除。planned トーンは `TextPrimaryBrush`（新色キー無し）。
- coverage exclusion は不要（App は測定対象外）かつ QLT-008 を壊すので撤回。
- `CommanderSession` は既に CS-013（324 論理行 > 300）を超えており、QLT-010 により先に分割が要る。
  ADR-0051 として独立 Issue に切り出した: palette と address の状態・検証を `SettingsSession` 形の
  owner に移し、dispatch と pane 副作用は残す。ctor / snapshot の 4 引数上限は closed record
  （`TransientScopeOwners` / `TransientScopeSnapshot`）で回避。ADR-0047:48 と ADR-0044:30、
  COMMAND_MODEL の palette 行を明示的に supersede する。CS-013 超過は他に 5 型（最大 580 行）あるので、
  ゲート化は別決定として切り離し、行数は PR に記録するだけにした。

両 ADR は `proposed` のまま scratchpad にあり、hide の確認事項（`Ctrl+W`、左上固定の拡大縮小、`m`/`r` の
冪等 2 命令、ADR-0051 の分割着手）を待って起票する。

## やらなかったこと

#94、#100 の実装、ADR-0051 の Issue 起票、統合済み worktree（`-issue93` `-129` `-130`）の後始末。
