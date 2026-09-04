# F2 rename 引き継ぎ書 — 2026-09-04

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `efa2f326c19ed7c32d81c77102e995a4dd976312`
- Completed scope: [Issue #37](https://github.com/hideyukiMORI/NeNeCommander/issues/37) / [PR #38](https://github.com/hideyukiMORI/NeNeCommander/pull/38)（squash merge した（main `efa2f32`））
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)
- Previous handoff: [`2026-09-04-create-directory-handoff.md`](2026-09-04-create-directory-handoff.md)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 体制

hide の指示で、実装（Issue → branch → 実装 → gate → deep review → PR → squash merge）は Opus 5 のバックグラウンド agent に委任する。本セッション（リナ）は受け入れ境界の切り出し、agent への仕様、証跡の突き合わせ、diff review、日報 / 引き継ぎ書 / `PROJECT_STATE.md` の docs PR を担当する。agent への仕様には、規範文書の読み順、触る file、repo の mechanics（CRLF、`dotnet test --solution`、`gh pr checks` 前の待ち、100% coverage 層の到達不能分岐禁止、キー送信禁止）を毎回書く。

## 現在の実装境界

- Domain: path parse、provider-native identity、`FileSystemPath.Parent`、`FileSystemPath.Child(name)`。
- Application: `DualPaneSession(left, right, gateway)` が唯一の coordinator。`HandleAsync(intent, observer, cancellationToken)` は move / copy / delete / create directory / rename を `StartAsync` で始め、operation ごとの linked token を所有し、進捗ごとに observer へ snapshot を渡す。modal state は `OperationAwaitingConfirmation`（`Confirm` / `Escape`）と `OperationAwaitingName(kind, subject, initialName)`（`NameSubmission` / `Escape`、`F7` と `F2` で共有）。`FileOperationGateway.ExecuteAsync(request, progress, cancellationToken)` は transfer（move / copy）、delete、create directory、rename の 4 経路。`PaneContentListed.FindFocusedEntry` が focus entry 参照の唯一の経路。
- Infrastructure.Windows: `WindowsLocalDirectoryReader`、`WindowsLocalFileOperationAdapter`（inspect / preflight / copy / verify / delete / create directory / rename）。
- Presentation: `KeyboardIntentMapper`（FileList / TextEntry / Modal）、`PaneListingPresenter`、`DualPanePresenter`（`PaneFrame`、`OperationStatus`、`OperationDetail`、`NameEntryPresentation`（`Hidden` / `ActiveNameEntry(initialText)`）、`InputContext`）。
- App: window は observer として再描画し、intent を転送し（editor 表示中の `Confirm` は `SubmitName(text)`）、presentation を control に代入するだけ。status 行は文言 + 数値 + 区切り + 総数 + 名前入力 TextBox（表示時に初期 text を代入して `SelectAll()`）。

## 動作している画面

