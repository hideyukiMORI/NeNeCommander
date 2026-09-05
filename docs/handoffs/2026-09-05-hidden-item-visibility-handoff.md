# hidden 項目の可視集合 引き継ぎ書 — 2026-09-05

Status: informational

## 実装サナ継続 checkpoint

- branch は `main=27495b38`（ADR-0025 / QLT-015）を取り込み、実装 checkpoint `dfe0593` に進んだ。
- 全 hidden を非表示にした後で再表示すると focus が null のままになる回帰を、`Shown -> Hidden -> Shown -> MoveNext` の失敗 test で先に証明して修正した。visibility transition は可視集合が非空なら必ず可視 entry に focus を戻す。
- Release build、Application 173、Infrastructure 66、Presentation 65、Commit mode が成功した。
- deep review は成功した。canonical 373 / 373、coverage 100.00 / 100.00 / 95.48 / 98.06%、mutation 97.12 / 98.32 / 93.20 / 100.00%。
- interactive desktop の screenshot / UIA だけが未完了。hide の作業を妨げない別 desktop object では process / window 作成を false / true の両方で確認できたが、DWM と UIA content が得られず黒画像だったため proof に数えていない。settings は元 byte 列へ復元済み。Draft PR のままこの環境 proof を終えてから Ready にする。
- 次の独立作業では gate fixture copy の改善前 baseline として、負例 1 件あたり約 410 MiB、cleanup 累計 8.1 GiB を観測済み。

## 最新 `main` 統合後 checkpoint

- Draft PR #53 は Issue #54 / #56 / #57 / #58 / #59 を含む `main=dd6439e` を取り込み済み。
- hidden 可視集合と ADR-0028 の増分投影を統合し、`PaneReducer` が決めた `VisibleEntries` だけを安定した `PaneRows` に投影する。focus / selection mark の変更時は影響 row だけを置換し、visibility によって row 集合が変わる時は新しい可視集合を作る。
- Release restore / build は warning 0、error 0。Application 176、Infrastructure.Windows 69、Presentation.WinUI 76、Architecture 5、conformance 110規則、security 18 adversarial cases が成功した。
- pre-integration deep review は履歴証跡であり、final head の canonical CI は未取得。interactive-desktop screenshot / UIA proof が可能になるまで Draft を維持し、proof 後に Ready にして fresh canonical CI を得る。

## 通常 desktop proof 完了 checkpoint

