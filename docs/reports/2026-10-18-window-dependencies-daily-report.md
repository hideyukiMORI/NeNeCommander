# CommanderWindow dependencies — 2026-10-18

[Issue #318](https://github.com/hideyukiMORI/NeNeCommander/issues/318)で、
`CommanderWindow`の6引数をCS-013の上限内へ整理した。
唯一の`CommanderApplication.CreateWindow`が既存のmapper、session、defect callback、
local time zoneを`CommanderWindowDependencies.Create`へ渡す。
window constructorはdependenciesと左右の初期パスの3引数、依存型のfactory/constructorは4引数。
依存型はApp.Viewsのimmutable sealed recordで、private constructorと唯一のCreateを持つ。

状態所有者、非同期所有権、初期位置、タイムゾーンの一回読取り、描画とイベント経路は維持した。
旧6引数overloadは削除し、公開constructorの呼出契約は非互換となる。
新しいDI機構、サービスロケータ、依存パッケージ、runtime dispatchは追加していない。
ARC-006、CS-007/008/009/013、GIT-001〜004、ADR-0061に従う。

## 検証

SDK 10.0.401、Release。作業場所は`D:/NeNeCommander/worktrees/issue-318`、
恒久証拠は`D:/NeNeCommander/evidence/issue-318/`。TEMP/TMPはD側のtask outputs。

```powershell
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~CommanderWindowCompositionTests'
dotnet format whitespace NeNeCommander.slnx --no-restore --include src/NeNeCommander.App/Views/CommanderWindowDependencies.cs src/NeNeCommander.App/Views/CommanderWindow.xaml.cs src/NeNeCommander.App/CommanderApplication.xaml.cs tests/NeNeCommander.Architecture.Tests/CommanderWindowCompositionTests.cs
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~CommanderWindowCompositionTests|FullyQualifiedName~AsyncLifecycleOwnershipTests'
```

旧実装で最初の対象テスト1件が意図した引数上限違反で失敗した（exit 2）。
修正後は対象buildが警告0・エラー0、引数上限2件と既存非同期所有権1件の計3件が成功、失敗/skip 0。
formatはexit 0、workspace読込み警告が1件あるため「全工程無警告」とはしない。
全変更C#の整形差分を確認し、唯一の呼出元と各依存の配線も差分で照合した。
通常commitは`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`をhookで実行する。

ADR-0061の限定的なApp composition refactorとして、対象buildと構造/所有権の証拠を選んだ。
全体CS-013 gateの実装ではない。既存入力の成功証拠はpush/mergeだけでは再実行しない。
canonical/deep CI、全件回帰、coverage/mutation、live UI/provider、release #94は実行していない。
実ウィンドウを起動した動作証明は今回の結果に含めない。

## 統合・整理

Issue PRと恒久証拠の`completion.json`に最終Commitチェック、squash merge、
main同期とclean/tree一致、成果物の保存、worktree/output整理の実行結果を記録する。
PR #319は19:52:27 JSTに`839936e1a1e7e471b2c80df28087f1d223f4bf91`でsquash統合した。
mainの同期・clean・task tree一致を確認し、未保存/未追跡データ、無視されたbuild出力、
リンクと稼働参照を点検した。恒久証拠を残して`Complete-Cleanup.ps1`がexit 0で終了し、
`D:/NeNeCommander/worktrees/issue-318`と`D:/NeNeCommander/outputs/issue-318`は削除済み。
worktree登録解除も確認した。ブランチ・commit・共有SDKは保持し、後片付けの実行待ちはない。
後続は用語集のprovider visibility記述修正、CS-013棚卸しと分割・gate化の準備をこの順で行う。
