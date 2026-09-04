# color scheme 設定 引き継ぎ書 — 2026-09-05

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `af7a7fdd188c8e1bc570f2b88adb994561c1765c`
- Completed scope: [Issue #40](https://github.com/hideyukiMORI/NeNeCommander/issues/40)（design pass、PR #41 / #42）、[Issue #43](https://github.com/hideyukiMORI/NeNeCommander/issues/43) / [PR #44](https://github.com/hideyukiMORI/NeNeCommander/pull/44)（squash merge、main `af7a7fd`）
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-rename-handoff.md`](2026-09-04-rename-handoff.md)
- Design: [`docs/design/2026-09-04-design-brief.md`](../design/2026-09-04-design-brief.md)、canvas <https://claude.ai/code/artifact/e2b0baae-b69f-4520-9e8f-886ae8ce8919>（page 3 = 方向 C、承認済み）

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 体制

実装（Issue → branch → 実装 → gate → deep review → PR → squash merge）は Opus 5 のバックグラウンド agent に委任する。本セッション（リナ）は受け入れ境界の切り出し、agent への仕様、証跡の突き合わせ、diff review、日報 / 引き継ぎ書 / `PROJECT_STATE.md` の docs PR を担当する。agent は deep review の待ちで停止することがあるので、通知が来たら「完了まで続けよ」と再開させる。

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent` / `Child`。
- Application: `DualPaneSession` が唯一の coordinator（move / copy / delete / create directory / rename、`OperationAwaitingConfirmation`、`OperationAwaitingName(kind, subject, initialName)`）。`FileOperationGateway` は 4 経路。`Settings/`: `ColorScheme`（8 member）、`UserSettings`、`ISettingsStore` → `SettingsReadOutcome`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`、`WindowsLocalSettingsStore`（読み取り専用）、`WindowsLocalSettingsLocation`（`%LOCALAPPDATA%\NeNeCommander\settings.json`）、`SettingsDocumentValidator`（schema v1: `schemaVersion` / `showHiddenItems` / `colorScheme`）。
- Presentation: `KeyboardIntentMapper`、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`、`OperationDetail`、`NameEntryPresentation`、`InputContext`）。
- App: `Themes/DesignTokens.xaml`（色以外の token）、`Themes/Schemes/<identifier>.xaml` 8 file（18 Color + 18 Brush）、`ColorSchemeResources`（scheme → 定数 URI の exhaustive mapping）。`CommanderApplication` は settings を読み、scheme dictionary を 1 つ merge してから window を作り、`RequestedTheme` を appearance に合わせる。window は従来どおり presentation を control に代入するだけ。
- Gate: ARC-012 の scheme dictionary parity scan、CS-010 の environment scan は `WindowsLocalSettingsLocation.cs` だけ除外。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F2` / `F5` / `F6` / `F7` / `F8` / `Escape` / 進捗表示。既定は nene-dark で、`settings.json` の `colorScheme` を書き換えて再起動すると palette と element theme が変わる。layout は旧来のまま（ペイン見出し 18px、address の header 行、名前だけの一覧行、status 行）。

## 確認済み証跡

- quality（PR #44、`0ac71b4`）: [`33891043332`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33891043332) 成功、3分22秒。
- quality（main、`af7a7fd`）: [`33891406319`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33891406319) 成功、4分54秒。
- ローカル deep review: passed。
- tests: 334 / 334 pass。
- protected branch coverage: 100.00 / 100.00 / 95.43 / 97.84%。
- mutation: 97.12 / 97.96 / 93.06 / 100.00%。
- 実機: nene-dark / dracula / solarized-light の切り替え、不正値で既定起動かつ file 不変を確認（キー送信なし）。

## 次の一つの縦切り: 方向 C の layout 統合

canvas page 3 と `scratchpad/design/gen.py`（`c_shell` / `c_pane` / `c_row` / `c_keys` / `c_numbers` / `c_entry`）が正本。`docs/design/2026-09-04-design-brief.md` の "Page 3" の記述も参照。

受け入れ境界:

1. **Token**（`DesignTokens.xaml`、CS-023）: 窓の padding 6、ペイン間 gap 3、下部 bar との gap 3、ペイン角丸 3、ペイン border 1、header 34、行 28、行 marker 幅 2、kind icon 16、bar 34、名前入力 320 × 26、key cap padding、body 13、monospace 12 / 11、monospace font family（Cascadia Code → Cascadia Mono → Consolas）を Spacing / Typography / Radius / Density / Border family の key として足す。色 key は既存 18 で足りる（`FocusSurface`、`SelectionMark`、`TextHiddenColor`、`TextKeyHint`、`StatusWarningSurface`、`StatusDangerSurface`、`OperationTrack` を使う）。
2. **Pane**: header 行にペイン番号 badge（`1` / `2`、active は accent 塗り）、address text、pane status を右寄せ。「左ペイン」「右ペイン」の見出しと address の header 行は消す（resw の文言は narrator 名などに残すか削除するかを ADR で決める）。一覧行は 28px、左 2px marker（active の focus = accent、selection = `SelectionMarkBrush`、passive の focus = subtle）、kind icon（directory = accent、file = muted、hidden = `TextHiddenBrush`）、名前は省略記号、directory には右に `DIR` の monospace ラベル。行の背景は focus（active）= `FocusSurfaceBrush`、selection = `SelectionSurfaceBrush`。
3. **下部 bar**: 全幅 34px、状態で tone を切り替える閉じた型を Presentation に足す（idle / name entry = `FocusSurface` + accent 枠 / delete confirmation = `StatusWarningSurface` + warning / rejected・partial = `StatusDangerSurface` + danger）。左に icon + status 文言、右に detail（件数、または 12 segment の進捗 + `done/total` の monospace）、その右に key hint。key hint は KBD-005 に従い `KeyboardIntentMapper` の canonical な binding data から生成する（mapper に読み取り専用の binding 一覧を公開し、Presentation が `KeyHint` 列に射影、label は resw）。idle は `F2 F5 F6 F7 F8 Tab Esc`、modal 中は `Enter Esc` だけ。
4. **名前入力**: bar 内の 320 × 26 の TextBox、monospace、accent の 1px 枠、初期 text は選択済み（既存）。
5. **証明**: QLT-011（focus / selection / inactive / busy / cancel / error / name entry / localization expansion / token state）を Presentation テストで。App / XAML 変更後は exe を起動して screenshot と UIA（nene-dark と 1 つの light scheme）。
6. **混ぜないもの**: key map と command semantics、`HiddenItemVisibility` の反映、byte 進捗、collision resolver、settings の書き込み、WSL。

## その次に待つ仕事

- `HiddenItemVisibility` を pane transition（hidden の表示切り替え）で消費する。
- settings の書き込み経路（別 ADR）と、壊れた settings の user 向け表示（localized resource）。
- collision の resolver（FS-007）と copy / move の衝突時の再実行。
- byte 単位の進捗、同一 volume の atomic move（FS-005）、Win32 file ID による identity、verify の hash 照合、shell recycle。
- drive 発見、初期 location の永続化、visible-row capacity の実測。
- WSL の read / operation adapter と `IWslDistributionCatalog`。
- high contrast / DPI / 狭い window の環境証明、Issue #2、旧 typo path。

## 禁止事項

- `System.IO` や shell API、`Environment` を Application / Presentation / App の feature code から直接呼ばない（environment は `WindowsLocalSettingsLocation` のみ）。
- second gateway、second read port、second pane coordinator、second adapter for one provider、second awaiting-name state、second settings store、second theme mechanism を作らない。
- 色の値を view に書かない。色 key は 8 つの scheme dictionary 全部に足す（parity scan が落ちる）。token family を足さない。
- `CommunityToolkit.Mvvm` を binding の最初の必要と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
- 閾値 100% の層に到達不能な分岐を書かない（string switch の compiler 分岐に注意）。`bool` を parameter にしない（CS-002 scan）。
- テストで wall clock や `Environment` を使わない。file は `TestOwnedTemporaryRoot`。
- editor tool や sed で file を触ったら CRLF に戻してから commit する。
- key hint を XAML に手書きしない（KBD-005）。名前の検証を window / presentation に置かない。observer を session の ctor に注入しない。
