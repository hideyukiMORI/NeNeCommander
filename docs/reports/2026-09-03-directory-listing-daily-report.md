# 日報 — 2026-09-03（第2セッション: ディレクトリ一覧）

Status: informational

## 本日の区切り

引き継ぎ書が指定した次の縦切り「Windows directory を一つ読み、左ペインへ表示する」を [Issue #3](https://github.com/hideyukiMORI/NeNeCommander/issues/3) / [PR #4](https://github.com/hideyukiMORI/NeNeCommander/pull/4) として実装し、squash merge した。あわせて、前回のベースラインでは起動できていなかった shell の欠陥を修正し、初めて実行時の証跡を取った。

## 完了したこと

- Application に `IDirectoryReadPort`、`DirectoryReadRequest`（entry boundary 1..10,000）、閉じた `DirectoryReadOutcome`（Succeeded / Cancelled / Failed）、`DirectoryListing`（directories first → 名前 ignore-case → ordinal の決定的整列、duplicate identity / null / 上限超過は typed rejection、completeness と unrepresentable count を保持）、`DirectoryEntry` を追加した。
- Infrastructure.Windows に非再帰の `WindowsLocalDirectoryReader` を実装した。hidden / system も列挙し、`IgnoreInaccessible=false` で拒否ディレクトリを空一覧にせず `AccessDenied` にする。boundary で打ち切り、cancellation は列挙前と各 entry 前に観測する。非 WindowsLocal は `ProviderUnavailable`、ファイルをディレクトリとして読むと `NotFound`。path model が拒否する名前（末尾ドット等）は数えるだけで落とさない。
- Presentation.WinUI に `PaneListingPresenter` / `PanePresentation` / `PaneStatus` を追加し、行・初期フォーカス・status resource key を決定的に決める。文言は resw（en-US / ja-JP）にのみ置く。
- App は初期 location `C:\` を composition root で一度だけ parse し、`Loaded` で左ペインへ描画する。ステータス行と `x:Bind` の ItemTemplate を追加した。
- Windows integration test のために `TestOwnedTemporaryRoot` harness を追加し、CS-018 の scan を `Infrastructure.Windows.Tests` まで許可した（ADR-0011）。負例 proof `platform-api-outside-infrastructure` を `eng/prove-gates.ps1` に追加した。
- ADR-0010（directory read port）、ADR-0011（integration test root）、FS-011、COMMAND_MODEL registry 2 行、GLOSSARY 3 語、CS-018 文言、PROJECT_STATE を更新した。

## ベースラインの欠陥として修正したこと

前回の `a386406` はゲートを全段階通過していたが、`NeNeCommander.App.exe` は起動直後に `XamlParseException` で落ちていた。ゲートも CI もアプリを起動しないため見つかっていなかった。

- `CommanderWindow.xaml` が自身の `Grid.Resources` で DesignTokens を merge しつつ同じ Grid の `Background="{StaticResource SurfaceWindowBrush}"` を先に解決していた → DesignTokens を `App.xaml` で merge。
- `FocusVisualPrimaryThickness` / `FocusVisualSecondaryThickness` は WinUI に存在しない → `BorderActivePaneThickness` / `BorderPassivePaneThickness` token を追加して置換。
- `Window` に `x:Uid` を付けて `Title` を割り当てていた → `CommanderWindowTitle` resource を code-behind で `ResourceLoader` から設定。

## deep review で是正したこと

初回のローカル deep review で新規 adapter に mutant が 2 件生き残った。`EnumerationOptions` が static 初期化子だったため `IgnoreInaccessible` 反転が検出されず、非ディレクトリ HRESULT 変換の両分岐が同じ `NotFound` に落ちていた。options を read ごとの生成へ移し、変換を internal にして直接証明した（`6ae3158`）。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由で 2 回、手動で 1 回）。
- GitHub quality run（PR #4、`6ae3158`）: [`33745807131`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33745807131) 成功、2分53秒。
- GitHub quality run（main、`de8f197`）: [`33746256879`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33746256879) 成功、2分55秒。
- ローカル deep review（`6ae3158`）: passed。敵対的テスト 3 反復、NuGet audit、4 プロジェクトの mutation を完走。CodeQL は scheduled workflow の external step。
- build: 0 warnings、0 errors。
- tests: 160 passed、0 failed、0 skipped（前回 118）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.88%、Presentation.WinUI 96.24%。
- mutation score: Domain 96.93%、Application 96.68%、Infrastructure.Windows 97.39%、Presentation.WinUI 99.30%。
- 実行時: Release ビルドの exe を起動し、左ペインに `C:\` の一覧（ディレクトリ先行・名前順・先頭項目にフォーカス・「一覧を読み込みました」）が表示されることをスクリーンショットと UI Automation（`LeftAddress` / `LeftStatus` / `LeftFileList`）で確認した。

## 残した注意点

- adapter は `Task` 署名の中で同期列挙する。.NET に非同期のディレクトリ列挙がなく CS-016 が `Task.Run` を禁止するため。UI thread から外す scheduling は WSL / UNC adapter の前に ADR で決める。
- entry と entry の間での cancellation は決定的に再現できず、列挙前の観測だけを test で証明している。
- hidden / system entry は `PaneReducer` の visibility transition ができるまで常に表示される。
- App は `FileSystemPath.Parse` を使うため Domain への compiled reference を持つ（既存の Application 参照と同じ inward 方向）。architecture test の期待文字列を更新した。
- Design token は単一（ライト）テーマの placeholder なので、Windows がダークテーマだと TextBox（テーマ前景色）が白いペイン上で見えない。UI Automation では存在とローカライズを確認済み。テーマ対応 token は design handoff で扱う。
- 旧 typo path `C:\Users\info\WORKS\NeNeComander` は空のまま残っている。hide の指示があれば削除する。
- Issue #2（CodeQL の alert 48 件）は未着手。

## 次の推奨縦切り

左ペインの `PaneState` を `DirectoryListing` から生成して `KeyboardIntentMapper` の intent（`j` / `k` / `gg` / `G` / `Ctrl+D` / `Ctrl+U`）で focus を動かし、`OpenFocused` でディレクトリ entry へ入り `NavigateParent` で親へ戻る。その最初の ViewModel と同時に CommunityToolkit.Mvvm を導入する（ADR-0003）。WSL / UNC の read adapter、file mutation、最終デザインは混ぜない。
