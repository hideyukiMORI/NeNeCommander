# 日報 — 2026-09-04（F6 move）

Status: informational

## 本日の区切り

引き継ぎ書が指定した縦切り「`F6` で active pane の item を passive pane へ move する」を [Issue #15](https://github.com/hideyukiMORI/NeNeCommander/issues/15) / [PR #16](https://github.com/hideyukiMORI/NeNeCommander/pull/16) として実装し、squash merge した。`FileOperationGateway` と Windows local adapter が App に組み込まれ、最初のファイル操作が UI から起動できる。

## 完了したこと

- `DualPaneSession` が `FileOperationGateway` を受け取り、`UserIntent.Move` で active pane の selection（無ければ focus item）を source、passive pane の listed location を destination とする `MoveRequest` を作って実行する。両ペインが listed で focus がある場合だけ動く。
- request の typed rejection（例: destination が source と同一）は `OperationRequestRejected` として残し、gateway には届かない。
- `OperationRunning` の間は `HandleAsync` と `NavigateAsync` が snapshot を変えずに返す（ADV-014 / ADV-016）。完了後は `OperationCompleted(outcome)` を記録し、両ペインを `RefreshAsync` で再読み込みする。
- `PaneSession.RefreshAsync` は同じ location を元の focus を優先して再読み込みし selection を消す。`Ctrl+R`（`UserIntent.Refresh`）も同じ経路を使う。
- `DualPaneSnapshot.Operation`（閉じた `OperationActivity`: Idle / Running / Completed / RequestRejected）を Presentation が `OperationStatus` の resource key に投影し、App は status 行に代入する。
- composition root で `WindowsLocalFileOperationAdapter` → `FileOperationGateway` → `DualPaneSession` を組み、`CommanderApplication` が window の `Closed` で gateway を破棄する。
- ADR-0015、COMMAND_MODEL registry、GLOSSARY（operation activity）、PROJECT_STATE を更新した。

## 是正したこと

- 初回 commit `816ffae` で Presentation の branch coverage が 92.45 % に下がった（operation の完了状態の投影が未検証）。queue 式の fake port で gateway の Succeeded / Rejected / PartiallyCompleted / Cancelled を作り、`8f8046a` で投影を直接証明して 95.75 % に戻した。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- GitHub quality run（PR #16、`8f8046a`）: [`33781084657`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33781084657) 成功、3分42秒。
- GitHub quality run（main、`57ad86e`）: [`33781596495`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33781596495) 成功、3分5秒。
- ローカル deep review（`8f8046a`）: passed。
- tests: 229 passed、0 failed、0 skipped（前回 220）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.53%、Presentation.WinUI 95.75%。
- mutation score: Domain 96.88%、Application 97.23%、Infrastructure.Windows 95.59%、Presentation.WinUI 100.00%。
- 実行時: Release の exe を起動し、UI Automation で両ペインの一覧と `OperationStatus` 行の存在を確認した。`F6` のキー送信は hide の実キーボード利用と競合するため行っていない。

## 残した注意点

- `F6` の実機確認は未実施。hide が手元で右ペインを temp 相当の場所へ移してから左ペインで `F6` を押すと、status 行に結果が出て両ペインが更新される。Windows local の move は copy → verify → delete で、失敗時は source が残る。
- 操作の cancellation UI と progress は未実装（`CancellationToken.None`）。
- status 行は最後の結果を次の操作まで表示し、effect ごとの詳細は出さない。
- 前回からの注意点（metadata identity、byte count 照合、recycle 未実装で常に確認要、同期列挙、ダークテーマ、capacity 定数、Issue #2、旧 typo path）は継続。

## 次の推奨縦切り

`F8`（delete）を接続する。`DeleteRequest` を作り、`PermanentOnly` の provider では gateway が `ConfirmationRequired` を返すので、確認を typed な pane 状態（`OperationAwaitingConfirmation`）として持ち、`Enter` / `Escape` の modal context で `PermanentDeletionConfirmation` を付けて再実行する。KBD-002 の modal context を初めて実動作にする。`F5` copy、collision UI、progress は混ぜない。
