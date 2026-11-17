# Applicationのテスト整理（#421）

CS-013の9 findingsを、PR #402の変更ファイルに重ならない範囲で解消した。
BookmarkCatalog、CommanderSessionのLocations/window adjustment、DualPaneConflictSession、
LocationsSession、PaneReducer、PaneSession、SettingsSessionの8 fixtureを整理し、
LocationsModelTestsの参照先も移行した。既存9ファイルと追加9ファイルの計18 C#ファイル。
製品コード・schema・依存・tooling・閾値・除外を変えていない。

大きなfixtureを選択・ナビゲーション・ファイル起動・終了などの責務で分けた。
Locationsのcatalog生成は同じ内部型へ集め、window adjustmentの所有fixtureを独立させた。
長い3メソッドは名前のあるhelperへ抽出した。

158のテスト宣言、44 DataRow、合計186ケースの名前と属性を保持した。
90本文は完全一致し、残る本文も型名の修飾変更と3つのhelper抽出を展開すると、
文字列・assertion・処理順序を保持する。移動/複製helper41件、window fixtureの所有状態、
Settingsの同期context全体も元と照合した。主担当は比較記録と実差分を確認し、必須修正の指摘なし。

## 検証（ADR-0061）

対象projectのRelease buildと変更fixtureの186ケース、18ファイルの書式、Inspect、通常hookを選択した。
full gate・mutation・Infrastructure/live・GUIは選択していない。
コマンドの完全なcwd/argv/exitは`D:/NeNeCommander/evidence/issue-421/`へ保全する。

| 実行 | 結果 |
| --- | --- |
| 対象Application.Testsのlocked restore | 成功 |
| 対象project directoryで`dotnet format whitespace NeNeCommander.Application.Tests.csproj --no-restore --verify-no-changes --verbosity diagnostic --include <18 basenames>` | exit 0 |
| 同directoryで`dotnet build NeNeCommander.Application.Tests.csproj --configuration Release --no-restore -m:2` | exit 0、警告0・エラー0 |
| `dotnet test --project NeNeCommander.Application.Tests.csproj --configuration Release --no-build --no-restore --filter <対象15クラス> --results-directory <専用出力> --report-trx --report-trx-filename focused.trx` | exit 0、186成功・失敗0・skip 0 |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | exit 1、基点b5d9af8の67→58件。対象9件消失・追加0 |
| ソース比較 | 宣言・属性・意味の差0、所有状態と同期context一致 |

Inspectのexit 1は他領域の既存58件による。正確な実行記録はfinal-verification-commands.json、
検出差分はinspect-comparison.json、ケース照合はsource-comparison.jsonとfocused.trxに残す。
通常commit hookの結果・最終head・統合・整理は同じ保存先のcompletion.jsonへ記録する。

初期buildは分割後に不要になったusingのIDE0005（26件、次に8件）で停止した。
初回は診断groupの先頭だけを削除したため残りが再検出された。対象projectのformatterで整理し、
最後のbuildを成功させた。初期formatの絶対include指定では修正がなく、その原因は独立に確定していない。
project directoryとbasenameの指定で最終確認した。失敗ログも保存し、成功扱いにしない。

#417/#422の先行main統合はApplication・製品・toolingの入力を変えていない。
文書追記・push・base移動だけでは、成功済みの対象検証を再実行しない。
規則: CS-013、TST-001/006、QLT-015、GIT-001〜004、ADR-0061/0075。waiverなし。
この変更は実機や全製品の検証ではない。PR #402と重なる大きなfixtureは保留のまま。