- hide の明示許可により通常 desktop で `showHiddenItems=false` / `true` を起動し、左 `C:\` の UI Automation tree と window screenshot を取得した。キー送信はなく、各caseで所有 process だけを終了した。
- false では hidden/system 項目がなく、true では `$RECYCLE.BIN`、`System Volume Information`、`hiberfil.sys`、`pagefile.sys`、`swapfile.sys` を含む対象が現れた。screenshot で hidden/system 行の muted 表示も確認した。
- settings は `showHiddenItems=false` / `nene-dark` に復元済みで、NeNe Commander process は残っていない。ローカル証跡は `artifacts/implementation-sana/runtime-proof/normal-*`。
- `origin/main=dd6439e` と merge-base は一致する。日報・handoff更新をcommit/push後に PR head一致を再確認し、Readyへ移して final-head canonical CI を監視する。

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `c3791ae07de9d77138c10e5d595922525c4e6665`（`main` の先端は docs commit `f121ed6`）
- **未完了の作業**: [Issue #49](https://github.com/hideyukiMORI/NeNeCommander/issues/49) / branch `feat/49-hidden-item-visibility`（`74d9169`、push 済み、PR 未作成）
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)、dependabot の [PR #27](https://github.com/hideyukiMORI/NeNeCommander/pull/27) が open のまま
- Previous handoff: [`2026-09-05-direction-c-layout-handoff.md`](2026-09-05-direction-c-layout-handoff.md)
- Design: [`docs/design/2026-09-04-design-brief.md`](../design/2026-09-04-design-brief.md)、canvas <https://claude.ai/code/artifact/e2b0baae-b69f-4520-9e8f-886ae8ce8919>

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 体制

実装（Issue → branch → 実装 → gate → deep review → PR → squash merge）は Opus 5 のバックグラウンド agent に委任する。本セッション（リナ）は受け入れ境界の切り出し、agent への仕様、証跡の突き合わせ、diff / ADR review、実機 screenshot と canvas の照合、日報 / 引き継ぎ書 / `PROJECT_STATE.md` の docs PR を担当する。agent が長い工程の途中で止まったら「完了まで続けよ」と再開させる。agent には毎回、規範文書の読み順、触る file、repo の mechanics（CRLF、`dotnet test --solution`、`gh pr checks` 前の待ち、100% coverage 層の到達不能分岐禁止、キー送信禁止）を書いて渡す。

## 現在の実装境界（`main` = merge 済み）

- Domain: path parse、provider-native identity、`FileSystemPath.Parent` / `Child`。
- Application: `DualPaneSession` が唯一の coordinator（move / copy / delete / create directory / rename、`OperationAwaitingConfirmation`、`OperationAwaitingName(kind, subject, initialName)`）。`FileOperationGateway` は 4 経路。`Settings/`: `ColorScheme`（8 member）、`HiddenItemVisibility`、`UserSettings`、`ISettingsStore` → `SettingsReadOutcome`。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`、`WindowsLocalSettingsStore`（読み取り専用）、`WindowsLocalSettingsLocation`、`SettingsDocumentValidator`（schema v1）。
- Presentation: `KeyboardIntentMapper`（table 駆動、`Map` と `BindingsFor(context)` が同じ `KeyBinding` 表を使う）、`KeyHintPresenter`、`PaneListingPresenter.Present(snapshot, frame)`、`DualPanePresenter`（`OperationBarTone`、`KeyHints`、`OperationStatus`、`OperationDetail`、`NameEntryPresentation`、`InputContext`）、`PaneRow` / `PaneRowMark`（4 member）/ `PaneRowKind`、`ProgressSegment`。
- App: `Themes/DesignTokens.xaml`（方向 C の非色 token）、`Themes/Schemes/<identifier>.xaml` 8 file、`ColorSchemeResources`、lookup 専用 converter 2 件。`CommanderApplication` が settings を読み scheme dictionary を merge してから window を作る。
- Gate: ARC-012 の scheme dictionary parity scan と Presentation の resource key scan、CS-010 の environment scan は `WindowsLocalSettingsLocation.cs` のみ除外。

## 動作している画面

左 `C:\`、右 `C:\Users`。ペイン番号 badge + monospace の path + 右寄せ pane status、28 dip の行に marker・種類 icon・`DIR`、下部は全幅 34 dip の operation bar（状態で tone、右端に `F2 名前 / F5 コピー / F6 移動 / F7 作成 / F8 削除 / Tab ペイン / Esc 中止`）。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F2` / `F5` / `F6` / `F7` / `F8` / `Escape` / 進捗。scheme は `%LOCALAPPDATA%\NeNeCommander\settings.json` の `colorScheme` で選ぶ（再起動が必要、現在 `nene-dark` / `showHiddenItems: false`）。

## 確認済み証跡（`main`）

- quality（main、`c3791ae`）: [`33900434956`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33900434956) 成功。
- ローカル deep review: passed。
- tests: 351 / 351 pass。branch coverage: 100.00 / 100.00 / 95.43 / 98.05%。mutation: 96.15 / 98.19 / 93.06 / 100.00%。
- 実機: `nene-dark` と `solarized-light` を起動して canvas と照合済み。

## 次の一つの仕事: `feat/49-hidden-item-visibility` を完成させて merge する

branch は canonical gate を通っている（372 tests、branch coverage 100.00 / 100.00 / 95.48 / 98.06%）。**新しく書き直さず、この branch の続きから**行う。

残っている手順:

