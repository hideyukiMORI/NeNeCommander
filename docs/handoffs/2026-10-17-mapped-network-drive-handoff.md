# 引き継ぎ 2026年10月17日 ネットワーク割当てドライブ

Status: informational

更新: 2026-10-18。Issue #181は実装・技術受理・main統合・後片付けまで完了。
本更新は[Issue #276](https://github.com/hideyukiMORI/NeNeCommander/issues/276)の文書整備で、製品入力は変更しない。
元の後片付け依頼は解消済み。同じ削除commandを再実行・再送しない。

正本は[PROJECT_STATE](../PROJECT_STATE.md)、
[ADR-0072](../adr/0072-mapped-network-drive-boundary.md)、
[日報](../reports/2026-10-17-mapped-network-drive-daily-report.md)。
[Issue #181](https://github.com/hideyukiMORI/NeNeCommander/issues/181)を
main `a6113c427b98eeddce94efa2f4aefc215b6bfd04`から再開した。
先行#251・#269・#271の統合と整理は完了済み。同じ削除依頼を再送しない。

## 維持する契約

- ドライブ文字をlocal capabilityと同一視しない。`WindowsDriveBoundary`を共通のOS事実境界とする。
  Network・Unknown・NoRootDirectory・未知の値と取得失敗は閉じる。既存の個別capabilityは緩めない。
- direct local adapter、source/destination、再帰child、verified source削除、recycleのcallbackでも再検査する。
  正規化前のraw traversal例外はverification/deletion ownerが捕捉し、既存の型付き拒否とpartial effectへ戻す。
- settingsの公開・cleanupも同じdrive境界を使う。危険なcleanupは残置結果にし、別の場所を削除しない。
  既存のdocument変更理由、schema、candidate tokenと所有権の意味を維持する。
- read結果の`DriveKind`は一覧に伴う観測事実。paneのstate変更や失敗で取り違えず、新しい成功readで置き換える。
  statusの警告・活動・sort表示を残したままnetwork表示を結合する。表示をmutation許可に使わない。
- 最後のqueryからpath reopenまでの競合、OS一括効果の内部再帰は残余。mapping固定やhandle-relative実装はない。
  UNC/network変更操作は未対応のまま。恒久削除の確認だけではidentity/capabilityの証明にならない。

## 検証証拠

`D:/NeNeCommander/evidence/issue-181/`に有効REDのsource/patch/SHA/argv/raw logを保持する。
local inspection 3件、pane表示2件、settings6件を修正前に再現した。
non-behavioral seamを含む再現なので、未変更mainの直接実行と記述しない。
compile・restore・0件filterの失敗はREDや成功件数に含めない。

現時点の成功はPresentation 67件、Application 131件、settings新規6件と既存80件。
settingsの17件filter runは設定以外の重複も含むため、17件を新規settings件数として加算しない。
Applicationの旧constructor反射テストとsettingsの既存失敗理由の順序は修正済み。
操作matrixのfocused 19、関連境界142、最後のcross/reader 4、最終reader/capability 99、
destination 13のfilterも成功。重複とsource時点があるため合計せず、
`infra/final/proof.json`の再利用・上書き対応を読む。
read観測中取消と、既存main由来の空列挙後取消は別々のREDを保存して修正済み。
最終App Release buildは警告・エラー0。変更C# formatは成功済みの各owner分に加え、
未確認10 fileとAppの1 fileをrootが補完し、全変更分を確認した。conformance 112規約成功。
独立読み取り専用レビューは未解決の具体的P1/P2なし。`review/README.md`を参照。
settings最終sourceの実行前hashは未採取であり、成功後に採取した現行hashと混同しない。

最初のcommit hookのSEC-014指摘2件は、既定`ReadWslFacts`参照をWSL owner内へ戻して修正した。
規則は不変。修正後のInfrastructure.Tests buildは警告・エラー0、対象8件・security-check・
変更5 fileのformatが成功し、独立再レビューに追加P1/P2なし。formatのworkspace-load警告は保持する。
App buildはこの内部構成修正前で、candidate全体のRelease buildは後述のdeep reviewで成功した。
`root/commit.log`と`infra/final/proof.json.sec014Followup`を参照。

candidate `2e6c0aeed0df0dca41b437eedd6d3d0063f218be`はCommit検査成功済み。
[draft PR #273](https://github.com/hideyukiMORI/NeNeCommander/pull/273)で
[deep review run 39036679946](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/39036679946)を明示起動した。
canonical/coverage/adversarial/mutation/CodeQLを含むためqualityを別に重ねない。
元runは通常test段階で失敗。全体buildと1,733通常testは成功、2件のtree-copy null testが
Public-only reflectionでinternal methodを検索していた。live 16 skipは未実行のまま。
製品コードを変えずexact internal signatureのnull/callback未呼出し証明へ修正し、不足工程を個別に補完した。
Domainの全関連入力はIssue #243から不変で、`ci/domain-evidence-reuse.json`に成功証拠の再利用を記録した。
CodeQL analysis `1972885555`は685/685 C#、164 rules、結果0・branch open alerts 0。
元workflowはfailedのまま保持する。補完を含む最終技術受理は以下の確定版CodeQLと併せて読む。
補完済み: null test 2件、Application/Infrastructure/Presentation coverage 100/92.138177/94.246862%、
明示dependency audit（脆弱性なし）、adversarial各3回（88/362/12成功、Infra live 3 skip）。
Complete mutationもApplication 95.60%、Infrastructure 90.22%、Presentation 95.05%で成功した。
Domain 95.54%は前記の入力差分確認に基づく再利用。閾値・除外・concurrencyは不変。
Infrastructureの新しい拒否境界5箇所の追加・強化8件（unique増加7）は通常・ADV-024×3とも成功。
5 owner限定mutationは95.82%、確認対象7 mutantは全てKilled。settingsの既存ADV-012も
同長JSON・復元identity一致で1件強化し、通常・3反復成功、`DestinationMatches`文字範囲の
mutationは90.91%、内容比較をORに変えるmutant 2433はKilled。これらは全体scoreに置き換えない。
`supplemental/`と`root/mutation-review.md`を参照。
最終candidate `f9635abeadc798894ec319f62d86f2de794acf08`のCodeQLも完了。
analysis `1973042465`は685/685 C#・164 rules・結果0・open branch alerts 0で、2026-10-17に技術受理した。
生成コード/native/Appの既存8 diagnostic groupは保持し、#243以後に増えた生成`InputSurface`参照1件は
先行#251のものと確認した。新しいdrive境界と補完testに新たな抽出問題はない。
`ci/codeql-final/`と`root/acceptance.json`に照合・API結果を保存する。文書だけの最終更新は再解析しない。
全体通常testや成功済みstageを工程都合で再実行しない。
成功証拠は関連入力が不変なら工程変更やpush/mergeだけを理由に繰り返さない。
live share/mapping/removableと#94手動release確認は別tierで未実行。
P4 Issues #244–#248は今回に混ぜず、記録のみのまま維持する。

## 作業場所と整理

branch `fix/181-mapped-network-boundary`は保持済み。
worktree `D:/NeNeCommander/worktrees/issue-181`と一時出力 `D:/NeNeCommander/outputs/issue-181`は削除済み。
恒久証拠は `D:/NeNeCommander/evidence/issue-181`、SDKは共有tool領域に保持する。
PR #273は2026-10-18 00:01:38 JSTに`5d817de8e91b68155148fe50ca14db7abaf76a2b`へsquash merge済み。
main同期・clean・task tree一致を確認し、Issue #181の追加worktreeは削除した。branch・commitは保持する。
出力削除は当初、自動承認レビューが`blocked by policy`で拒否し、コマンドは開始されなかった。
エージェントは別手段で再試行していない。Issue #274 / PR #275はその時点の保留記録であり、
00:10:18 JSTに`fc709c895ae9b311a2d4468ccd515bbe1c6a75ae`へ統合済み。

hideが実行したcommandは次のとおり。再実行依頼ではない。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-181/cleanup-completed-output.ps1
```

00:44:44 JSTに完了し、実行記録は対象不在・link entry 10個の解除を示す。
00:47:08 JSTの読み取り専用確認でも出力と#181・#274のworktreeは不在だった。
37 raw log・51補完artifact・4 TRX・CodeQL証拠、branch・commit、共有SDKは保持する。
`cleanup-pending.json`は拒否時点の履歴。現在の完了状態は`cleanup-executed-by-hide.json`、
`cleanup-confirmation.json`、`completion.json`を正本とする。後片付けの実行待ちはない。

## 次回の入口

1. AGENTS.md、PROJECT_STATEと実際のIssue/PR状態を確認する。製品統合はPR #273、本書整備はIssue #276のPR。
   文書PRの最終統合とworktree整理は`D:/NeNeCommander/evidence/issue-181/report-closure/completion.json`で確認する。
2. 変更fileは既存の日報、本書、PROJECT_STATEだけ。記録の整合性を`git diff --check`、リンク確認、
   通常commit hookの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`で確認する。
   終了結果はIssue #276のPRと`report-closure/`へ保存する。製品検証やCIを文書更新だけの理由で再実行しない。
3. 実share・network mapping・removableの証明と#94の手動release確認は未実施として扱う。
   synthetic事実や所有temporary rootによる成功を実環境の成功と読み替えない。
4. P4 #244–#248は記録のみ。UNC/network変更操作やshare列挙などの追加機能は別の要求・契約で扱う。
   過去の役割分担は当時の実装記録として読み、次の作業へ自動的に広げない。
