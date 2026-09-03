# dual pane 引き継ぎ書 — 2026-09-03

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `0cd053e4f8348e692e70ffa5996fc74b820efb38`
- Completed scope: [Issue #9](https://github.com/hideyukiMORI/NeNeCommander/issues/9) / [PR #10](https://github.com/hideyukiMORI/NeNeCommander/pull/10)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-03-pane-navigation-handoff.md`](2026-09-03-pane-navigation-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`。
- Application: `DualPaneSession` が両ペインと active side の唯一の coordinator。`ActivateOtherPane` だけが active を変え、他の intent は active 側の `PaneSession.HandleAsync` へ。各 `PaneSession` は `IDirectoryReadPort` と `PaneReducer` で pane を進める。`FileOperationGateway` は本番 adapter 未接続。
- Presentation: `KeyboardIntentMapper`、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame` で border resource key）。
- App: composition root が `WindowsLocalDirectoryReader` を両 `PaneSession` に渡し、`DualPaneSession` と初期 location（`C:\` / `C:\Users`）で `CommanderWindow` を組む。window は `PreviewKeyDown` / `CharacterReceived` → mapper → `DualPaneSession.HandleAsync` → `DualPanePresenter` → 両ペインの control 代入と frame 適用。

## 動作している画面

左 `C:\`、右 `C:\Users` が一覧表示され、左が active（太い枠）。`Tab` で active が入れ替わり、以降の `j` / `k` / `l` / `h` などは active pane だけを動かす（Application テストで証明、実機のキー送信は未実施）。

## 実行時検証の手順

exe を起動し、UI Automation（`LeftAddress` / `LeftStatus` / `LeftFileList` / `RightAddress` / `RightStatus` / `RightFileList`、SelectionPattern / ValuePattern）で状態を読む。キー送信は hide が実キーボードを使っていない時だけ行い、毎打鍵前にフォアグラウンドを確認する。

## 確認済み証跡

- quality（PR #10、`5dac7fb`）: [`33756209244`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33756209244)、success。
- quality（main、`0cd053e`）: [`33757652361`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33757652361)、成功、2分49秒。
- ローカル deep review（`758de72`）: passed（初回 5dac7fb は Application 95.11 % と余裕がなく、758de72 で是正）。
- tests: 198 / 198 pass。
- protected branch coverage: 100.00 / 100.00 / 95.88 / 96.00%。
- mutation: 96.88 / 97.05 / 97.39 / 100.00%。

## 次の一つの縦切り

新しい focused Issue と branch を作り、「`IFileOperationPort` の Windows local production adapter」だけを実装する。

受け入れ境界:

1. Infrastructure.Windows に `WindowsLocalFileOperationAdapter`（仮名、GLOSSARY に合わせる）を一つ実装し、`InspectAsync`（provider identity は file ID + volume serial、`DeletionCapability` は recycle 能力の問い合わせ結果）、`PreflightMoveAsync`（containment、collision、recursion）、`CopyAsync`、`VerifyCopyAsync`（byte count と宣言 metadata）、`DeleteAsync`（Permanent は `File.Delete` / `Directory.Delete`、Recycle は shell recycle が使えるまで `ProviderUnavailable` で閉じる）を持つ。
2. `TestOwnedTemporaryRoot` で contract test を書く。verify failure で source を消さない（ADV-007）、identity 変化の検出（ADV-004）、link を辿らない（ADV-003）を実列挙で証明する。
3. `FileOperationGateway` への UI 接続、collision UI、WSL / UNC は混ぜない。
4. 正本ゲートと deep review を通し、環境未実施項目を明記する。

## その次に待つ仕事

- `F5` / `F6` / `F8` の gateway 接続（passive pane を destination とする）と confirmation UI。
- drive 発見と初期 location の永続化。
- visible-row capacity の実測。
- adapter の同期列挙を UI thread から外す scheduling の ADR。
- WSL の read adapter と `IWslDistributionCatalog`。
- テーマ対応 design token（design handoff）。
- Issue #2 の CodeQL 是正、旧 typo path の削除。

## 禁止事項

- `System.IO` や shell API を Application / Presentation / App の feature code から直接呼ばない。Infrastructure.Windows.Tests でも `TestOwnedTemporaryRoot` の外で使わない。
- second gateway、second read port、second pane coordinator、UI code-behind decision を作らない。
- `CommunityToolkit.Mvvm` を binding の最初の必要と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- final visual design を semantic token の外へ hard-code しない。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
