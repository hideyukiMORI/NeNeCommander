# 日報 — 2026-10-09(後半: 基準の右サイズ化、並べ替え、Locations、entry metadata)

Status: informational

前半(#167 closure、#94 の環境調査、#169 ハーネス)は
[同日の前半の日報](2026-10-09-ui-evidence-harness-daily-report.md)にある。本稿は hide の方針転換以降を記す。

## hide の決定(2026-10-09)

- #94 の release matrix(100〜300%、8 scheme、high contrast、Narrator、taskbar seam、専用 VM/アカウント)は
  2026-09-02 の規範文書を根拠に AI 側が組んだもので、hide が求めた基準ではない。hide は「DPI が要るときは自分で
  モニタの scale を変える」「hide が一日いる日にまとめて見る」「それ以外にできることを先に進める」と決めた。
- 進め方: 設計リナが次の作業を自分で選び、hide しか決められないことだけを聞いて、merge まで自走する。
  実装・調査・test は Opus 背景席の実装リナに回す(#173 以降は 2 席を並行させた)。

## 統合した PR(すべて canonical gate 成功 → squash → main 同期 → worktree 整理)

| Issue | PR | squash | 内容 |
|---|---|---|---|
| #174 | #175 | `7b2f367` | ADR-0054。QLT-009・CHARTER・KEYBOARD_MODEL を「hide 在席の日に一枚の画面の scale を変えて一通り見る、high contrast を一度見る、記録は Observe mode」に書き直し。Issue #94 本文も同じ手順に書き直し(旧 matrix は折りたたみで保持)。rule ID・件数(112)・gate は不変 |
| #173 | #177 | `257e6a4` | ADR-0053。`PaneSortOrder`(Name/Extension × Ascending/Descending)を `PaneState` が持ち、`EntryOrdering.Apply` が唯一の投影。`Ctrl+F3`/`Ctrl+F4`、同じ key で方向反転、palette に 2 件、状態行に `name ↑` 等。Application 493 件・Presentation 177 件成功、coverage 100/100/92.88/92.93、mutation Application 96.33% / Presentation 94.72% |
| #176 | #180 | `c5c7d8c` | ADR-0055。`IDriveCatalog` + `WindowsDriveCatalog`(`DriveInfo` に触る唯一の内部 enumerator、I/O 境界経由)、既存 `IWslDistributionCatalog` を再利用。`LocationsSession`(ADR-0051 の scope owner、`TransientScopeOwners` は 4 引数)。`Ctrl+G` で開き、j/k/Up/Down/Enter/Escape、選択は `DualPaneSession.NavigateAsync` を一度だけ。全 1126 件成功、coverage 100/100/92.64/93.35、mutation Application 96.03% / Infrastructure 90.38% / Presentation 94.71%。`CommanderSession` は 295 論理行 |

| #178 | #182 | `d229785` | ADR-0056。`EntryMetadata { Visibility, Size, Modified }` を `DirectoryEntry` に束ね(ADR-0024 が予告した CS-013 の束ね直し。`Create` は 4 引数、snapshot は `(name, attributes, size, modified)` で `Kind` は Directory 属性から導出)、`EntryOrdering` に Size/Modified(Unknown は昇順で末尾、同値は名前昇順)、`Ctrl+F5`/`Ctrl+F6`、palette 2 件、状態行 `size`/`modified`。1162 件成功、coverage 100/100/92.67/93.36、mutation Application 96.11% / Infrastructure 91.06% / Presentation 94.77% |
| #179 | #185 | `c6fe1d6` | design handoff `docs/design/2026-10-09-entry-metadata-columns-handoff.md`(proposed)。`EntryMetadataFormatter`(純関数、1024 境界、小数 1 桁、`1023.9 TB` 飽和、`—`)、`PaneRow.SizeText/ModifiedText`、行 template に 2 列(`Auto` 列 + `TextAlignment=Right`)、新 token `DensityRowSizeWidth`=64 / `DensityRowModifiedWidth`=112、時帯は `CreateWindow` が `TimeZoneInfo.Local` を一度渡す。Presentation 216 件成功、coverage 93.82%、mutation 94.97%。見た目と列幅は hide 在席時に 125% で確認 |

canonical run: #175 `37928533460`、#177 `37933142710`、#180 `37941189558`、#182 `37945350284`、#185 `37949029167`。

## 進行中

- **#183 UNC 共有の directory listing**(ADR-0057): 読み取りだけを `ProviderDirectoryReadPort` の 3 番目の分岐に足し、mutation/launch/identity は `ProviderUnavailable` 据え置き、1326/1219 → `AccessDenied`、1231/1232/121/64/59 → `ProviderUnavailable`、到達不能ホストの凍結は OS timeout に任せると宣言、暗黙の credential を SECURITY_MODEL に記載、ADV-021 追加、security deep review を integration readiness で実行。本稿の時点で実装リナが deep review 中。結果は PR と次の引き継ぎ書に記す。
- Issue 化のみ: #184 読み込み中 pane の取り消し(ADR-0027 の改訂を含む)、#181 ネットワークドライブ(読み取りで「削除は常に permanent + 確認、atomic move は volume GUID 一致」が既に満たされていると分かり、範囲を縮小)。

## 判断の記録

- `.psm1` が SEC-011/SEC-005 の走査外になるのを受け入れず、走査対象に加えた(#169、gate の強化)。
- ADR-0053 の「`PaneContentListed.Entries` を公開」は、実装で既存の `PaneState.VisibleEntries` 一本に揃える方が
  第二の露出を作らないため、実装に合わせて ADR を直した。
- `DirectoryEntry.Create` の 5 引数化は CS-013 違反。可視性・サイズ・更新日時はいずれも provider の報告事実なので
  `EntryMetadata` に束ね、snapshot は `Kind` を attributes から導出して 4 引数に収めた(ADR-0056)。
- 割り当てネットワークドライブ(`Z:\`)は path model が `WindowsLocalPath` として parse するため、Locations からの選択
  は address 入力と同じ既存挙動になる。AGENTS の「network path を local NTFS として扱わない」との差は Issue #181
  に切り出し、Locations は種別 `Network` を見せるだけにした。
- 実装リナの commit の `Co-Authored-By` は実際に書いたモデル(`Claude Opus 5.5`)を示す。設計リナの commit は
  `Claude Fable 5.1`。

## 残っている follow-up(hide の承認待ちは引き継ぎ書に集約)

- #181 ネットワークドライブの provider 方針。
- `CommanderWindow` のコンストラクタは 6 引数(App host の配線、変更前から 5)。依存を 1 つの record に束ねる整理が候補。
- `CommanderSession` 295/300 行。次の transient scope は ADR-0051 の再編が先。`DispatchIdleIntentAsync` は 39/40 行。
- Infrastructure.Windows の mutation 90.38〜91.06% は閾値 90 に近い。`WindowsDriveEnumerator` の seam test を足せば
  上がる。
- Locations の Loading 中は Escape が効かない(既存の pane read と同じ挙動)。
- 削除確認 modal・settings entry・file list 行の AutomationId は未宣言(#169 で判明)。