左 `C:\`、右 `C:\Users`。`Tab` / `j` / `k` / `l` / `h` / `Space` / `F5` / `F6` / `F8`（`Enter` / `Escape`）/ 実行中の `Escape` / 進捗表示 / `F7` に加え、`F2` で status 行の名前入力に現在の名前が選択済みで出て、`Enter` で rename し新しい path に focus、`Escape` で取りやめる（Domain / Application / Presentation / Infrastructure テストで証明、実機のキーは未送信で hide の確認待ち）。

## 確認済み証跡

- quality（PR #38、`320c99e`）: [`33880714918`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33880714918) 成功、3分40秒。
- quality（main、`efa2f32`）: [`33881080541`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33881080541) 成功、3分18秒。
- ローカル deep review（working tree、commit 前）: passed。
- tests: 316 / 316 pass。
- protected branch coverage: 100.00 / 100.00 / 96.06 / 97.84%。
- mutation: 95.67 / 97.85 / 96.05 / 100.00%。

## 次の一つの縦切り: design pass（`/design` による GUI 整備）

file command の基本セットが揃ったここを「大きい区切り」とし、hide の指示で次は機能追加ではなく GUI を整える。`docs/DESIGN_HANDOFF.md` が正本。hide が `/design`（Claude Design の design canvas）を使う前提で、engineering 側は次の順で準備と統合を行う。

受け入れ境界:

1. **Design brief（engineering が用意するもの）**: focused Issue を切り、`docs/design/` に design brief を置く。内容は `DESIGN_HANDOFF.md` の "What engineering prepares" に対応させる。既にあるもの（dual-pane layout と AutomationId、semantic token 辞書 `Themes/DesignTokens.xaml` の 13 family、en-US / ja-JP resw）を列挙し、足りないもの（interaction state の一覧: inactive / active / focused / selected / busy / disabled / warning / error / conflict / destructive confirmation / name entry、long name・hidden・permission・progress・partial failure の fixture、light / dark / high-contrast / 100–300% DPI / narrow window の制約）を箇条書きで埋める。実装は増やさない。
2. **Design canvas（hide 主導）**: `/design` で artboard を作る。idle、active / passive pane と focus / selection、`F8` 確認 modal、`F7` / `F2` 名前入力、実行中の進捗（数値と将来の bar）、error / partial failure、dark theme。hide がキャンバス上で調整し、承認する。承認された token 値・component inventory・state 注釈を `docs/design/` に記録する（"What the design handoff must return"）。
3. **統合（承認後、別 Issue）**: token 値は `DesignTokens.xaml` の theme dictionary だけで変え、light / dark を分ける（ARC-012、CS-023、Integration law 1–2）。Windows dark theme で address TextBox が見えない既知の問題を解消する。名前入力 TextBox と確認 modal を placeholder から design どおりに置き換える。command semantics、focus order、key map、確認の必須性は変えない（変える場合は別 product ADR）。
4. **証明**: QLT-011 に従い、focus / selection / inactive pane / busy / cancel / error / high contrast / localization expansion / token state を presentation test で証明する。App / XAML を変えたら exe を起動して screenshot と UIA で確認する。
5. **混ぜないもの**: collision resolver、byte 単位進捗の実装、WSL、新しい token family（family 追加は fixture と token conformance test の更新が必要）。

## その次に待つ仕事

- collision の resolver（FS-007: Replace / Skip / KeepBoth / Cancel）と copy / move の衝突時の再実行。
- byte 単位の進捗と進捗 bar（design pass の結果を受けて）。
- 同一 volume の atomic move（FS-005、capability と ADR）。
- Win32 file ID による identity の強化、verify の hash 照合、shell recycle。
- drive 発見、初期 location の永続化、visible-row capacity の実測。
- WSL の read / operation adapter と `IWslDistributionCatalog`。
- Issue #2、旧 typo path。

## 禁止事項

- `System.IO` や shell API を Application / Presentation / App の feature code から直接呼ばない。
- second gateway、second read port、second pane coordinator、second adapter for one provider、second awaiting-name state を作らない。
- `CommunityToolkit.Mvvm` を binding の最初の必要と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- App / XAML を変えたら必ず exe を起動して確認する。キー送信は hide の作業中に行わない。
- 閾値 100% の層（Domain / Application）に到達不能な分岐を書かない（1 source の request で「inspection が cancelled」を分岐させない、nullable の `?.` / `??` の到達しない側を作らない、parent identity 検査の後ろに containment 検査を重ねない）。
- テストで `DateTime.UtcNow` 等の直接 wall clock を使わない（CS-010）。identity 変化は内容の変更で起こす。
- editor tool や sed で file を触ったら CRLF に戻してから commit する（`.gitattributes` が正本）。
- 名前の検証を window / presentation に置かない（`FileSystemPath.Child` が正本）。observer を session の ctor に注入しない。
- design pass で token 値を view に書かない。`Blue500` のような literal key を feature layer に置かない。
