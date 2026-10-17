# 日報 2026年10月17日 検証済み移動元削除の検証完了

Status: informational

最終更新: 2026-10-17 18:21:49 JST。

hideの再開指示と役割分担に従い、NeNe Commanderサナが設計・受理判断を担当し、
Solが残る検証、Lunaが独立した設定検体修正と整理前の調査を担当した。
[前回日報](2026-10-16-verified-move-daily-report.md)と
[前回引き継ぎ](../handoffs/2026-10-16-verified-move-handoff.md)から再開した。

## 完了した修正と受理範囲

[Issue #257 / PR #258](https://github.com/hideyukiMORI/NeNeCommander/pull/258)を
mainへsquash統合した（`5b6ac92ec4bf7c752f2b7fd2da0f0f87fb097ace`）。
対応済みschema 3を未知形式としていた検体を4へ変更した1行だけで、製品コードと
`UnknownVersion`の期待値は維持した。対象methodの1件、Release build、changed-file format、
Commit checkが成功。buildの警告・error、testの失敗・skipはいずれも0。
初回commit hookはsystem SDKを参照して停止したが、process PATHへ固定SDK 10.0.401を指定して
成功した。最初のhook raw logは上書きされ復元できず、記録済みのcommand・原因を保持し、
後から再実行して当時のrawを作ったことにはしていない。

そのmainを[Issue #243 / PR #249](https://github.com/hideyukiMORI/NeNeCommander/pull/249)へ
通常mergeした検証候補が`d2d3dba57d1b8133d391d229d4b202b210c20dc9`。
この時点での前回の製品/test commit `30e6bc0`からの関連差分は上記検体1行だった。
続くInfrastructure mutationが87.59%で基準90%を満たさず、個別guardの検出力を補強した。
[ADR-0069](../adr/0069-verified-move-source-deletion.md)の契約と製品実装は変えていない。
補強はInfrastructure.Testsの新規5fileだけで、既存fixture・製品code・依存関係・gate設定を変えていない。
`VerifiedTreeBoundaryTests`はidentity、bytes、membership、複数ancestorの照合、hidden/system子の完全列挙、
Ordinal順序とlink拒否を確認する。`VerifiedSourceDeletionBoundaryTests`はfrozen sourceのbinding、
leaf削除直前の再照合、親のobject／membership／A・B不一致と一過性不整合を確認する。
`VerifiedVerificationBoundaryTests`は不正source identity、観測途中の変更、余剰target child、
AccessDeniedの正規化を、`VerifiedAdapterBoundaryTests`はsource ownerの拒否とcopy前失敗のeffectを確認する。
`VerifiedCopyAdmissionBoundaryTests`はcache済みsourceやWSL childのjunction置換、コピー先の親の
不在・junctionを確認する。ACL拒否を空の列挙と扱わないこと、祖先がfileの拒否、共有readerとの
共存も既存ownerで確認した。
最終44件はすべて成功し、失敗・skipは0。最後に2件の失敗種別を`IdentityChanged`へ厳密化したため、
不変42件の成功と最終2件の成功を合わせた件数であり、同じ44件を工程のために再実行した数ではない。
新Adversarialは33件で、各ケースの最終入力について3回成功を確認した。
最初のbuildでIDE0305を検出し、コレクション式へ修正した最終Release buildは警告・error 0。
changed-file formatとCommit-mode（112規則・23登録）も成功した。最終test commitは
`0b9f5ba4d906f3ba63ea747cf5aa2cbc4950076e`で、通常hook付きcommit/pushとPR headの一致を確認した。

不変条件は、コピー先に保存されていないsourceの観測済み変更を拒否し、残存sourceと完了した
部分effectを正しく報告すること。唯一の経路は既存gateway → provider router → shared tree
verifier → source providerの`DeleteAsync`である。immutableな子孫証拠を再確認し、記録済みの
子だけを下から非再帰削除する。全surviving directoryと外側ancestorのfull identityを維持し、
成功した自分の直下child削除の直後だけ、そのparentをA/membership/Bの一致で更新する。
完全削除と`SourcePartiallyDeleted`は既存の日英表示まで分離する。

## 検証結果

元のdeep run [38920109653](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/38920109653)
は失敗のまま保存する。1630成功・既存設定検体1失敗・live 16skipで停止しており、workflow全体が
成功したとは記録しない。ADR-0061に従い、成功済みの関連入力が不変な証拠と、今回完了した
未実行／失敗stageの補完を合わせて判断した。

| Layer | 通常test成功 | branch coverage | mutation | mutation基準 |
| --- | ---: | ---: | ---: | ---: |
| Domain | 100 | 100.00% | 95.54% | 95% |
| Application | 657 | 100.00% | 95.64% | 95% |
| Infrastructure | 627 + 新規44 | 92.52% | 90.32% | 90% |
| Presentation | 242 | 94.10% | 95.17% | 90% |

今回取得したInfrastructureの通常testは失敗0・opt-in live 16skip、Release buildは警告/error 0。
元のcollector/settingsで得た4layerのCoberturaをpackage単位で照合し、全閾値を満たした。
既存Infrastructureの`TestCategory=Adversarial`は各回300成功・0失敗・live 3skip、3反復すべて成功。
新規33件の3反復は別に記録し、既存の成功した300件を工程のために繰り返していない。
他3layerのcoverage、各3反復の異常系480実行、mutation、dependency auditは成功済み入力を再利用した。
Applicationの不正1文字hash guardの4変異Killedという前回補完も保持し、局所scoreを全体scoreに
置き換えていない。

Infrastructure mutationは固定Stryker 5.0.0、Complete/vstest、break-on-initial-test-failure、
閾値90、既存の対象・除外を維持した。今回はCLIの`--concurrency 1`だけでworkerを直列化した。
full identityがshared TEMPの祖先metadataまで観測し、別workerのfixture作成・削除が影響する
可能性を避けるためである。過去の並列結果に誤検出があったと実証したものではない。
repositoryのconcurrency 4設定やCI policyは変えていない。
最初の全Infrastructure実行は87.59%、Stryker exit 2（Killed1400、Timeout12、Survived136、
NoCoverage64、CompileError408、自動Ignored400）で失敗した。rawを保持し、成功へ読み替えない。
追加テストはsource/targetの単独不一致、複数ancestor中の一件不一致、観測中だけの変化、
parent再取得で観測した不整合、provider/modeの拒否を既存ownerの動作として証明する。
例外messageや内部の正確な呼出回数を閾値のためにassertしない。
削除直前の単独metadata不一致は観測した時点で拒否する。fixtureの復元callbackは該当guardを
外した場合に後段が同じ不一致で拒否を代行しないためのもので、正常実装が復元まで進んでから
拒否したという証明ではない。この区別は独立レビューとの確認に記録した。
局所mutationの初回はCLIの選択指定が誤って0件となり、scoreなし・exit 0を成功証拠に採用していない。
指定を直した最初の局所検証は88.89%・exit 2で、28件を新たに検出したがまだ補強が必要だった。
補強後の局所検証は92.31%・exit 0（Killed168、Survived8、NoCoverage6、Timeout0）。
旧全体rawとsource・region・operator・replacementを照合し、40件の新規Killedと既検出の後退0を確認した。
削除直前の単独metadata照合と検証entryの再取得は、対応する追加methodによる検出をrawの`killedBy`から確認した。
局所の未検出文字列や境界caseもrawに保持し、除外や基準変更は行っていない。
最終の全Infrastructure集計は90.322580645%（1456/1612）・exit 0で基準90%を満たした。
全2420件はKilled1435、Timeout21、Survived99、NoCoverage57、CompileError408、自動Ignored400。
旧rawとの全2420件の一意なtupleと製品sourceが一致し、既検出から未検出への後退は0だった。
局所で新規Killedだった40件は正式実行では39件Killed・1件Timeout（914）。914はコピー先parentの
不在判定を飛ばす変異で、同じtuple・最終test入力の局所実行では
`CopyWhenTargetParentIsUnavailableRejectsBeforeWritingTarget`によりKilledを確認済みである。
正式TimeoutをKilledへ変更せず、rawに理由がないため遅延原因も断定しない。
他の旧SurvivedからTimeoutへの4件（1260/1759/1792/2029）と旧KilledからTimeoutへの5件も、
新しいassertionによるKilledとは扱わない。旧Timeout12件は11件Timeout・1件Killedとなった。
局所に残る8Survived・6NoCoverageも正式実行で同じ状態を保持している。文字列変異やclosed modeを
除外せず、405のidentity衝突throwと439のfallbackは未実行境界として記録し、同値認定しない。
source削除の中核`VerifiedSourceDeletion`は66件Killed、Survived/NoCoverageは0。
全体score、局所の検出証拠、残存境界を分けて設計担当が受理した。
最終runの前後でHEAD・tracked/untracked status・全関連source/config/tool SHAが不変だった。
正確なraw/比較/Timeoutレビューは`infra-aggregate-final-20261017T072326050Z/`、
受理判断は`technical-acceptance.json`に保持する。

CodeQLの旧入力`30e6bc0`はcanonical analysis `1968566448`、164規則・666/666 C# file・
結果0件、branch open alert 0、#115 fixedを確認済みだった。今回のテスト補強はC#解析入力を
追加するため、最終候補についてcanonical build-mode noneの解析を更新した。
最終候補`0b9f5ba4d906f3ba63ea747cf5aa2cbc4950076e`をCodeQL2.27.2で解析し、
164規則・671/671 C# file（追加5fileをすべて含む）・結果0件を確認した。
GitHub canonical analysisは`1972096782`で、commit/ref一致、API error/warning空、
branch open alert0、#115 fixedを読戻した。build-mode noneの既存抽出error8群は
payloadを順序無視で照合して前回と完全一致し、追加テストの抽出errorはない。
この8群を「errorなし」とは記録しない。不変の製品codeについては前回のcompiler-fed補完を
再利用した。同補完は抽出error/warning・compile error0で、native呼出3件のnoteは
SEC-014/ADR-0063の受理済み境界である。canonicalのraw、exact argv/exit、抽出照合、
SARIF SHA-256、upload/API読戻しは`20261017/codeql-final/`に保持する。
独立読み取りレビューと設計担当の確認結果は最終候補の証拠と併記する。

## 正確な実行と失敗履歴

作業directoryは`D:/NeNeCommander/worktrees/issue-243`、SDKは
`D:/NeNeCommander/tools/dotnet-10.0.401`をprocess PATHで指定した。
TEMP/TMPは`D:/NeNeCommander/outputs/issue-243/tmp`。

```powershell
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore -- --coverage --coverage-settings D:/NeNeCommander/worktrees/issue-243/eng/coverage.settings --coverage-output-format cobertura --coverage-output D:/NeNeCommander/evidence/issue-243/20261017/infra-20261017T050738683Z/infrastructure.windows.cobertura.xml
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter TestCategory=Adversarial
```

上の異常系commandは3反復で各exit 0。他の上記commandもexit 0。
mutationはInfrastructure.Tests directoryから次を実行した。

```powershell
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --break-at 90 --concurrency 1 --output D:/NeNeCommander/evidence/issue-243/20261017/infra-final-20261017T051432782Z/mutation-infrastructure --skip-version-check
```

上の初回mutationのexitは2。最終mutationのcommand・exitは`dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --break-at 90 --concurrency 1 --output D:/NeNeCommander/evidence/issue-243/20261017/infra-aggregate-final-20261017T072326050Z/mutation-infrastructure --skip-version-check`、exit 0。
最終Strykerのsimulated buildは一度失敗し、Stryker自身の既定NuGet restoreへのfallbackでanalysis succeededとなった。手動restoreや同じrunの再起動は行わず、原因をstdoutだけから断定せずwarningを保持した。
検証前後のtracked/untracked statusはいずれも空で、HEAD・関連SHAも不変だった。
失敗履歴として、古いCodeQL package cacheを参照したassetsによるNETSDK1064を残す。
対象projectのlocked restoreでassetsを復旧してから成功した。異常系への追加TRX引数をCLIが
開始前に拒否したexit 1も保持し、canonicalのcommandへ戻した。mutation直列化の判断時に中断した
異常系2反復目は成功扱いせず、未完了の2・3反復目だけを実行した。成功したcoverageや1反復目、
他layerは繰り返していない。

正確なargv・exit・XML hash・raw reportは恒久evidence
`D:/NeNeCommander/evidence/issue-243/20261017/`の各run directoryと
`mutation-isolation-decision.json`へ保持する。前回の証拠は`20261016/`、#257は
`D:/NeNeCommander/evidence/issue-257/`。新しい文書変更はlink/diffと通常Commit checkだけで確認し、
製品test、full/deep CIを追加しない。文書保存時のcommandは
`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`で、exit 0。
追加44件のmethod/data rowと採用した各実行log、5fileのSHA-256は
`guard-reinforcement/final-case-inventory.json`と`verification-index.json`に対応づけた。
新規5classだけの通常実行、変更した異常系だけの追加反復、局所mutationの9個の独立した
`--mutate`引数も同じargv記録に保存する。局所scoreを全Infrastructureのscoreへ置き換えない。

## 統合、整理、残る別作業

設計担当が最終test候補`0b9f5ba4d906f3ba63ea747cf5aa2cbc4950076e`と選択した証拠を技術受理した。PR #249は文書head`18e30d9`を含め、2026-10-17 17:52:18 JSTにmain `a6587bfb16f1d804148d12e7f9d91d2945b573ad`へsquash統合済み。Issue #243 CLOSED、main同期・clean、taskとmainのtree一致を確認した。
PR #249の最終squash/main同期と整理実績は、PRの読戻しおよび恒久evidenceの
`20261017/completion.json`に記録済み。本報告commit後の機械的な統合では成功済みtestを再実行していない。
#257の追加worktree/outputは取込・未保存物・稼働参照・linkを確認して削除済み。
#243もmain取込と唯一のraw成果物の収載を確認して、次の二つを整理済み。

- `D:/NeNeCommander/worktrees/issue-243`
- `D:/NeNeCommander/outputs/issue-243`

branch、commit、恒久evidence、固定SDKは保持する。
以前のissue-240やissue-236の完了済み整理は再要求しない。

path最終確認からmutationまで、およびowned child削除から直後のparent再取得までのraceは
ADR-0069の明示した残存範囲である。tree凍結・writer排他・live 9Pの実証は主張しない。
手動release表示確認#94は別件。次は設定一時ファイル残留#250をred再現から扱い、終了契約#251、
P4 #244〜#248、低優先度#181は独立Issueのまま維持する。

## 後片付け完了

対象だった`D:/NeNeCommander/evidence/issue-243/20261016/supplemental-deep/mutation-raw`は削除済み。
別名で保存済みの原本を見落として重複コピーした5件だけが対象だった。
自動承認レビューが再帰削除を`blocked by policy`として拒否した履歴は保持し、サナは再試行していない。
2026-10-17 18:19:06 JSTにhideが準備済み`cleanup-pending.ps1`を実行し、完了結果を報告した。
同日18:20:31 JSTの読み取り確認で対象不在と原本5件の存在・SHA-256一致を確認した。
原本は親directoryの`mutation-application.json`、`mutation-domain.json`、
`mutation-presentation.json`、`mutation-hash-guard-character-span.json`、
`mutation-hash-guard-selection0.json`として保持する。
証拠は`D:/NeNeCommander/evidence/issue-243/20261017/cleanup-pending-completed.json`と
`cleanup-confirmation.json`。記録更新は[Issue #259](https://github.com/hideyukiMORI/NeNeCommander/issues/259)に対応する。
後片付けの保留はなく、同じ削除を再依頼しない。文書更新は差分・リンクと通常Commit-modeだけで確認する。
