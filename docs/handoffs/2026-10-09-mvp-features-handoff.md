# 引き継ぎ書 — 2026-10-09(後半: 自走の体制、MVP の残り)

Status: informational

## 最初に読むもの

1. [後半の日報](../reports/2026-10-09-mvp-features-daily-report.md)と[前半の日報](../reports/2026-10-09-ui-evidence-harness-daily-report.md)。
2. ADR-0053(並べ替え)、ADR-0054(UI release 確認の右サイズ化)、ADR-0055(Locations)、ADR-0056(entry metadata)。
3. `docs/PROJECT_CHARTER.md` の MVP scope。残りの未実装はここから選ぶ。

## 体制(hide の指示、2026-10-09)

- 設計リナ(Fable 席)が次の作業を自分で選び、Issue・ADR・設計・レビュー・merge・文書を担当する。hide しか決められないこと
  (金銭、hide の機材やアカウント、破壊的操作、charter に反する方向転換)だけを聞く。それ以外で止まらない。
- 実装リナ(Opus 背景席)が実装・調査・test・coverage・mutation・Commit gate・Draft PR を担当する。席ごとに worktree
  `D:/NeNeCommander/wt-<issue>` と branch、出力は `D:/NeNeCommander/outputs/wt-<issue>/`。触るファイルが重なる
  席は直列にする(#176 と #178 は intent/mapper/palette/resw/ADR README が重なるので、#178 は第 1 段だけ並行させ、
  第 2 段は #176 の merge 後に続けた)。
- 実装リナの commit の `Co-Authored-By` は `Claude Opus 5.5`、設計リナは `Claude Fable 5.1`。
- ADR は設計リナが先に書いて席に渡し、実装で食い違えば席が ADR を実装に合わせて直し、報告で挙げる。

## main の状態

- 統合 baseline は `c6fe1d6`(#185)。経路は #175 `7b2f367` → #177 `257e6a4` → #180 `c5c7d8c` → #182 `d229785` → #185 `c6fe1d6` → 本 closure(#186)。
- 全層 deep review の最後の記録は `2ef9cb0`(run `37641629223`)。その後の production 変更(#173/#176/#178/#179)は
  各 PR の局所 mutation(Application 96%台、Infrastructure 90〜91%、Presentation 94%台)と canonical gate で
  証明しており、全層 deep run は scheduled tier に任せている。次に main deep の結果を読んだら、4 層の score と
  CodeQL を PROJECT_STATE に記録する。
- open Issue: #94(配布前の確認、hide 在席の日)、#183(UNC 読み取り、Draft PR 作成中)、#184(読み込み中 pane の取り消し)、#181(ネットワークドライブ、範囲縮小済み)、#186(本 closure)。

## 次の作業(この順で自走する)

1. **#183 UNC 読み取り provider**: 実装リナの Draft PR を受け取り、deep review の結果(4 層 mutation、security proof)を確認して Ready → canonical → squash。ADR-0057 と FS-011 の改訂、ADV-021 が入る。live UNC は未実施と明記する。
2. **#184 読み込み中 pane の取り消し**: ADR を設計リナが書く(ADR-0027 の改訂: 放棄した worker の扱い、`PaneLoading` の凍結範囲、Escape の精度順序)。UNC と割り当てネットワークドライブの両方に効く。
3. **#181 ネットワークドライブ**: 低優先。volume identity query 失敗が fail closed である test と、`WindowsFileIdentifier` の SMB 上の fail closed test を足す程度。
4. **cross-provider transfer**(Windows local ↔ WSL): provider routing の ADR から。
5. **sort の永続化**(settings schema v3)、**Locations での share 列挙**、**UNC の mutation/launch**: hide が欲しがれば。

## hide の承認待ち(増えた分)

- `CommanderWindow` のコンストラクタが 6 引数(#179 で +1、App host の配線)。host の依存を 1 つの record に束ねるか。
- `CommanderSession` が 295/300 論理行。次の transient scope を足す前に、ADR-0051 の再編(dispatch 表の分離など)
  を行うかどうか。
- GLOSSARY の entry visibility の行「名前から導出しない」と、WSL の dot 名による Hidden 分類の食い違い(#178 以前から)。
  文書を直すか、WSL の分類方針を変えるか。
- 削除確認 modal・settings entry(`Ctrl+,` の hint)・file list 行の AutomationId を宣言するか(#169 で判明。
  宣言は production 変更)。
- 以前からの follow-up(held `l` の 11/15 repeat、closed abstract record の copy constructor、commit-mode への
  format check、ADR-0047 catalog への `OpenWindowAdjustment`/`OpenBookmarks`、CS-013 gating、`AsyncWorkOwner`
  survivor、test-platform pin の ADR-0048 trigger、WSL link-entry deletion ADR)。

## 配布前の UI 確認(#94、hide 在席の日)

ADR-0054 のとおり。hide が 1 枚の画面を 100% か 200% にし、app を置いて両 pane・アドレス・status・key hints・
`Ctrl+W` helper・F2/F7/F8 modal を一通り触り、high contrast を一度オンにして同じ画面を見て、元に戻す。記録するなら
`eng/ui-evidence/Invoke-UiEvidence.ps1 -Mode Observe`(入力は送らない)。実行前にチャットで合図を出す
(ツール経由のコンソールは hide に見えない)。

## 作業場所と整理

- 本体 `C:/Users/info/WORKS/NeNeCommander` は main、clean、同期済みを保つ。
- `D:/NeNeCommander/wt-183`(#183 の席が使用中)と `D:/NeNeCommander/wt-186`(本 closure)は、各 PR の統合後に tree 一致・clean・稼働参照なしを確認して削除する。
- `D:/NeNeCommander/outputs/resume-20261008/` は自動承認審査が削除を拒否したため保持(内容は #165 の検証出力)。
- その他の `wt-*` と `outputs/wt-*` は各 PR の merge 時に tree 一致・clean・稼働参照なしを確認して削除済み。
  closure 文書の worktree は PR 統合後に同じ確認で削除する。
