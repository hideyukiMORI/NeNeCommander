# コマンドパレットの表示責務整理 — 2026-10-18

[Issue #326](https://github.com/hideyukiMORI/NeNeCommander/issues/326)は、hideが指定した
3件のうち①を実装する。②ペイン表示モデル、③App画面クラスは順に別Issue/PRで扱う。
基準は`d9f234b6616bbe9faac1a4fdc0ca66afdd9e897c`。

唯一の投影経路`CommandPalettePresenter`と選択所有者`CommandPaletteViewState`を維持し、
行の内部構成をcaption（文言/shortcut/識別子）、context（両ペインの説明）、
feedback（利用可否の理由/marker/detail/読み上げ文言）へ整理した。
`CommandPaletteRow`の内部constructorは10→4引数となり、旧形は削除した。
公開プロパティ、candidate/scopeのidentity、順序、検索と選択、localized text、
UIA name/ID、canonical key mapを維持した。App/XAML、Application、provider、資源は不変。

`Present`は50→13 logical lines。理由の認知複雑度/深さは22/6→1/1、
キーヒントの対応表は15/5→1/1となった。441行の旧test typeから、
投影・query/selection・key hint・command labelと共通の決定的fixtureを分離した。
旧14 test methodsはすべて一度ずつ移行し、旧row-null検証は新しいrow境界testへ移した。
新たに5メソッドで各入力の欠落、record copy/equality/hash、candidate identityを確認する。
移行対応表は恒久証拠の`test-migration.json`。

## 検証

SDK 10.0.401、Release。作業treeは`D:/NeNeCommander/worktrees/issue-326`、
TEMP/TMPは`D:/NeNeCommander/outputs/issue-326/tmp`。

```powershell
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~CommandPalette|FullyQualifiedName~CommandLabelCatalogTests'
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore --disable-build-servers
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
git diff --check
```

Presentation.TestsとAppの最終buildは警告0・エラー0。選択した23ケースは全成功、
失敗/skip 0。変更C#だけを`dotnet format whitespace NeNeCommander.slnx --no-restore
--include <changed C# paths> --verify-no-changes --verbosity minimal`で確認しexit 0。
formatterには未復元の他projectを含むworkspace読込み警告があり、全工程無警告とはしない。
初回buildのIDE0046二件は平坦なswitchへ修正し、fixture移行後のIDE0005一件も修正した。
いずれも失敗した開発試行として保存し、最終成功の代わりにはしていない。

計測は730 C# files、1,170 types、7,837 callables、既存指摘173件で期待どおりexit 1。
製品71・tests102・eng0、method/cognitive/nesting/parameters/typeは58/24/14/43/34。
変更/追加した全C#の指摘は0。工具の既存self-proofを再利用し、閾値や検査対象を変えていない。
通常hookの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`を使用する。

ADR-0061に基づき、純粋な表示投影とその直接利用者に範囲を限定した。
公開binding名とApp/XAMLが不変で、App buildが接続を証明するため、無関係なarchitecture
testや製品全件回帰は追加しない。full/deep CI、coverage/mutation、live provider/desktop、
release #94は実行していない。既存UIの画面表示を今回実測したとは主張しない。

## 統合と整理

恒久証拠は`D:/NeNeCommander/evidence/palette-refactor-20261018/`。
Issue PRと`completion.json`が最終Commit検証、squash、clean main同期と、
証拠保存後のtask worktree/output整理を記録する。branch/commit・証拠・共有SDKは保持する。
先行#324はPR #325で統合・main同期・追加作業物削除まで完了しており、後片付け待ちはない。
