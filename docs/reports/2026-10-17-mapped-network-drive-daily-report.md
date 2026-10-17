# 日報 2026年10月17日 ネットワーク割当てドライブの境界

Status: informational

hideの「対応を続けて」を受け、[直前の日報](2026-10-17-owned-window-shutdown-daily-report.md)と
[引き継ぎ](../handoffs/2026-10-17-owned-window-shutdown-handoff.md)、実際のIssue/PR状態を照合し、
[Issue #181](https://github.com/hideyukiMORI/NeNeCommander/issues/181)を再開した。
開始時のmainは`a6113c427b98eeddce94efa2f4aefc215b6bfd04`でclean・originと同期していた。
#251・#269・#271の統合と後片付けは完了済み。P4 Issues #244–#248は記録のみのまま維持する。
hideがこの作業で指定した分担に従い、NeNe Commanderサナが設計と判断、Solが各ownerの実装・検証、
別のSolが読み取り専用レビューを担当する。

## 対象と設計

[ADR-0072](../adr/0072-mapped-network-drive-boundary.md)を製品変更より先に受理した。
ドライブ文字の構文は、ローカル媒体や安定したネットワーク先のidentityを保証しない。
既存のrecycleはFixed-only、atomic moveはvolume identityを確認していたため、これらを未対策と扱わない。
今回の不足は、割当てドライブがlocal adapterへ入り、ローカルidentityや効果を使えることだった。
Issue初回の恒久削除許可案は履歴として保持し、今回の受入れ範囲をIssue先頭へ追記した。
ネットワーク変更操作には別のidentity/capability契約が必要なため、今回は明示的に拒否する。

`WindowsDriveBoundary`が既存I/O worker内でOSの現在のdrive typeを読み、
Network・Unknown・NoRootDirectory・未知の値をlocal identityと操作から除外する。
Fixed・Removable・CDRom・Ramの観測だけでNTFS機能や書込み権限を付与せず、既存の個別条件を維持する。
local adapter、両方向のWSL転送、再帰copy、verification、証拠付きsource削除、
recycle直前のcallbackと設定storeが同じ境界を各ownerから使う。
拒否は既存の型付き結果へ戻し、既にできたcopy targetや部分削除の事実を保持する。
設定保存は公開・所有一時ファイルの片付け前にも再検査し、危険な場所では残置結果を返す。

一覧読込みとShell起動は維持する。読込み成功に任意の`DriveKind`を伴わせ、
`PaneContentListed`が保持し、Presentationが既存status行へ日本語・英語のnetwork表示を加える。
この表示事実は操作許可には使わない。選択・並べ替え・失敗・取消・放棄は表示中の一覧に対応した事実を保持し、
次の成功読込みで置き換える。Appは投影を渡すだけで、layout・style・入力経路を変更しない。
公開read-success factoryとstatus-line APIは全呼出し側を同じ変更で移行し、旧overloadを残さない。

現在のqueryで観測した割当て変更は拒否するが、最後のquery後からpath reopenまでの競合は残る。
OS一括の再帰削除やShell処理の内部child操作までadapterが個別制御したとは主張しない。
Win32 identifier、settings schema、依存package、資格情報、実ドライブ割当ては変更しない。

## 検証と証拠

ADR-0061により、実装中は影響するbuild・test・formatを選択する。
local mutation全体、転送、settingsと共有read結果に及ぶ安全境界の変更なので、
安定candidateに一度の明示的security deep reviewを実行した。
deep reviewがcanonical gate・coverage・adversarial反復・mutation・CodeQLを含むため、
同じ入力にquality CIを重ねない。単にpushやmergeをした理由で成功証拠を再実行しない。

修正前の有効REDは、local inspectionの3件、pane表示の2件、settingsの6件を保存した。
必要な非動作seamを追加した段階で再現しており、未変更mainをそのまま実行した記録とは区別する。
source・patch・SHA・実行引数・raw logは`D:/NeNeCommander/evidence/issue-181/`配下に保持する。
restore不足、compile failure、filter 0件はREDとして数えず、そのまま履歴を残す。

Presentation 67件とApplication 131件が成功。
settingsは新規6件と既存store 80件が成功し、対象file formatも成功した。
既存settings 2件が示した失敗理由の優先順は、drive拒否を追加しつつ従来のローカル結果へ修正した。
テストAPI移行、XML説明、constructor、改行・規約に関する途中失敗はraw logに分離する。
Infrastructureはfocused 19件、関連境界142件、最後のcross/reader 4件、
最終reader/capability 99件、destination 13件が成功した。これらは重複filterと異なるsource時点の
実行なので合計件数にしない。最終capabilityは既存の実在確認契約を広げずdrive判定だけに限定し、
source linkの既存verification理由も維持した。変更した部分は最後の99件・13件で上書きし、
不変部分は前の成功証拠を再利用する。正確なargv/filter・時点・matrix対応は
`infra/final/proof.json`と`README.md`に記録する。

新しいdrive観測中の取消漏れと、レビューで見つかった既存の空列挙後の取消漏れは、
それぞれ1件のREDを別保存して同じread owner内で修正した。
後者をドライブ確認追加で初めて発生した問題とは記述しない。
関連Release buildと最終App buildは警告・エラー0。
変更C#のformatは各ownerの成功証拠を再利用し、未確認だった10 fileとAppの1 fileをrootで補完した。
conformanceは112規約成功。独立読み取り専用レビューに未解決の具体的P1/P2指摘はない。
レビューは`review/README.md`、Appの正確なコマンドとraw logは`root/app-commands.txt`から参照できる。
settings最終sourceの実行前hashは未採取で、成功後のCRLF変更と現行hash採取を`settings/README.md`に区別する。

最初のcommit hookはSEC-014の2件を検出してcommitを止めた。規則やallowlistは変更せず、
`ReadWslFacts`の既定参照を`WindowsWslFileSystem`内へ戻し、共有drive境界をconstructorで渡す。
修正後のInfrastructure.Tests Release buildは警告・エラー0、null guard・構成・直接境界の8件、
`eng/security-check.ps1 -SkipProof`と変更5 fileのformatが成功した。formatのworkspace-load警告は記録する。
独立レビューもこの差分に追加P1/P2なし。App buildはこの内部構成修正より前の証拠であり、
candidate全体のbuildは後述のdeep reviewで成功した。失敗hookは`root/commit.log`、
修正後のargv/log/hashは`infra/final/proof.json`の`sec014Followup`に保持する。

commit `2e6c0aeed0df0dca41b437eedd6d3d0063f218be`でCommit規約・112 conformance・24 security登録の検査が成功し、
[draft PR #273](https://github.com/hideyukiMORI/NeNeCommander/pull/273)を作成した。
このcandidateへ[deep review run 39036679946](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/39036679946)を
2026-10-17 22:28:09 JSTに明示起動した。
candidateの全変更file hashと実行先は`root/candidate-manifest.json`、成功hookは`root/commit-02.log`に保存する。
このrunは失敗した。negative gate proof、security/supply-chain proof、locked restore、format、
全体Release build（警告・エラー0）は成功し、通常testsは1,733成功・2失敗・live 16 skipだった。
失敗した2件は既存tree-copy null testがinternalへ移したCopy/PreflightをPublic-only reflectionで
検索していたためで、製品コードを変えずexact internal signatureへ追従し、全引数nullとcallback未呼出しを確認する。
成功済みstageの再実行は求めず、未実行のcoverage以降を個別に補完した。Domainはsource/testと全関連設定が
Issue #243の成功時から不変で、coverage 100%、Complete mutation 95.54%、adversarial 60件×3回を再利用する。
差分と元証拠hashは`ci/domain-evidence-reuse.json`。元workflowを成功に書き換えない。
後続CodeQL analysis `1972885555`はcandidateの685/685 C#を解析し、164 rules・結果0・branch open alerts 0。
build-mode noneの依存解決試行の失敗や診断はraw logに残し、通常Release build成功と区別する。
workflow log、元failed report、API結果とSARIF exportは`ci/run-39036679946/`に保持する。

補完のnull test 2件は成功。canonical coverage settingsを用いた実行はApplication 660件・100%、
Infrastructure 712成功/16 live skip・92.138177%、Presentation 258件・94.246862%で、各分岐閾値を満たす。
明示dependency auditは不足していたArchitecture.Tests/Domain.Testsのassetsだけをlocked restoreして成功し、
脆弱性を検出しなかった。Domain testを再実行したものではない。
adversarialはApplication 88成功、Infrastructure 362成功/3 live skip、Presentation 12成功を
各3回確認した。Complete mutationはApplication 95.60%（閾値95）、Infrastructure 90.22%、
Presentation 95.05%（後二者の閾値90）で成功した。Domainは前記の95.54%を再利用する。
閾値・除外・concurrencyは変更していない。Infrastructureの全体成功後、今回の変更に属する
取得失敗・型付き拒否の未実行分岐を確認し、製品を変更せず5境界の8ケースを追加・強化した。
既存1ケースの2行化と新規6ケースで、unique件数の増加は7。通常8件とADV-024の8件×3回が成功し、
5 owner限定mutationは95.82%、確認対象7 mutantは全てKilledになった。

settingsの既存並行編集テストも1件強化した。従来の編集は長さが変わり、metadataを復元しても
byte比較の独立性を示せなかった。同じ長さの有効JSONへ書換え、復元後のidentity一致をassertする。
この1件とADV-012の3反復が成功し、`DestinationMatches`の文字範囲だけのComplete mutationは
90.91%。drive許可とbyte一致をORに変えるmutant 2433がKilledになった。
限定scoreを新しいInfrastructure全体scoreとして扱わず、元の90.22%と補強結果を別に保持する。
scope・コマンド・raw JSON・source hashは`supplemental/`、設計判断と照合は`root/mutation-review.md`。
補完testを含むcandidate `f9635abeadc798894ec319f62d86f2de794acf08`は通常commit hookを通過した。
同じCodeQL 2.27.2・canonical設定・build-mode noneで最終解析し、685/685 C#を抽出、164 rules・結果0。
GitHub analysis `1973042465`もこのcommitで結果0・open branch alerts 0を返し、2026-10-17に技術受理した。
raw diagnosticは既存の生成コード/native/Appに関する8 groupを保持する。以前の#243との比較で増えた
1件は#251で加わった生成`InputSurface`参照で、今回の変更ではない。XAMLの宣言と成功した全体buildを
照合した。新しいdrive境界と補完testに新たな抽出問題はなく、診断なしとは記述しない。
最終command、SARIF、照合とGitHub API readbackは`ci/codeql-final/`、受理一覧は`root/acceptance.json`。
以後の文書更新は検証入力を変えず、成功済みbuild/test/mutation/CodeQLを再利用する。

実share、network mapping、removable mediaと[#94](https://github.com/hideyukiMORI/NeNeCommander/issues/94)の
手動release確認は未実行の別tier。synthetic drive factsと所有temporary rootの試験をlive証明と混同しない。

## 統合と整理

作業branchは`fix/181-mapped-network-boundary`、worktreeは`D:/NeNeCommander/worktrees/issue-181`。
一時出力は`D:/NeNeCommander/outputs/issue-181`、恒久証拠は`D:/NeNeCommander/evidence/issue-181`に置く。
実装と技術受理は完了し、PR #273を2026-10-18 00:01:38 JSTにsquash mergeした。
mainは`5d817de8e91b68155148fe50ca14db7abaf76a2b`へ同期・cleanで、受理したtask treeと一致した。
Issue #181はclosed、追加worktreeは削除済み。branch・commit・恒久証拠・共有SDKを保持する。

未保存・未追跡・ignored file、唯一の成果物、稼働参照とlinkを確認し、37 raw log・51補完artifact・
4 TRXとCodeQL証拠を恒久領域へ保存した。出力内のjunction 10個は全て同じ所有一時領域内を指し、
稼働参照はなかった。しかし出力削除は自動承認レビューが`blocked by policy`で拒否した。
コマンドは開始されず、別手段での削除再試行もしていない。読み取り確認で出力の残存を確かめた。
この実績と保留は`completion.json`、`cleanup-preflight.json`、`cleanup-pending.json`へ保存し、
Issue #274で本書・引き継ぎ・PROJECT_STATEへ反映する。文書だけのため製品検証やCIは追加しない。

! hide実行待ち：後片付け

対象は`D:/NeNeCommander/outputs/issue-181`のみ。理由は自動承認レビューの`blocked by policy`。
保持するものは`D:/NeNeCommander/evidence/issue-181`、branch・commit、共有SDK。
下記をhideで実行する。対象外に置いたscriptは構文と範囲を確認済みで、サナは実行していない。
既知linkを追跡せず外し、対象directoryを1回のnative呼出しで削除する。削除済みでも実行できる。
個々のfile再検証・削除loopや恒久証拠の再hashは行わず、完了時間の保証はしない。

```powershell
pwsh -NoProfile -File "D:/NeNeCommander/evidence/issue-181/cleanup-completed-output.ps1"
```
