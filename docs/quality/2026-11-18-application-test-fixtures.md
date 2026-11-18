# Issue #431 — Applicationテストfixtureの責務分割

対象はPR #402のhead `db8fcc72ee342885185b789bd7ac2b72ed1b0f2e` にある
`CommanderSessionTests`、`BookmarkEditorSessionTests`、`NullGuardTests`。
テストの意味と所有状態を保って責務ごとに分け、対象のCS-013 findingsを21件から0件にする。
PR #402のfeature branchへ先に統合する変更で、実機確認前のDraft条件は変えない。

## 不変条件と変更

- 103個のテスト宣言、12個のDataRow、展開後110ケースのmethod名・引数・属性を維持する。
- 19個のfixtureへ分け、既存のfactory/assertionは3個のinternal型へ移す。
  Commanderのfixtureは設定、bookmarkのslot/navigation/registration/ownership、
  addressのentry/navigation、paletteのcatalog/dispatch/ownershipをそれぞれ持つ。
  BookmarkEditorはsession/draft/category/navigation、null guardはvalue/gateway、
  pane、Commander scope、settings、bookmarkの境界を持つ。
- `CommanderSessionTestComposition` は4個の協力objectをreadonlyで借り、
  元の7個の `CreateSession` overloadと同じ順序でsessionを組み立てる。
  `using FileOperationGateway` は各テストに残り、compositionは破棄を所有しない。
- 長い処理から責務が連続する25箇所を抽出する。非同期処理の開始、完了barrier、
  cancellation、observer、assertionの順序は変えない。
- 製品コード、設定schema、依存、pin、gate、閾値、除外に変更はない。

規約: CS-009/011/012/013/027、TST-001/003/006/012、QLT-015、
GIT-001〜004、ADR-0061/0075。waiver: none。

## 意味の比較

原本3fileを基点commitから保全した。実際に分割したsourceから25個のhelperを読み、
再帰的に呼出し位置へ展開し、owner名の修飾とcompositionの借用を元へ戻して比較した。
空白とcomment以外のtokenは同じで、文字列はそのまま比較した。
formatterが短縮した `Launching.FileLaunchOutcome` と `Bookmarks.BookmarkPath` は
元の完全修飾型名へ戻して比較した。
103宣言の名前・属性・DataRow・全statement順が一致し、35個の既存helper、
7個のfactory overload、4個のreadonly参照とconstructor代入も一致する。
class属性は全fixtureで元と同じ `TestClass` だけで、余分なmemberはない。
TRXのfixture・method表示名・DataRow値も原本から作った110ケースのmultisetと一致し、
欠落・追加・失敗・skipは0だった。

比較器の初回は別々の元fixtureが持つ同名 `Category` helperを区別せず重複とした。
元fixtureの移動先に比較範囲を限定して訂正した。製品やテストの差ではない。
事前の行数確認で新しいguard helperが46行になる点を見つけ、
失敗outcomeのguardを追加抽出した後も比較は一致した。
format適用後は前述の型名短縮を3個のtestと1個のhelperの差として検出した。
その2種類の型名の対応を明示して再比較し、差がないことを確認した。
buildとテストの失敗はなかった。

## 検証

ADR-0061により、対象Application.Tests projectのRelease build、対象19fixtureの110ケース、
変更23fileのformat、CS-013 Inspect差分、通常commit hookを選ぶ。
full gate、全Application suite、mutation、GUI/liveの検証は選ばない。

以下のdotnet commandのcwdは `tests/NeNeCommander.Application.Tests`。
projectは `NeNeCommander.Application.Tests.csproj`、includeは変更23fileのbasename、
filterは対象19fixtureの完全修飾class名それぞれの末尾に `.` を付けてOR結合したもの。
完全なargv・cwd・exitは各 `*-command.json` に記録した。

| 検証 | 実行command | 結果 |
| --- | --- | --- |
| 依存復元 | `dotnet restore <project> -p:Configuration=Release --locked-mode --disable-parallel -m:2` | exit 0 |
| format適用 | `dotnet format <project> --no-restore --verbosity diagnostic --include <23 files>` | exit 0 |
| Release build | `dotnet build <project> --configuration Release --no-restore -m:2` | exit 0、警告0・エラー0 |
| 対象ケース | `dotnet test --project <project> --configuration Release --no-build --no-restore --filter <19 fixtures> --results-directory <evidence>/TestResults --report-trx --report-trx-filename focused.trx` | exit 0、110成功・失敗0・skip 0 |
| 書式検証 | `dotnet format whitespace <project> --no-restore --verify-no-changes --verbosity diagnostic --include <23 files>` | exit 0、変更0 |
| CS-013 | repo rootで `pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect` | before/afterともexit 1、全69→48、対象21件消失・追加0 |

beforeは編集23fileをhash付きで保全し、原本3fileとcleanな基点checkoutに戻して測った。
測定後は23fileを復元し、全hashが一致した。以前のIssue #398のInspect候補は
対象3fileこそ一致するが、全866 source中17fileが基点と違うためbeforeとして使わない。
before/afterのsource hash差は対象23fileだけだった。
分割後23型の最大は213行、memberの最大は40行・4引数・cognitive 3・nesting 2。
残る48 findingsは対象外の既存分で、追加や抑制はない。

原本・比較器・全試行の記録・raw log・TRX・Inspect JSONは
`D:/NeNeCommander/evidence/issue-431/` に保持する。
source比較は `source-comparison.json`、実行ケースは `test-case-comparison.json`、
CS-013差分は `inspect-comparison.json`。
通常commit hookの実行結果は同領域の `commit.log` と完了記録に残す。

## 限界

この証拠はfixture分割の同値性と対象ケースの実行を示す。
PR #402の実機確認、全製品、releaseを証明するものではない。
PROJECT_STATE・日報・引継ぎは統合担当が更新する。
