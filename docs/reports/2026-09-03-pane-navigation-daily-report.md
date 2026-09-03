# 日報 — 2026-09-03（第3セッション: 左ペインのナビゲーション）

Status: informational

## 本日の区切り

引き継ぎ書が指定した縦切り「左ペインの focus を keyboard で動かし、ディレクトリへ入って親へ戻る」を [Issue #6](https://github.com/hideyukiMORI/NeNeCommander/issues/6) / [PR #7](https://github.com/hideyukiMORI/NeNeCommander/pull/7) として実装し、squash merge した。`j` / `k` / `G` / `gg` / `Ctrl+D` / `Ctrl+U` / `Space` / `Escape` が focus と selection を動かし、`l` / `Enter` でディレクトリへ入り、`h` / `Backspace` / `Alt+Up` で親へ戻る。

## 完了したこと

- Domain に `FileSystemPath.Parent` を追加した。WindowsLocal / UNC / WSL がそれぞれの root で absence を返し、WSL は LinuxPath も同時に導出する。再 parse はしない。
- Application に `PaneSession` を追加した。`NavigateAsync` と `HandleAsync(UserIntent)` の二つだけで pane を進め、focus / selection は `PaneReducer.Apply`、location 変更は新設の `PaneReducer.Navigate`（selection を消し、親へ戻るときは元 location に focus）を通す。`PaneSnapshot` は閉じた content（Absent / Listed）と activity（Idle / Loading / ReadFailed / ReadCancelled）の積で、失敗しても表示中の一覧は残る。
- 読み込み中の intent は凍結し、新しい navigate に追い越された読み込み結果は navigation token の参照同一性で捨てる。fake port の TaskCompletionSource で決定的に証明した（ADV-016）。
- Presentation の `PaneListingPresenter` を snapshot 入力へ移行し、行・focus entry・status resource key・address text を一つの `PanePresentation` に投影する。`PaneStatus` に `NoListing` / `Loading` を追加した。
- App は mapped intent を session へ転送し、ListView に focus を置いたまま Grid の `PreviewKeyDown`（tunneling）で framework の既定ナビゲーションより先にキーを横取りする。描画後と `Window.Activated` で dispatcher 経由に file list へ focus を戻す。
- ADR-0012、COMMAND_MODEL registry、GLOSSARY、FILESYSTEM_BOUNDARIES、KEYBOARD_MODEL、PROJECT_STATE を更新した。

## ベースラインの欠陥として修正したこと

`gg` chord が実機で完成しなかった。印字キーは raw virtual-key（KeyDown）と produced character（CharacterReceived）の二回 mapper に届き、二回目の `g` の KeyDown が `Other` として pending chord を消していた。mapper は `Other` を chord に影響させず素通しにするよう直し、KEYBOARD_MODEL の文言を合わせた。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- GitHub quality run（PR #7、`f357ccc`）: [`33750214417`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33750214417) 成功、3分2秒。
- GitHub quality run（main、`b60e56d`）: [`33752403998`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33752403998) 成功、3分27秒。
- ローカル deep review（`e74d38b`）: passed（初回 f357ccc は Application 92.73 % で失敗、e74d38b で是正）。
- tests: 185 passed、0 failed、0 skipped（前回 160）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.88%、Presentation.WinUI 95.92%。
- mutation score: Domain 96.88%、Application 96.85%、Infrastructure.Windows 97.39%、Presentation.WinUI 100.00%。
- 実行時: Release の exe を起動し、UI Automation で `j` / `k` / `G` / `Down` / `Ctrl+D`（10 行）/ `Ctrl+U` / `l`（`C:\adobeTemp` へ入る）/ `h`（`C:\` へ戻り adobeTemp に focus）/ `Backspace`（root では no-op）/ `Enter` を確認した。計装ビルドのログで、最初の打鍵から mapper → session → 描画まで通ることを確認した。

## 残した注意点

- CommunityToolkit.Mvvm は導入していない。host が immutable な presentation を control に代入するだけで binding 要件が無いため。binding が必要になった時点で ADR-0003 に従って導入する。
- framework の focus visual（点線枠）は最初の行に残り、選択ハイライトが focus を表す。design handoff で扱う。
- half-page 移動の visible-row capacity は composition root の定数 20。pane の実測へ置き換える。
- 実行時検証中の自動操作がフォアグラウンドを奪い、hide の実キー入力が数打鍵アプリ側へ入った。以後、hide が実キーボードを使っている間は自動操作を行わない。
- 前回からの注意点（同期列挙、entry 間 cancellation の未証明、ダークテーマでの TextBox 不可視、Issue #2、旧 typo path）は継続。

## 次の推奨縦切り

右ペインを二つ目の `PaneSession` として構成し、`Tab` で active pane を切り替える。active pane だけが intent を受け取り、passive pane は表示だけを保つ。active / passive の枠線 token を切り替える。WSL / UNC の read adapter、file mutation、最終デザインは混ぜない。
