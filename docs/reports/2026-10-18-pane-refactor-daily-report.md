# ペイン表示の責務整理 — 2026-10-18

[Issue #328](https://github.com/hideyukiMORI/NeNeCommander/issues/328)は、hideが指定した
3件のうち②を実装する。基準は①を統合した`63c1a561f34edc5f0e3af6eb7e0f243848f7c0c5`。
次は③App画面クラスを別Issue/PRで扱う。

唯一の投影経路`DualPanePresenter` / `PaneListingPresenter`を維持し、表示モデルを
source（snapshot/frame）、header（status/address/sort/location）、pane pair、
operation bar、operation interactionへ整理した。内部constructorは
`PanePresentation`が6→4、`DualPanePresentation`が12→3引数となり、旧形は削除した。
左右frameは各paneのsourceから取得し、同じ事実を二重に渡さない。
公開binding名と型、行collectionとfocusのidentity、差分更新、競合modalの継続参照、
Applicationの状態所有と単一実行経路は維持した。App/XAML・provider・資源は不変。
操作種別の3対応表は平坦なswitchに変え、深さ4→1とした。

旧2クラスの59 test methodsは属性とDataRowを含めて一度ずつ移行した。
移行対応表は`test-migration.json`。巨大なnull検証は新しい必須部品の境界へ移し、
status/addressはprivate header factoryで決定するため、任意のnullを渡す旧経路自体がない。
行・count・keyboard・modalの旧拒否検証は維持した。6メソッドを追加し、部品の欠落拒否、
元のrows/focus/snapshot/frameとmodal参照、record copy/equalityを確認した。
削除確認と競合再発の大きいテストは、準備・観測結果の検証を責務ごとに分けた。

## 検証

SDK 10.0.401、Release。作業treeは`D:/NeNeCommander/worktrees/issue-328`、
TEMP/TMPは`D:/NeNeCommander/outputs/issue-328/tmp`。

```powershell
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~DualPane|FullyQualifiedName~PaneListing|FullyQualifiedName~PaneActivityPresentation|FullyQualifiedName~PaneResource|FullyQualifiedName~PanePresentationTests|FullyQualifiedName~PaneRowAutomation|FullyQualifiedName~DeletionConfirmationAutomation'
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore --disable-build-servers
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
git diff --check
```

Presentation.TestsとAppの最終buildは警告0・エラー0。選択した74ケースは全成功、
失敗/skip 0。変更C#を`dotnet format whitespace NeNeCommander.slnx --no-restore
--include <changed C# paths> --verify-no-changes --verbosity minimal`で確認しexit 0。
formatterの未復元projectを含むworkspace読込み警告は残る。初回buildのIDE0046/IDE0005
各1件は修正後に成功した。初回計測の競合再発test 43行も整理した。失敗試行も証拠に残す。

最終計測は748 C# files、1,188 types、7,873 callables、既存指摘159件で期待どおりexit 1。
製品66・tests93・eng0、method/cognitive/nesting/parameters/typeは54/24/11/38/32。
未解決invocationは7,269。今回の変更/追加C#の指摘は0で、計14件を解消した。
工具は変更していないため#324の24 fixture groupsと7 launcher casesの証拠を再利用する。
通常hookの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`を使用する。

ADR-0061に基づき純粋な表示投影と直接利用境界を選択した。公開binding名・App/XAMLが不変で、
App buildが接続を確認する。full/deep CI、全件回帰、coverage/mutation、live desktop/provider、
release #94は実行していない。閾値・baseline・gate/CI配線を変更していない。
成功したbuild/test/計測は関連入力が不変のcommit/push/mergeで再実行しない。

## 統合と整理

恒久証拠は`D:/NeNeCommander/evidence/pane-refactor-20261018/`。
Issue PRと`completion.json`が最終Commit検証、squash、clean main同期、
証拠保存後のtask worktree/output整理を記録する。branch/commit・証拠・共有SDKは保持する。
①#326はPR #327で統合・main同期・追加作業物削除まで完了し、後片付け待ちはない。