1. `git checkout feat/49-hidden-item-visibility` して差分を全文読む（`git diff main...HEAD`）。特に `PaneReducer` の focus 回復規則、`PaneState` の不変条件（entries + visibility から `VisibleEntries` を constructor で導く）、`PaneListingPresenter` の射影、`WindowsLocalDirectoryReader` の属性判定。
2. `docs/adr/0024-hidden-item-visibility.md` を通読して、決定が実装と一致しているか確認する（合意事項: 名前から hidden を判断しない、read port は hidden も返す（FS-011）、可視集合の決定は `PaneReducer` だけ、focus 回復は「対象 → 次の可視 → 前の可視 → focus なし」、不可視になった選択は落とす）。
3. `pwsh -NoProfile -File ./eng/check.ps1` を実行して PASS を確認する。
4. `pwsh -NoProfile -File ./eng/deep-review.ps1` を最後まで走らせ、mutation が 95 / 95 / 90 / 90 を満たすことを確認する（Presentation は static field initializer の帰属で落ちやすい。閉じた型の値は expression-bodied property に置く）。
5. 実機確認: Release を build し、`%LOCALAPPDATA%\NeNeCommander\settings.json` の `showHiddenItems` を `false` で起動 → screenshot と UIA で `$RECYCLE.BIN` / `hiberfil.sys` / `pagefile.sys` / `swapfile.sys` が出ないこと、`true` で再起動 → 出て、かつ hidden 用の色で描かれることを確認し、最後に `false` と `colorScheme: nene-dark` に戻して内容を検証する。キー送信は行わない。
6. PR を作り（`Closes #49`、body の形は `gh pr view 47`）、`gh pr checks --watch` の後に squash merge、`main` を同期して run ID を記録する。
7. 日報と引き継ぎ書、`PROJECT_STATE.md` の checkpoint を docs PR で更新する。

この縦切りに **含めない**もの: 表示切り替えの keystroke（`docs/KEYBOARD_MODEL.md` の改訂と KBD-005 の hint 生成を含む別 ADR が要る）、collision resolver、byte 進捗、settings の書き込み、WSL、新しい token family。

## その次に待つ仕事

- hidden 表示切り替えの keystroke（KEYBOARD_MODEL 改訂 + ADR。binding を表に足せば key hint は自動で出る）。
- collision の resolver（FS-007: Replace / Skip / KeepBoth / Cancel）と copy / move の衝突時の再実行。
- settings の書き込み経路（別 ADR）と、壊れた settings の user 向け表示（localized resource が必要）。
- byte 単位の進捗、完了後に `12/12` を残すか、進捗数値を accent 色にするか。
- 同一 volume の atomic move（FS-005）、Win32 file ID による identity、verify の hash 照合、shell recycle。
- drive 発見、初期 location の永続化、visible-row capacity の実測。
- WSL の read / operation adapter と `IWslDistributionCatalog`。
- high contrast / DPI / 狭い window の環境証明、Issue #2、dependabot PR #27、旧 typo path。
- hide の実機確認待ち: `F2` / `F7` の名前入力、`F8` 確認、実行中の進捗表示。

## 禁止事項

- `System.IO`、shell API、`Environment` を Application / Presentation / App の feature code から直接呼ばない（environment は `WindowsLocalSettingsLocation` のみ）。
- second gateway、second read port、second pane coordinator、second adapter for one provider、second awaiting-name state、second settings store、second theme mechanism、second key map を作らない。key hint は `KeyBinding` 表から生成する（XAML に手書きしない）。
- 可視集合の判断を `PaneReducer` 以外（adapter、presenter、window）に置かない。hidden を名前から判断しない。
- 色・余白・字送りの値を view に書かない。色 key を足したら 8 つの scheme dictionary 全部に足す（parity scan が落ちる）。token family を足さない。Presentation の `...Color` / `...Brush` literal は scheme に存在させる。
- `Geometry` を resource dictionary から `Path.Data` に代入しない。`x:Bind` の converter を `Window` root の `DataTemplate` で使わない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。閾値 100% の層に到達不能な分岐を書かない。`bool` を parameter にしない（CS-002）。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
- テストで wall clock や `Environment` を使わない。file は `TestOwnedTemporaryRoot`。
- editor tool や sed で file を触ったら CRLF に戻してから commit する。
- 未検証の branch を merge しない（deep review と実機確認が済むまで）。
