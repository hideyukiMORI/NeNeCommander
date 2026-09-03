# ディレクトリ一覧 引き継ぎ書 — 2026-09-03

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `de8f197ef0d25372cb2433003f2ec35a3d88bf54`
- Completed scope: [Issue #3](https://github.com/hideyukiMORI/NeNeCommander/issues/3) / [PR #4](https://github.com/hideyukiMORI/NeNeCommander/pull/4)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-03-initial-foundation-handoff.md`](2026-09-03-initial-foundation-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain は path parse と provider-native identity を所有する（変更なし）。
- Application は immutable pane state、reducer、typed intent、operation gateway に加えて、`IDirectoryReadPort`、`DirectoryReadRequest`、`DirectoryReadOutcome`、`DirectoryListing`、`DirectoryEntry` を所有する。整列（directories first → 名前 ignore-case → ordinal）と上限（10,000）は `DirectoryListing.Create` が決める。
- Infrastructure.Windows は `WindowsLocalDirectoryReader` を所有する。WindowsLocal 以外は `ProviderUnavailable` で閉じる。HRESULT は `WindowsFileFailureNormalizer` に委ね、非ディレクトリ handle（0x80070057）だけを adapter で `NotFound` にする。
- Presentation.WinUI は keyboard mapping に加えて `PaneListingPresenter` を所有する。`PaneStatus` は resource key だけを持ち、文言は App の resw にある。
- App は composition root で初期 location `C:\` を一度だけ parse し、`CommanderWindow` が `Loaded` で port を呼んで control に代入する。code-behind は presentation の値を代入するだけで判断しない。

ファイル変更は引き続き `FileOperationGateway` だけを通す。読み取りは `IDirectoryReadPort` だけを通す。第二の列挙経路や第二の listing 型を作らない。

## 動作している画面

Release の `NeNeCommander.App.exe` を起動すると、タイトル「NeNe Commander」、左ペインに `C:\` の一覧、ステータス行、先頭項目のフォーカスが表示される。右ペインは空の placeholder のまま。keyboard intent は `IntentMapped` event として発行されるが、まだ何も購読していない。

## テスト harness

`tests/NeNeCommander.Infrastructure.Windows.Tests/TestOwnedTemporaryRoot.cs` が唯一の実ファイルシステム fixture である。OS の temp 直下に `NeNeCommander-Test-` prefix で作り、setup と Dispose の両方で prefix と親を再検証してから削除する。`DenyDirectoryListing` は ACL deny を記録し Dispose で外す。`CreateFileWithUnrepresentableName` は `\\?\` prefix で末尾ドット名を作る（`Path.GetFullPath` は末尾ドットを落とすので verbatim で結合している）。この harness の外で `System.IO` を使わない。

## 確認済み証跡

- quality（PR #4、`6ae3158`）: [`33745807131`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33745807131)、success。
- quality（main、`de8f197`）: [`33746256879`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33746256879)、成功、2分55秒。
- ローカル deep review（`6ae3158`）: passed。
- tests: 160 / 160 pass。
- protected branch coverage: 100.00 / 100.00 / 95.88 / 96.24%。
- mutation: 96.93 / 96.68 / 97.39 / 99.30%。
- 実行時: スクリーンショットと UI Automation で左ペイン表示を確認（環境: Windows 11 Pro 10.0.26200、3072x1728、ダークテーマ）。

scheduled deep review（UTC 3 日周期）の次回は GitHub 上で確認する。release 時点では 96 時間以内の成功が必要である。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「左ペインの focus を keyboard で動かし、ディレクトリへ入って親へ戻る」だけを実装する。

受け入れ境界:

1. `DirectoryListing` から `PaneState` を生成する経路を一つ定め、`PaneReducer` の既存 transition を左ペインに接続する。
2. `OpenFocused` はディレクトリ entry のときだけ新しい `DirectoryReadRequest` を発行し、`NavigateParent` は Domain に親 path の導出を一つ追加してから使う（`FileSystemPath.cs` は分割してから拡張する）。
3. 最初の ViewModel を Presentation.WinUI に置き、CommunityToolkit.Mvvm を ADR-0003 に従って同時に導入する（`Directory.Packages.props`、`eng/architecture.json`、lock file を同じ変更で更新）。
4. 読み込み中・失敗・cancellation の pane 状態を typed にし、ADV-016（操作中の pane 切替）を実動作へ広げる。
5. 実行時の証跡（起動・キー操作・スクリーンショット）を報告に残す。ゲートはアプリを起動しない。

この縦切りへ WSL / UNC の read adapter、file mutation、sort / hidden の切替、final styling を混ぜない。

## その次に待つ仕事

- adapter の同期列挙を UI thread から外す scheduling の ADR。
- WSL の read adapter（同じ port の別 provider）と `IWslDistributionCatalog`。
- `IFileOperationPort` の Windows local production adapter（`TestOwnedTemporaryRoot` を再利用）。
- テーマ対応 design token（design handoff）。現状ダークテーマで TextBox が見えない。
- Issue #2 の CodeQL 是正。
- 旧 typo path `C:\Users\info\WORKS\NeNeComander`（空）の削除。

## 禁止事項

- `System.IO` や shell API を Application / Presentation / App の feature code から直接呼ばない。Infrastructure.Windows.Tests でも `TestOwnedTemporaryRoot` の外で使わない。
- second gateway、second read port、parallel parser、UI code-behind decision を作らない。
- `CommunityToolkit.Mvvm` を ViewModel の最初の利用と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- final visual design を semantic token の外へ hard-code しない。
- WSL home や利用者の実データを test root として使わない。
- App / XAML を変えたら必ず exe を起動して確認する。ビルドとテストの緑は起動の証拠にならない。
