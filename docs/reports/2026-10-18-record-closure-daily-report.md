# 日報 2026年10月18日 closed recordの外部継承調査

Status: informational

[Issue #305](https://github.com/hideyukiMORI/NeNeCommander/issues/305)で、private constructorを持つ
abstract recordに、別アセンブリから未知の具体caseを追加できることを再現した。
製品baselineは`1abea9392ab9421fe47246e761d655c62b49d112`、SDK 10.0.401 / C# 14.0。
公開119型のうち118型で具体的な外部派生がコンパイルでき、`FileSystemPath`だけは拒否された。
製品code/test、依存、SDK、gateは変更していない。調査完了を製品修正済みとは扱わない。

## 不変条件と最小再現

ARC-001/009、CMD-005、CS-002/004/008/009/020、QLT-005/015、ADR-0002/0061を維持する。
既存recordの生成とdispatchが唯一の正規経路であり、別モデルやruntime registryを追加しない。

実製品Application DLLを参照する非friendアセンブリ`ClosureProof`で、次が成立した。

```csharp
public sealed record ExternalWindowAction : WindowAdjustmentAction
{
    public ExternalWindowAction(WindowAdjustmentAction original) : base(original) { }
}
```

`WindowAdjustmentAction.MoveLeft`を渡して未知caseを生成できた。既知caseとは不等で、
`with { }`によるcloneは未知caseの型と等価性を保持した。rootのcopy constructorはprotectedだった。
同じ実製品DLLを使い、引数なしの`base()`はCS7036で拒否、`base(original)`は成功した。
したがってprivateの通常constructorとprivate sealed caseだけでは外部caseを閉じていない。

scale 1、左上(100,100)、800×600、desktop 1920×1080の同じplacementを、
実製品の純粋関数`WindowAdjustmentPlanner.Plan`へ渡した。既知MoveLeftはXを100から68へ変更し、
未知caseは例外や拒否ではなくX=100の`WindowMovePlan`になった。OSへの移動適用は行っていない。
これは特定consumerのfallbackを実行で確認したもので、全consumerの被害を示すものではない。

## 型と利用側の分類

| Assembly | abstract record総数 | 公開root | 外部具体case compile成功 | 拒否 |
| --- | ---: | ---: | ---: | ---: |
| Domain | 3 | 3 | 2 | 1 |
| Application | 97 | 92 | 92 | 0 |
| Infrastructure.Windows | 10 | 4 | 4 | 0 |
| Presentation.WinUI | 20 | 20 | 20 | 0 |
| 合計 | 130 | 119 | 118 | 1 |

実際にbuildした4 DLLの公開rootごとに、非friend名の独立assemblyを固定SDK付属Roslynでコンパイルした。
公開abstract propertyがある場合はthrowするgetterを実装し、copy constructor経路だけを調べた。
全119型のsource、diagnostic、成功binaryと型名一覧を保存した。全118型を実行生成したという結果ではない。
非公開11型は外部から型を参照できないためcompile対象外。Appのsourceにabstract recordはなかった。

`FileSystemPath`は既存のinternal abstract `HasSameIdentity` / `GetIdentityHashCode`を
外部で実装できずCS0534になる。public abstract propertyだけでは防げない。
Infrastructureの公開4型は`DiagnosticSaltCreation`、`PathContainmentFailureKind`、
`PathContainmentOutcome`、`RevalidationOutcome`で、いずれもcompile成功した。

利用側は次の範囲で読み分けた。runtime実行は上記plannerに限り、source確認を新たなtest成功へ加算しない。

- `WindowAdjustmentPlanner`: 未知actionが移動量0の計画に到達する。実行確認済み。
- `BookmarkEditorSession`: action switchのdefaultと未知category filterは明示的なInvalidOperationException。
  #248の状態不変testはfriend assemblyから通常constructorで未知caseを作る。これはdispatch防御の証拠であり、非friendからの継承禁止の証拠ではない。
- `FileOperationGateway.ExecuteAsync`: 未知requestはswitchのdefaultでInvalidOperationException。
  キャンセル済みならその前にCancelled、lease競合ならReentrantとなる。通常の未知request経路にはport呼出がないことをsourceで確認した。

`WindowAdjustmentModelTests.AssertCases`はrootと同じassemblyの型だけを列挙し、copy constructorを除外する。
「通常constructorだけがcase生成可否を決める」というコメントは再現結果に反する。
`eng/conformance.ps1`のCS-002/004の該当走査はboolean modeとenum等のsource検査で、今回の外部compile proofではない。
既存の検証全体が無効という意味ではなく、外部case閉包を証明していない箇所を特定した。

任意のCLR型をユーザー文字列やsettingsから注入できること、remote exploit、ファイル破壊は実証していない。
今回の問題は通常のmanaged codeでの型契約違反であり、reflection/unsafeや悪意あるin-process codeへの隔離保証とは別である。

## 対策比較と提案

| 案 | 固定C# 14での結果 | 判断 |
| --- | --- | --- |
| 通常constructorをprivateにする | 実製品でcopy経由の外部case生成が成功 | 不十分 |
| copy constructorをprivate / private protectedにする | どちらもCS8878 | 使用不可 |
| 外部で実装できないabstract memberを要求 | concrete派生はCS0534、override試行はCS0115+CS0534 | 推奨候補 |
| protected copy constructorで実行時の型一致を確認 | 外部型はcompile成功、未知生成は実行時に拒否、既知copyは保持 | compile時の閉包より弱く、root fieldsの手動copy管理が必要 |
| class/enumへ置換、言語版更新 | 今回は試作しない | 既存規約/APIまたはSDKの変更範囲が大きい |

推奨案の試作では、public sealed caseとprivate nested caseの両方が成立した。
既知caseのroot経由with、等価性、hash一致、payload維持、別payloadとの不等を確認した。
外部abstract派生は許され、明示friend assemblyの具体派生も許されることまで対にして確認した。
closureは「非friend外部コードが追加の具体caseを生成できない」と定義し、abstract宣言まで禁止とは言わない。

[ADR-0073](../adr/0073-close-record-cases-with-inaccessible-abstract-members.md)は`proposed`。
既存の意味のある非公開abstract memberを再利用し、不足するrootにはprivate protected abstract memberと
sealed caseの空overrideを置く案を示す。runtimeの呼出・検証経路は追加しない。
製品118階層の一括適用、friend test移行、compilerによる正負proofのgate組込み、規約更新は後続実装。
全型のcopy/equality互換性、coverage/mutationへの実影響は未検証であり、除外や閾値変更で回避しない。
consumer fallbackを変更するなら型閉包とは別に挙動とテストを明示する。

Microsoft一次資料の[record説明](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)、
[constructor診断](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/constructor-errors)、
[private protectedの範囲](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/private-protected)と
固定compilerの実結果を突き合わせた。最新referenceにあるpreview機能を固定C# 14へ持ち込まない。

## 証拠・実行コマンド

恒久証拠は`D:/NeNeCommander/evidence/record-closure-20261018/`。
`reproduction-01.json`、最終`inventory.json`、`comparison.json`が結果。
`artifacts/`に実行source、csproj、lock、global/NuGet設定、参照製品DLL、実行binary、各compile probeを保持し、
`artifact-manifest.json`に最終497 fileのSHA-256を記録した。初回3層のinventoryは`inventory-v1/`へ分離保存した。
最終4層集計に初回の115/114を加算しない。比較初版からordinary拒否のdiagnosticを固定し、runtime案と具体case経由のwith確認を追加した。

以下のdotnetは`D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe`をPATH先頭へ設定し、
TEMP/TMPを`D:/NeNeCommander/outputs/issue-305/tmp`へ固定して実行した。
製品参照buildの作業場所は`D:/NeNeCommander/worktrees/issue-305`。

```powershell
dotnet restore D:/NeNeCommander/outputs/issue-305/fixture/ClosureProof.csproj -p:Configuration=Release --configfile D:/NeNeCommander/outputs/issue-305/fixture/NuGet.Config
dotnet build D:/NeNeCommander/outputs/issue-305/fixture/ClosureProof.csproj -c Release --no-restore --disable-build-servers
dotnet D:/NeNeCommander/outputs/issue-305/fixture/bin/Release/net10.0-windows10.0.26100.0/ClosureProof.dll
dotnet build src/NeNeCommander.Infrastructure.Windows/NeNeCommander.Infrastructure.Windows.csproj -c Release --disable-build-servers -p:RestoreLockedMode=true
dotnet restore D:/NeNeCommander/outputs/issue-305/inventory/Inventory.csproj --configfile D:/NeNeCommander/outputs/issue-305/inventory/NuGet.Config
dotnet build D:/NeNeCommander/outputs/issue-305/inventory/Inventory.csproj -c Release --no-restore --disable-build-servers
dotnet D:/NeNeCommander/outputs/issue-305/inventory/bin/Release/net10.0-windows10.0.26100.0/Inventory.dll
dotnet build D:/NeNeCommander/outputs/issue-305/comparison/Comparison.csproj -c Release --disable-build-servers
dotnet build D:/NeNeCommander/outputs/issue-305/comparison/Comparison.csproj -c Release --no-restore --disable-build-servers
dotnet D:/NeNeCommander/outputs/issue-305/comparison/bin/Release/net10.0-windows10.0.26100.0/Comparison.dll
pwsh -NoProfile -File D:/NeNeCommander/evidence/record-closure-20261018/Verify-Evidence.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/record-closure-20261018/Check-Documentation.ps1
```

restore/build/runはexit 0。各buildは警告・エラー0。
最終comparisonの11 compile期待結果と3 runtime判定はすべて成立した。
negative compile自体の拒否は期待結果であり、失敗を抑止した成功扱いではない。
497 fileで作業出力と保存先のhash一致を確認し、残る60 fileは再生成可能なobjのみと分類した。
5文書・197 local links・proposed状態・docs-only diffの確認も成功した。
通常`eng/check.ps1 -Mode Commit`はcommit hookで実行し、最終結果をPRへ記録する。
製品code/test/依存/gateが不変のため、全製品suite、full/deep CI、coverage/mutation、live UI/NAS/WSL、#94は実行しない。
共有契約を実際に移行する次のPRでは、4層への影響に応じたcanonical proofを別途選ぶ。

## 終了時の扱い

本Issueは再現・分類・対策提案で完了する。製品修正は未実装で、ADRも未受理。
[後続Issue #306](https://github.com/hideyukiMORI/NeNeCommander/issues/306)で未実装の修正を追跡し、
調査Issueのcloseで欠陥まで解消したとは記録しない。
PR統合後、main同期・tree一致・保存済み成果物・未追跡/無視file・link・稼働参照を確認して、
taskのworktree/outputだけを整理する。branch/commit、恒久証拠、共通SDKは残す。
実際のPR、merge、clean main、整理結果はIssue PRと`completion.json`へ記録する。
