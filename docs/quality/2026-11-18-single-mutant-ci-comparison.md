# Issue #439 — 固定した単一変異のCI比較

#424/#429では、`PaneReducer.GetFocusIndex` のFirst→FirstOrDefault変異が
ローカルのpin済みStryker/VSTestでSurvivedだった。31 exact test IDによる
初期・coverage・変異の各phaseは全件Passedだったが、過去の全体CIではKilledと記録されている。
この変更は同じ限定入力をhosted Windowsで1回比較する手動入口を追加する。
この文書の時点ではCI比較をまだ実行していない。

## 固定条件と経路

- `diagnose-pane-mutant.yml` はinputsのない `workflow_dispatch` だけ。既存のquality/deep/dependency workflowは変更しない。
- `Invoke-PaneMutantComparison.ps1` が唯一の実行入口。対象repoのmainに対するmanual初回attemptだけを受け付ける。
  汎用command/selector、別branch、再attempt、自動retryは受け付けない。
- 制御checkoutと診断sourceは別で、後者は `b5d9af8dcf0c04622be517aaa3ce6bf0390e8b3e` のdetached worktree。
  作業木・temp・package cache・出力・証拠はDのtask領域。Dがなければ停止する。
- `pane-mutant-429.json` は#429原本の424 input hash、順序付き31 test ID、対象span、置換、tool hashを固定する。
  manifest自身のhashも入口で確認する。元commitの424 Git blobをCRLF化すると原本hashに全件一致するため、
  checkoutは `core.autocrlf=true` / `core.eol=crlf` とし、結果を実hashで照合する。
- 対象のlocked restore/tool restore/Release buildだけを準備し、
  `--mutate '**/PaneReducer.cs{12475..12537}'` と31 ID filterでStrykerを1回呼ぶ。
  Application break 95、Complete、VSTest、concurrency 4、その他のpin/configを変えない。
- `PaneMutantEvidence.psm1` がreportとraw VSTest eventを読み、
  3 phaseのID集合・重複・個別Outcome・統計・abort/cancel/error・単一active ID・coverageを照合する。
  targetの同一性はsource hash、path、location、mutator、replacementで確認する。
  生成された数値IDだけを恒久識別子にせず、実際のactive IDと対応づける。
- 注入methodの全snapshotを照合する。生成helper名と数値IDの対応だけを正規化し、
  RunSettingsのcontrol名が実際の注入controlを指すことも要求する。
  最後に424 inputと元の2 DLLの復元、Gitのclean状態を検査する。

規則: QLT-012/014/015、SEC-004/006/011、TST-008/012、ADR-0048/0061、GIT-001〜004。
waiver: none。製品/test source、schema、依存pin、閾値、除外、既存gateに変更はない。
初回beta #434の候補とは独立で、配布候補へ加えない。

## 終了値と証拠

Survivedのraw exit 2を成功へ変換しない。commandは開始時点でjournalを保存し、
終了後にraw exitを追記する。診断logは最初からartifact対象のevidence配下へ書く。
通常のwrapper failureやraw exit 2でもfinallyで検査結果を保存し、workflowの `if: always()` でuploadする。
入力や証拠の検査失敗は `evidenceValid: false` と具体的errorで別に記録する。
raw nonzeroがある場合はその値を保持し、raw 0または実行前でも証拠が不成立なら失敗する。

**job timeout、runner loss、強制取消時にfinallyやuploadが完走する保証はない。**
完了recordがなければ未完了として扱い、通常failureの証拠保存と混同しない。
raw log、runsettings、selected/actual IDs、個別失敗message/stack、注入source、
runner image/runtime/tool binary情報、source/assembly/artifact hashを残す。
環境情報は比較に要るallowlistだけで、全環境変数やtokenを出さない。

workflowの証拠範囲はこのrun-owned directoryだけで、hidden fileも含めてhash manifestと整合させる。
CI owned worktree/outputは明示cleanupを行わず、hosted runnerの廃棄まで保持する。
取得済みの証拠と#429のローカル原本は永久保持する。新しいcleanup機構は加えていない。

## この変更で行った検証

ADR-0061によりtool fixture、構文/設定確認、既存conformance/security、通常commit hookを選ぶ。
この席では実Stryker、製品build/test、CI、GUIを実行しない。
成功済みの#429実行を繰り返さず、保存済みraw logを検査器へ入力して再利用した。

| 確認 | 結果 |
| --- | --- |
| `pwsh -NoProfile -File eng/mutation-diagnostics/Test-PaneMutantEvidence.ps1 -Output D:/NeNeCommander/outputs/issue-439/proof-3` | exit 0、38 checks成功 |
| #429保存raw logを新readerで読み取り | initial/coverage/mutant各31件、active1813、Survived、15個の注入snapshot一致。control名の対応も一致 |
| PowerShell Parserによる3 script/moduleの構文解析 | error 0 |
| workflowの固定trigger・read権限・timeout・pinned actions・always uploadの読み合わせ | 条件と一致。専用YAML parserは現環境にないため未実行 |
| 通常commit hook | `eng/check.ps1 -Mode Commit` の結果を完了原本とcommit.logへ記録 |

38 checksは、Survived/Killedの正例、manifest改変、input不在/hash不一致/脱出、assembly復元差、
IDの欠落/重複、複数/誤active、phase欠落/重複、completion欠落、abort、初期失敗、skip、
統計不整合、複数tested mutant、置換/source/coverage/discovery/injection/control/filter/exitの不整合を含む。
raw exit 2の保持とjournal/log保存は `exit 2` する小さな疑似processで確認した。
非CI・非manual・別repo・別branch・不正run ID・attempt2はentryの別process試験で、
task保存先や製品commandに進む前に拒否された。

全試行は `D:/NeNeCommander/evidence/issue-439/`、fixture出力は `D:/NeNeCommander/outputs/issue-439/`。
proof-1は32 checks成功、command開始journalと直接evidence出力を加えたproof-2も32成功、
entry guardの6 checksを追加したproof-3は38成功。fixtureの失敗はなかった。
調査中のreportサンプル読取では空のtestFiles先頭要素を参照してIndexErrorになり、
存在するtarget/phaseを選んで読み直した。製品実行やsource変更ではない。
PyYAMLの存在確認は未導入と分かったためparser実行に進んでいない。新しい依存は追加していない。
独立レビューで、uploadのhidden対象とhash列挙の範囲を揃える指摘があり、
最終hash列挙へ `-Force` を追加した。証拠の列挙だけの変更で、成功済みfixtureや製品検証は繰り返さない。

## 引渡しと限界

rootがPR/main受理後、manual workflowを1回依頼し、run ID・制御commit・raw結果と取得したartifactを記録する。
現在のhosted imageでの比較であり、過去CI imageや当時の全体batch・前後の変異履歴の再現ではない。
CIもSurvivedなら単一条件の一致を記録して止め、過去Killedの原因を断定しない。
Killedなら最初の個別失敗、注入control/active ID、runtime/runner差を比較する。
比較条件が成立しなければ不成立として保存し、対象拡大や自動再試行は行わない。
この結果をrelease deep review、全層mutation score、元batchの原因確定に読み替えない。

ローカルの追加worktreeは未統合中保持する。push/PR/main/CI実行、全体docs、最終整理はrootが担当する。
