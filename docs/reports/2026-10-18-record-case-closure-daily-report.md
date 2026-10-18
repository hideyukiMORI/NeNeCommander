# 日報 2026年10月18日 record具象caseの外部派生閉鎖

Status: informational

[Issue #306](https://github.com/hideyukiMORI/NeNeCommander/issues/306)で、#305が実証した
record copy constructor経由の未知case追加を修正した。hideの実装選択を受けて
[ADR-0073](../adr/0073-close-record-cases-with-inaccessible-abstract-members.md)をacceptedへ移した。
baselineは`30529cdf9d99dd1e41b86cac2f03d2706237fabb`、SDK 10.0.401 / C# 14.0。

## 不変条件と唯一の機構

通常のC#を使う非friendの外部assemblyは、公開abstract recordへ追加の具象caseを作れない。
外部から実装できないabstract instance memberとsealed caseを既存のrecord階層に置く。
118 rootに`private protected abstract void RestrictDerivation();`、499の製品caseと
5つの既存friendテストcaseに空overrideを追加した。既にinternal abstract identity memberで
閉じている`FileSystemPath`と、外部非公開の11 rootは変更しない。

公開factory・既知case・payload・record equality/hash/withを維持する。空memberは呼び出さず、
runtime registry、別constructor経路、runtime例外、coverage除外を作らない。
実行文と既存tokenを比較する構造差分監査で、変更製品266ファイルがmember追加と空body表記以外
不変、追加実行文0であることを確認した。これは意味論テストの代用ではなく移行範囲の確認である。

外部abstract派生と明示friendの具象派生は許容される。悪意ある同一process内の実行コード、
reflection/unsafeへのsandboxではない。任意の外部subclassに対しては継承APIの破壊的変更であり、
非互換性をPRとcommit footerに明記する。plannerの未知action fallbackは変更していない。

## 恒久的なコンパイル証明

`eng/verify-record-closure.ps1`が固定SDKのRoslynとreference packを直接使用する。
package・project・SDK pinは追加/変更しない。製品4assemblyのmetadataから対象を発見し、
全119公開rootについて通常constructor・copy constructor・inaccessible overrideによる
外部具象派生を実際にcompileして拒否する。対応する外部abstract派生とroot/公開caseのwithは
compile成功が必要で、単なる別のcompile errorを閉鎖の証拠として受け入れない。

同じcheckerのself-proofは2つの閉じたroot、3種類の閉鎖解除、診断理由違い、friend、
公開/非公開nested case、runtimeのwith/equality/hash/payloadを扱う。
canonical gateからcheckerを外すfixtureもCS-002で拒否する。
`eng/check.ps1`のRelease build後に製品証明、`eng/prove-gates.ps1`にself-proofを組み込んだ。
既存の手動quality workflowもこの同じ経路を使い、push/Readyでfull CIを始めない。
CS-002/004/008、quality/test/layout/glossary、proof registryを更新した。

## 検証範囲と結果

4層の共有型契約とゲートを横断するため、ADR-0061に従いcanonical gateを1回選択した。
実行内容と最終結果は以下に記録する。

- 移行前の同じ製品compiler検査は119公開root中118の外部具象派生成立を検出して失敗した。
  移行後は119すべて閉鎖、失敗0。外部abstract派生と既知withの対照も成功した。
- canonical gateはexit 0。112規約、全negative proof、24 threat登録を含むsecurity、locked restore、
  format、Release build、製品compiler証明、全tests、coverage、tree不変確認を通過した。
- Release buildは警告/エラー0。全テスト1,856件中1,840件成功、失敗0、live WSLの16件は
  専用root未指定として明示skip。成功件数にskipを混ぜない。
- branch coverageはDomain 100.00%、Application 100.00%、Infrastructure 92.32%、
  Presentation 94.25%。各閾値100/100/90/90%を維持して通過した。
- 最終レビューでcheckerが拒否した再compileの横に以前の成功DLLを残す問題を発見した。
  同名の成功→拒否という回帰fixtureは修正前に意図した理由で失敗した。Emit開始時に
  そのchecker所有DLLだけを消す修正後、CS-002 self-proof/切断negativeと製品119 rootを再確認した。
  製品source/tests/dependenciesは不変なので、成功済みのbuild/tests/coverageを再実行していない。
- solution formatはexit 0だがworkspace警告を出した。診断logで、Releaseだけをbuildした状態で
  WinUI Appのdesign-time読込がDebug参照DLLを探すことが原因と確認した。729ファイルの
  format変更は0、実Release buildは成功している。診断確認を製品失敗とは混同しない。

追加の8 runtimeテストは各層の実factoryから作る既知caseについてroot/concrete with、
参照の分離、等価性/hash、read-only payload、nested private caseを確認する。
既存のfactoryとfriend未知variant dispatchテストもcanonical対象に含まれる。

閉鎖markerに実行文・分岐はなく、Strykerの通常式mutationで今回の構造を壊すことはできない。
marker削除・public化・protected化の3つを実compileする構造的negative proofを使用した。
Stryker全層mutation/deep/CodeQL、live desktop/UNC/WSL、release #94は今回未実行。
実行境界やOS effectの変更はなく、追加のtrust-boundary試験は選択していない。
これらを成功済みとは扱わない。coverage閾値、除外、既存失敗の扱いは変更しない。

## 正確なコマンド

作業場所`D:/NeNeCommander/worktrees/issue-306`、証拠は
`D:/NeNeCommander/evidence/issue-306/`。TEMP/TMPもD側へ置いた。

```powershell
$env:PATH = 'D:/NeNeCommander/tools/dotnet-10.0.401;' + $env:PATH
$env:TEMP = 'D:/NeNeCommander/outputs/issue-306/tmp'
$env:TMP = $env:TEMP
dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode
dotnet build src/NeNeCommander.Presentation.WinUI/NeNeCommander.Presentation.WinUI.csproj --configuration Release --no-restore --disable-build-servers
dotnet build src/NeNeCommander.Infrastructure.Windows/NeNeCommander.Infrastructure.Windows.csproj --configuration Release --no-restore --disable-build-servers
pwsh -NoProfile -File eng/verify-record-closure.ps1 -Mode SelfTest
pwsh -NoProfile -File eng/verify-record-closure.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-306/Migrate-RecordCases.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-306/Audit-StructuralDiff.ps1
dotnet format whitespace NeNeCommander.slnx --no-restore
dotnet format whitespace --folder eng/record-closure
dotnet format whitespace --folder eng/record-closure --verify-no-changes
pwsh -NoProfile -File eng/prove-gates.ps1 -Rule CS-002
pwsh -NoProfile -File eng/check.ps1
dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore --verbosity diagnostic
```

製品証明は移行前REDと移行後GREENに同じ入口を使った。2つの製品buildも移行前後に行った。
stale-binaryのREDにもSelfTest、修正後にはCS-002と製品proofの同じ入口を使った。
canonical gate内のrestore/format/build/tests/coverageは`canonical-gate.log`に残す。
通常`git commit`で`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`が動く。

## 保存・統合・後片付け完了

PR #313は2026-10-18 18:59:37 JSTに`936ab0440bf0f65b5d32fa634befd8ca91326180`へ
squash統合した。19:00:04 JSTにmain同期・clean・受理tree一致を確認した。
恒久証拠`D:/NeNeCommander/evidence/issue-306/`にはRED/GREEN、source/diagnostics/result、
migration一覧、構造監査、build/format/full gate/最終対象proofのlog、入力hash、差分とPR記録を保存した。
最終compiler証明・coverage・製品binaryの1,971ファイルとhashも保持している。

削除前preflightのignored 4,383ファイルは証拠保存済みartifactと再生成可能なbin/obj、
一時出力22ファイルはcompilerのanalyzer cacheだった。今回起動したcompiler serverを正常終了し、
稼働参照・リンク・未保存/未追跡/未知ignoredがないことを確認した。
削除コマンドは自動承認レビューによりprocess作成前に`blocked by policy`で拒否されたため、
サナは別方式で再試行せず、Issue #314 / PR #315でhide実行待ちを記録した。

hideが専用scriptを2026-10-18 19:16:59 JSTに実行した。
19:17:32 JSTの読み取り専用確認で`D:/NeNeCommander/worktrees/issue-306`と
`D:/NeNeCommander/outputs/issue-306`の不在、worktree登録の解除を確認した。
branch/commit、恒久証拠1,971ファイルと共有SDKは保持している。
`cleanup-executed-by-hide.json`、`cleanup-confirmation.json`、`completion.json`に完了を記録し、
過去の自動拒否とpreflight記録も保存する。

確認は`Test-Path -LiteralPath`、`git worktree list --porcelain`、branch/commitと証拠一覧の読み取りで行った。
Issue #316はこの日報・引き継ぎ・PROJECT_STATEを完了へ更新する文書のみの変更。
既存checkoutを使い、追加worktree/output、製品build/test/coverage/mutation、CIは実行しない。
text/linkレビュー、`git diff --check`と通常Commitチェックが対象で、文書PRと
`cleanup-complete-closure.json`が最終統合・main同期の結果を持つ。

後片付けは完了しており、hideへの実行待ちはない。
