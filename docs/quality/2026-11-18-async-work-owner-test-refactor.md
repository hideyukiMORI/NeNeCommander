# AsyncWorkOwnerのテスト整理（#422）

CS-013の型の長さとメソッドの長さの2件を解消した。通常の受付・完了を
`AsyncWorkOwnerTests`、障害・終了を`AsyncWorkOwnerFaultAndShutdownTests`、
割込みを`AsyncWorkOwnerInterruptTests`へ分け、既存の同期contextを内部型へ移した。
製品コード、schema、依存、閾値、除外、toolingは変更していない。

25件の名前と属性を保持。24件の宣言と本文は改行以外同一で、残る1件も
同期contextの設定と復帰を行うhelperを展開すると、同じ処理順序になる。
queue、lock、通知とcallbackの実行も同一。主担当が比較記録と実差分を確認した。
基点は`b5d9af8`、実装headは`0559da1`。

## 検証（ADR-0061）

対象fixtureだけの変更なので、所有するPresentation.Testsのbuild、対象25件、
変更4ファイルの書式、CS-013 Inspectと通常commit検査を選んだ。

| コマンド | 結果 |
| --- | --- |
| `dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release -m:2 -p:RestoreLockedMode=true` | exit 0、警告0・エラー0 |
| `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~AsyncWorkOwner --minimum-expected-tests 25` | exit 0、25成功・失敗0・skip 0 |
| `dotnet format whitespace tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --no-restore --verify-no-changes --include <変更4ファイル>` | exit 0。完全なargvは恒久証拠のverification.json |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | exit 1、67→65件。対象2件消失・追加0。exit 1は他の既存65件による |
| 通常のcommit hook（`eng/check.ps1 -Mode Commit`） | 112規則・24登録case・security・空白・件名検査成功 |

Inspectの基点入力882件は#421の同一基点のSHA256と一致した。
`dotnet test --help`をproject指定なしで試した際、solution評価が始まったため、
そのhelp processと子を停止した。この試行をテスト成功とは数えない。
以後のbuild/testはすべて対象projectを明示した。

証拠は`D:/NeNeCommander/evidence/issue-422/`のverification.json、source-comparison.json、
inspect-comparison.jsonと各raw log。コミット・push・main移動だけでは、
同じ入力の成功済み検証を再実行しない。統合結果と整理はcompletion.jsonへ記録する。

規則: CS-013、TST-001/006、QLT-015、GIT-001〜004、ADR-0061/0075。waiverなし。
full gate、mutation、live WSL、GUI、CIは未実行であり、実機の証明ではない。
