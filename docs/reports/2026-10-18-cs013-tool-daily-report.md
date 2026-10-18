# CS-013計測定義と単独ツール — 2026-10-18

[Issue #324](https://github.com/hideyukiMORI/NeNeCommander/issues/324)で、hideが指定した
次の1件、計数定義のADRと5項目を測れる単独ツールを実装した。
[ADR-0075](../adr/0075-standalone-cs013-measurement.md)が`cs013-v1`の正本となり、
唯一の実行入口は`eng/measure-complexity.ps1`。SDK同梱Roslynで工具だけをコンパイルする。
製品コード、依存関係、閾値、既存gate/CIの起動条件は変更していない。

40行・認知複雑度10・深さ3・4引数・型300行の計数対象を明確にした。
partialをまとめ、入れ子・匿名関数・インデクサも測る。構文エラー、条件コンパイル、
primary constructor、top-level、入力欠落、未登録ソース、リンクは黙って除外せず拒否する。
固定native ABIはmetadata属性と実装対応から狭く識別し、引数の生値と理由を残す。
managed mirrorの引数超過と、nativeの他指標は通常どおり指摘する。

[最終集計](../quality/cs013-measurement-2026-10-18.md)は722ファイル、1,162型、
7,804 callable。既存指摘は製品77件、テスト103件、工具0件の計180件。
行数59、認知複雑度26、深さ16、引数44、型35件で、Inspectは正しくexit 1を返す。
固定native署名13件の引数数も保存した。未解決の呼出7,222件は明示的に記録し、
ソースだけの静的な再帰モデルを完全な実行時call graphやビルド成功とは扱わない。

## 実行した検証

実行時PATHは`D:/NeNeCommander/tools/dotnet-10.0.401`を先頭にし、
TEMP/TMPは`D:/NeNeCommander/outputs/issue-324/tmp`を使用した。

```powershell
dotnet format whitespace ./eng/complexity --folder --verbosity minimal
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode SelfTest
dotnet format whitespace ./eng/complexity --folder --verify-no-changes --verbosity minimal
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
pwsh -NoProfile -File D:/NeNeCommander/evidence/cs013-tool-20261018/Prove-Launcher.ps1
git diff --check
```

工具コンパイルは警告・エラー0、SelfTestは24グループ成功、書式検証はexit 0。
Inspectは既存指摘を180件保持してexit 1。入口の7ケースはすべて成功した。
構文/入力/コンパイル失敗はexit 2、古い成功JSONやバイナリの再利用はない。
実junctionの拒否とリンクだけの削除、リンク先保存も確認した。
保管したlauncher proofは、整理後も再実行できるようsource rootを引数化した。
実行時の固定作業rootと同じ場所を`-RepositoryRoot`で指定できる。証明ロジックは同じ。
通常commit hookで`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`を実行する。

開発中の初回集計で工具自身の5指摘を見つけ、責務別に分けて解消した。
通常インデクサと未登録ディレクトリ内binの取りこぼし、partial再帰の宣言順、
推移的project参照、失敗時の結果無効化を修正し、対応するfixtureを追加した。
formatterの初回`--folder --no-restore`併用は未対応でexit 1となり、
folder modeだけに修正した。失敗した試行を成功根拠には使用していない。

ADR-0061に従い、製品build/tests、全体/deep CI、coverage、all-layer mutation、
live provider/UI、release #94は実行していない。工具の検証は全体CS-013合格を意味しない。
次は既存計画の責務別分割であり、全体gateの接続はその解消後に別変更として行う。

## 統合と整理

基準mainは`85a6a9956ae37ae1fb0437542d0eaf6b1e556e74`。
作業branchは`build/324-cs013-metrics`、追加worktreeは
`D:/NeNeCommander/worktrees/issue-324`、一時出力は
`D:/NeNeCommander/outputs/issue-324`。
恒久証拠は`D:/NeNeCommander/evidence/cs013-tool-20261018/`に収載し、
受理・squash・main同期後に未保存物/唯一の成果物/稼働参照/リンクを確認して作業物を整理する。
Issue PRと`completion.json`が最終チェック、統合、同期、整理の結果を記録する。
先行#322はPR #323で統合済みで、追加作業物や後片付け待ちはない。
