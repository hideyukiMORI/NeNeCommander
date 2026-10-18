# 日報 2026年10月18日 名前の自然順

Status: informational

[Issue #246](https://github.com/hideyukiMORI/NeNeCommander/issues/246)で、hideが選んだ
`file1 → file2 → file10`を既存の名前順に実装した。判断は
[ADR-0074](../adr/0074-natural-name-ordering.md)、正本は[PROJECT_STATE](../PROJECT_STATE.md)。
baselineは`591a4062ff89eb4035ac3bb6ab803a708f22a0c6`、SDK 10.0.401 / C# 14.0。

## 不変条件と変更

表示順の唯一の入口`EntryOrdering.Apply`内のprivate名前比較を拡張した。
半角数字の連続を桁数と数字列で比較し、整数型への変換や数値オブジェクトの割当ては行わない。
同じ数値なら桁数の短い方が先で、`file2 < file02 < file002`、`file2z < file02a`。
数字以外は従来のOrdinalIgnoreCase、自然順で完全同値なら名前全体のOrdinal比較。
全角・その他Unicode数字、符号、小数点は通常の文字として扱う。
Unicode補助文字の大文字小文字比較はframeworkのspan比較に任せる。

名前キーの昇降順と、拡張子・サイズ・更新日時が同値のときの昇順名前比較に適用する。
ディレクトリ優先、他キーの主比較、パスidentity、選択・focusの所有者、設定schema 3は維持する。
Windows local、UNC、WSLに共通の表示規則で、`DirectoryListing`のcanonical snapshotは変更しない。
UI側に別のsort経路、新しい設定、API、依存は追加していない。

## 検証範囲と結果

ADR-0061の通常挙動変更として、Application/Presentationのbuild、名前比較と直接consumer、
変更C#のformat、通常Commitチェックを選択した。純粋比較の範囲を対象テストで証明できるため、
provider I/O、全件回帰、full/deep CI、coverage/mutation、desktop/live providerは実行しない。
実画面の手動release確認#94やExplorerとの完全一致は、この結果には含まれない。

- 旧実装でApplication対象70件中29件が失敗、41件成功。数値順を検出するREDを保存した。
- 実装後のApplication対象87件は全件成功、失敗/skipなし。
- Presentation対象29件は初回28件成功。数字入りfixtureへの変更でfocusを離れる行が変わったため、
  その行を不変としていた1件のテスト期待値を修正した。製品側の修正は不要だった。
  修正した1件だけ再実行し成功。変更のない28件とApplication87件は再利用する。
- 両プロジェクトのRelease build成功、警告/エラー0。変更C#のformat verify成功。
- 通常Commitチェックの結果、PR、統合・main同期・後片付けの最終結果はIssue PRと恒久証拠の
  `completion.json`に記録する。実行前の工程を完了とは扱わない。

新しい比較のテストはゼロ埋め、全ゼロ、400桁超、複数数字列、prefix、記号、case、Unicode数字と
補助文字、同名entryの安定性を扱う。21種類の明示した期待順について全210組を両方向・両入力順で
比較し、全21回転入力も確認する。listing上限1万件の順序とsnapshot不変も確認した。
consumerでは初期focus、上下/半ページ/端移動、同キーtoggle、選択identity、hidden focus回復、
refresh/navigation/history、保存済みsortの復元、表示行と変更のない行の再利用を確認した。

## 正確なコマンド

作業場所は`D:/NeNeCommander/worktrees/issue-246`。SDKをPATH先頭に置き、TEMP/TMPは
`D:/NeNeCommander/outputs/issue-246/tmp`を使用した。

```powershell
$env:PATH = 'D:/NeNeCommander/tools/dotnet-10.0.401;' + $env:PATH
$env:TEMP = 'D:/NeNeCommander/outputs/issue-246/tmp'
$env:TMP = $env:TEMP
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -p:Configuration=Release --locked-mode
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~EntryOrdering|FullyQualifiedName~PaneSortOrderTests'
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~EntryOrdering|FullyQualifiedName~PaneSortOrderTests|FullyQualifiedName~PaneSortPersistenceTests|FullyQualifiedName~PaneSessionTests.HandleAsyncWhenSort'
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~PaneListingPresenterTests'
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~PaneListingPresenterTests.PresentWhenSortOrderChangesRebuildsRowsInProjectedOrder'
dotnet format whitespace tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --include src/NeNeCommander.Application/Directories/EntryOrdering.cs tests/NeNeCommander.Application.Tests/EntryOrderingTests.cs tests/NeNeCommander.Application.Tests/EntryOrderingNaturalNameTests.cs tests/NeNeCommander.Application.Tests/PaneSortOrderTests.cs tests/NeNeCommander.Application.Tests/PaneSessionTests.cs --verify-no-changes --no-restore
dotnet format whitespace tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --include tests/NeNeCommander.Presentation.WinUI.Tests/PaneListingPresenterTests.cs --verify-no-changes --no-restore
dotnet format whitespace src/NeNeCommander.Application/NeNeCommander.Application.csproj --include src/NeNeCommander.Application/Directories/EntryOrdering.cs --verify-no-changes --no-restore
```

REDには実装前のApplication buildを、最終Presentation1件にはテスト修正後のbuildを使用した。
formatは同じinclude指定で`--verify-no-changes`なしの整形後にverifyした。
最初の新規テストbuildは`SortKey`のnamespace競合を検出し、CultureInfoの明示aliasに直した。
Presentation format verifyの一度のproject名誤記も訂正した。これらを成功証拠には数えない。
通常`git commit`で`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`が実行される。
成功した証拠は関連入力が同じ間はpush/Ready/mergeを理由に再実行しない。

## 保存と終了

恒久証拠は`D:/NeNeCommander/evidence/issue-246/`。RED/GREEN/build/format/commitのlog、
Issue・PR本文、差分、検証入力hashと最終統合記録をここに残す。
追加worktreeは`D:/NeNeCommander/worktrees/issue-246`、一時出力は
`D:/NeNeCommander/outputs/issue-246`。統合後のpreflightで未保存物、唯一の成果物、稼働参照と
リンクを確認して整理する。branch/commit、証拠、共有SDKは保持する。
