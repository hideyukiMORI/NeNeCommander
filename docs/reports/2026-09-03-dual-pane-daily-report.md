# 日報 — 2026-09-03（第4セッション: dual pane と active 切替）

Status: informational

## 本日の区切り

引き継ぎ書が指定した縦切り「右ペインを二つ目の pane session として構成し、`Tab` で active pane を切り替える」を [Issue #9](https://github.com/hideyukiMORI/NeNeCommander/issues/9) / [PR #10](https://github.com/hideyukiMORI/NeNeCommander/pull/10) として実装し、squash merge した。両ペインが一覧を表示し、intent は active pane にだけ届く。

## 完了したこと

- Application に `PaneSide`（Left / Right、`Other`）、`DualPaneSnapshot`（両ペイン + active side）、`DualPaneSession` を追加した。`ActivateOtherPane` だけが active side を変え、他の intent は active 側の `PaneSession` へ転送する。`NavigateAsync(side, location)` で active に関係なく任意の側へ読み込める。同じ session を両側に渡す composition は defect として拒否する。
- 進行中の読み込みは開始した `PaneSession` に着地するため、active を切り替えても元の pane に反映される。fake port の pending read で決定的に証明した（ADV-016）。
- Presentation に `PaneFrame`（active / passive の semantic border resource key）、`DualPanePresentation`、`DualPanePresenter` を追加した。
- App は `DualPaneSession` と左右の初期 location（`C:\` / `C:\Users`）を受け取り、両ペインを描画し、frame を resource key で適用し、active file list に focus を置く。右ペインに status 行と共有 ItemTemplate を追加し、ラベルを「左ペイン / 右ペイン」にした。
- ADR-0013、COMMAND_MODEL（CMD-002 の active side 所有を `DualPaneSession` に明記）、GLOSSARY（pane side）、PROJECT_STATE を更新した。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- GitHub quality run（PR #10、`5dac7fb`）: [`33756209244`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33756209244) 成功、2分50秒。
- GitHub quality run（main、`0cd053e`）: [`33757652361`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33757652361) 成功、2分49秒。
- ローカル deep review（`758de72`）: passed（初回 5dac7fb は Application 95.11 % と余裕がなく、758de72 で是正）。
- tests: 198 passed、0 failed、0 skipped（前回 185）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.88%、Presentation.WinUI 96.00%。
- mutation score: Domain 96.88%、Application 97.05%、Infrastructure.Windows 97.39%、Presentation.WinUI 100.00%。
- 実行時: Release の exe を起動し、UI Automation で左 `C:\`（27 行、`$RECYCLE.BIN` に focus）と右 `C:\Users`（7 行、`All Users` に focus）の一覧・address・status を読み取り、スクリーンショットで左の active 枠線と右の passive 枠線を確認した。`Tab` とキー操作の送信は hide の実キーボード利用と競合するため行わず、Application の fake port テストで証明した。

## 残した注意点

- `Tab` の実機確認は未実施。hide が手元で `Tab` → `j` → `Tab` を試すと右ペインだけが動くことを確認できる。
- 初期 location は定数（`C:\` / `C:\Users`）。drive 発見と永続化は後続。
- passive pane の framework focus visual は既定のまま（design handoff）。
- 前回からの注意点（同期列挙、entry 間 cancellation の未証明、ダークテーマでの TextBox 不可視、capacity 定数 20、Issue #2、旧 typo path）は継続。

## 次の推奨縦切り

`F5` / `F6` を `FileOperationGateway` に接続する前段として、`IFileOperationPort` の Windows local production adapter（inspect / preflight / copy / verify / delete）を `TestOwnedTemporaryRoot` の contract test 付きで実装する。UI 接続、collision UI、WSL / UNC は混ぜない。
