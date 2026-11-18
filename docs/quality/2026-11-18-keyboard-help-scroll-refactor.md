# キーボードヘルプのスクロール分岐整理（#428）

`KeyboardHelpView.Forward` の入れ子の条件式を、同じ順序の分岐へ置き換えた。
所有するview、intentの入口、閉じる処理、focus、表示とscrollの計算は変えていない。
基点は `2fd6745`。製品の変更は同ファイルの1か所だけで、schema、resource、依存、
閾値、除外、gateは不変。

元と後のsourceを機械比較し、4個の比較と値、未一致時の0、先行する半ページの計算、
最後の1回の `ChangeView`、それ以外の全文が同一であることを確認した。
CS-013 Inspect は30件から28件へ減り、対象のcognitiveとnestingの2件が消失、追加0。

## 検証（ADR-0061）

Appの局所的な等価変形なので、App Release build、既存ヘルプのApplication/Presentation
試験、変更ファイルの書式、source比較とInspectを選んだ。
Architectureの既存21件は再実行していない。確認対象はassembly参照、constructor、
fieldとcompositionの所有権であり、該当する宣言・project参照・試験は基点から不変。
本変更の差分が既存メソッド本文の条件式だけであることとApp buildを合わせ、
先行する成功済みArchitecture証拠（#417の品質記録）を再利用する。

| 実行 | 結果 |
| --- | --- |
| Appと対象2 test projectの `dotnet restore <project> -p:Configuration=Release --locked-mode` | 各exit 0 |
| 同3 projectの `dotnet build <project> -c Release --no-restore -m:2` | 各exit 0、警告0・エラー0 |
| Application `dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~KeyboardHelp --minimum-expected-tests 15` | 15成功、失敗0、skip 0 |
| Presentation `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~KeyboardHelpTests --minimum-expected-tests 5` | 5成功、失敗0、skip 0 |
| App directoryで `dotnet format whitespace NeNeCommander.App.csproj --no-restore --verify-no-changes --include Views/KeyboardHelpView.cs` | exit 0、変更0。workspace読込み警告あり |
| 同じformatに `--verbosity diagnostic` を付けた警告調査 | exit 0、変更0。Debug参照assemblyの未生成とWinUI生成source重複を記録。Release buildは成功しており、書式確認の警告をbuild成功に読み替えない |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | exit 1、30→28件。既存28件による非zero、追加0 |

全argvと時刻、TRX、raw log、sourceとInspectの比較は
`D:/NeNeCommander/evidence/issue-428/` に保存。
通常commit hook、PR・統合・整理の最終結果は同所の `completion.json` に記録する。
全体gate、deep review、mutation、live WSL、GUIは今回選択していない。
既存試験は実機スクロール・物理キー操作の証明ではない。

規則: CS-013/014/022、ARC-001/004/012、CMD-009、KBD-005、QLT-003/015、
TST-001/003、GIT-001〜004、ADR-0061/0075/0076。waiverなし。
