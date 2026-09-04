# 方向 C layout 統合 引き継ぎ書 — 2026-09-05

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `c3791ae07de9d77138c10e5d595922525c4e6665`
- Completed scope: [Issue #46](https://github.com/hideyukiMORI/NeNeCommander/issues/46) / [PR #47](https://github.com/hideyukiMORI/NeNeCommander/pull/47)（squash merge した（main `c3791ae`））
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-05-color-scheme-handoff.md`](2026-09-05-color-scheme-handoff.md)
- Design: [`docs/design/2026-09-04-design-brief.md`](../design/2026-09-04-design-brief.md)、canvas <https://claude.ai/code/artifact/e2b0baae-b69f-4520-9e8f-886ae8ce8919>（page 3 = 方向 C、実装済み）

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 体制

実装（Issue → branch → 実装 → gate → deep review → PR → squash merge）は Opus 5 のバックグラウンド agent に委任する。本セッション（リナ）は受け入れ境界の切り出し、agent への仕様、証跡の突き合わせ、diff / ADR review、実機 screenshot と canvas の照合、日報 / 引き継ぎ書 / `PROJECT_STATE.md` の docs PR を担当する。agent が長い工程の途中で止まったら「完了まで続けよ」と再開させる。

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent` / `Child`。
- Application: `DualPaneSession` が唯一の coordinator（move / copy / delete / create directory / rename、`OperationAwaitingConfirmation`、`OperationAwaitingName(kind, subject, initialName)`）。`FileOperationGateway` は 4 経路。`Settings/`: `ColorScheme`（8 member）、`HiddenItemVisibility`、`UserSettings`、`ISettingsStore` → `SettingsReadOutcome`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`、`WindowsLocalSettingsStore`（読み取り専用）、`WindowsLocalSettingsLocation`、`SettingsDocumentValidator`（schema v1）。
- Presentation: `KeyboardIntentMapper`（table 駆動、`Map` と `BindingsFor(context)` が同じ `KeyBinding` 表を使う。`gg` chord のみ表の前で解決）、`KeyHintPresenter`、`PaneListingPresenter.Present(snapshot, frame)`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`、`OperationDetail`、`NameEntryPresentation`、`InputContext`、`OperationBarTone`、`KeyHints`）、`PaneRow` / `PaneRowMark`（4 member）/ `PaneRowKind`、`ProgressSegment`。
- App: `Themes/DesignTokens.xaml`（色以外の token、方向 C の値）、`Themes/Schemes/<identifier>.xaml` 8 file、`ColorSchemeResources`、lookup 専用の converter 2 件。`CommanderApplication` が settings を読み scheme dictionary を merge してから window を作る。window は presentation を control に代入するだけ。
- Gate: ARC-012 の scheme dictionary parity scan と Presentation の resource key scan、CS-010 の environment scan は `WindowsLocalSettingsLocation.cs` のみ除外。

## 動作している画面

左 `C:\`、右 `C:\Users`。ペイン番号 badge（active は accent 塗り）+ monospace の path + 右寄せの pane status、28 dip の行に marker・種類 icon・`DIR` ラベル、下部は全幅 34 dip の operation bar（状態で tone が変わり、右端に `F2 名前 / F5 コピー / F6 移動 / F7 作成 / F8 削除 / Tab ペイン / Esc 中止` の key hint）。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F2` / `F5` / `F6` / `F7` / `F8` / `Escape` / 進捗。scheme は `%LOCALAPPDATA%\NeNeCommander\settings.json` の `colorScheme` で選ぶ（再起動が必要）。

## 確認済み証跡

- quality（PR #47、`ec7bf24`）: [`33900096342`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33900096342) 成功、3分29秒。
- quality（main、`c3791ae`）: [`33900434956`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33900434956) 成功。
- ローカル deep review: passed。
- tests: 351 / 351 pass。
- protected branch coverage: 100.00 / 100.00 / 95.43 / 98.05%。
- mutation: 96.15 / 98.19 / 93.06 / 100.00%。
- 実機: `nene-dark` と `solarized-light` を起動して canvas と照合済み（キー送信なし）。

## 次の一つの縦切り: hidden 項目の表示切り替え

設定 `showHiddenItems` は読めているのに誰も消費していない。これを pane の表示に効かせ、hidden 行を `TextHiddenBrush` で描く。

受け入れ境界:

1. **Application**: `DirectoryEntry` に closed な `EntryVisibility`（`Normal` / `Hidden`）を足す。判定は provider が報告する属性で、Domain の path 規則ではない（`.` 始まりを hidden と決めない）。`IDirectoryReadPort` の契約と `DirectoryListing` の順序規則は変えず、listing は hidden も従来どおり含める（FS-011）。
2. **Application**: `PaneState` に `HiddenItemVisibility` を持たせ、`PaneReducer` が visible 集合を決める唯一の場所にする（focus と selection の維持規則: 非表示になった focus item は隣の可視項目へ、選択集合からは除く）。`PaneSession` の生成に `UserSettings` 由来の初期値を渡す（`DualPaneSession` の ctor か `PaneSession` の ctor か、composition root からどう届けるかを ADR で決める。observer と同様に ctor 注入を避ける方針と整合させること）。
3. **Presentation**: `PaneListingPresenter` は可視行だけを射影し、`PaneRowKind` に hidden の描画（`TextHiddenBrush`）を足す。行数や `PaneStatus` の意味（bounded / omitted）を壊さない。
4. **キー**: 表示切り替えの keystroke は `docs/KEYBOARD_MODEL.md` に無い。この縦切りでは **設定値の反映のみ**を行い、切り替え key を足すなら KEYBOARD_MODEL の改訂と KBD-005（hint 生成）まで含めて一つの ADR にする。hint は table から自動で出るので、binding を足せば bar にも出る。
5. **証明**: reducer の可視集合と focus / selection 維持、presenter の射影、adapter が hidden を報告すること（`TestOwnedTemporaryRoot` で hidden 属性を付けた fixture）、`showHiddenItems` の true / false が画面に効くこと。実機は exe 起動 + screenshot + UIA。
6. **混ぜないもの**: collision resolver、byte 進捗、settings の書き込み、WSL、新しい token family。

## その次に待つ仕事

- collision の resolver（FS-007: Replace / Skip / KeepBoth / Cancel）と copy / move の衝突時の再実行。
- settings の書き込み経路（別 ADR）と、壊れた settings の user 向け表示（localized resource が必要）。
- byte 単位の進捗と進捗表示の精緻化（完了後に `12/12` を残すか、進捗数値を accent にするかを含む）。
- 同一 volume の atomic move（FS-005）、Win32 file ID による identity、verify の hash 照合、shell recycle。
- drive 発見、初期 location の永続化、visible-row capacity の実測。
- WSL の read / operation adapter と `IWslDistributionCatalog`。
- high contrast / DPI / 狭い window の環境証明、Issue #2、旧 typo path。

## 禁止事項

- `System.IO`、shell API、`Environment` を Application / Presentation / App の feature code から直接呼ばない（environment は `WindowsLocalSettingsLocation` のみ）。
- second gateway、second read port、second pane coordinator、second adapter for one provider、second awaiting-name state、second settings store、second theme mechanism、second key map を作らない（key hint は `KeyBinding` 表から生成する。XAML に手書きしない）。
- 色・余白・字送りの値を view に書かない。色 key を足したら 8 つの scheme dictionary 全部に足す（parity scan が落ちる）。token family を足さない。
- Presentation の文字列 literal で `...Color` / `...Brush` を名乗るものは scheme に存在させる（gate が見る）。
- `Geometry` を resource dictionary から `Path.Data` に代入しない（WinUI が実行時に落ちる）。`x:Bind` の converter を `Window` root の `DataTemplate` で使わない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。static field initializer に閉じた値を置くと Stryker の帰属で mutant が生き残るので、閉じた型の値は expression-bodied property にする。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
- 閾値 100% の層に到達不能な分岐を書かない。`bool` を parameter にしない（CS-002）。テストで wall clock や `Environment` を使わない。file は `TestOwnedTemporaryRoot`。
- editor tool や sed で file を触ったら CRLF に戻してから commit する。
