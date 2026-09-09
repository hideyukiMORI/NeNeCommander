# 引き継ぎ書 — 2026-09-08（address 入力・ペイン履歴・Windows file 起動）

Status: informational

2026-09-09 に遡って作成した。根拠は PR #108 / #110 / #112 の本文、ADR-0044 / 0045 / 0046、
GitHub Actions の run 記録のみ。

## 統合済みの範囲

| Issue | PR | head | 主な証拠 | merge |
|---|---|---|---|---|
| #103 canonical path 長の境界 | #104 | `8fd0da9a` | dependency `34041915916`、exact-head deep `34041924385`、canonical `34043839856` | `0dab3f67` |
| #107 address 入力から active pane 移動 | #108 | `1bbc3541` | dependency `34210612726`、canonical `34210697599` | `f0e3ece6` |
| #109 ペイン単位の戻る・進む履歴 | #110 | `2eeed73d` | dependency `34226675152`、canonical `34226792938` | `4ac98350` |
| #111 focused Windows file の Shell 起動 | #112 | `43b60a99` | dependency `34238742102`、exact-head deep `34239166946`、canonical `34242711243` | `6c9abd89` |

#107 と #109 は parser・provider・filesystem mutation・process・persistence・native boundary を
変えていないため Issue 固有の deep tier を持たない。#111 は Shell への process 起動を伴うため
exact-head deep を必須とし、最初の run `34235788796`（head `6e92050`）が Application mutation
93.94% で止めた後、契約テスト追加と重複 guard 除去を経た `43b60a9` で合格した。2026-09-07 には
main `0dab3f67` に対する scheduled deep run `34095344363` も成功している。

## 不変条件の要点

- address 入力: `CommanderSession` が一つの `AddressEditorState` を所有し、`FileSystemPath.Parse`
  を一度だけ呼ぶ。view が path を開くことはない。
- 履歴: `PaneNavigationHistory` は `PaneState` の一部で、遷移は成功した現在 generation の読取後に
  `PaneReducer` のみが行う。上限 100。第二の navigation/read 経路は無い。
- 起動: `PaneSession` が `OpenFocused` の唯一の判断者。Windows local の File だけを
  `IFileLauncher` へ渡し、WSL/UNC は `ProviderUnavailable`。verb・arguments・preflight を足さない。

## 未完・残余

- Issue #101 command palette は Draft PR #113 の checkpoint `358c294` で停止（Presentation
  mutation 80.98%、CodeQL alert #102 未再解析）。2026-09-09 の引き継ぎ書が所有する。
- 当日分の日報・引き継ぎ書・`docs/PROJECT_STATE.md` 更新は未実施で、2026-09-09 の docs 閉塞変更で
  正本化する。
- 実環境 UI 証拠は Issue #94、live WSL は Issue #93 のまま未実施。
