# 引き継ぎ書 2026年10月17日 検証済み移動元削除

Status: informational

最終更新: 2026-10-17 18:21:49 JST。

[日報](../reports/2026-10-17-verified-move-daily-report.md)と
[ADR-0069](../adr/0069-verified-move-source-deletion.md)を参照する。
設計担当が最終test候補`0b9f5ba4d906f3ba63ea747cf5aa2cbc4950076e`と選択した証拠を技術受理した。PR #249は文書head`18e30d9`を含め、2026-10-17 17:52:18 JSTにmain `a6587bfb16f1d804148d12e7f9d91d2945b573ad`へsquash統合済み。Issue #243 CLOSED、main同期・clean、taskとmainのtree一致を確認した。

## 再開時の確認

1. 現在のhide指示、`AGENTS.md`、`PROJECT_STATE.md`、mainとGitHubの実態を確認する。
   PR #249の最終統合・整理は恒久evidenceの`20261017/completion.json`とPR読戻しに記録する。
   完了した検証・修正・整理を古い日報の保留記述から復活させない。
2. #257はPR #258でmain `5b6ac92`へ統合済み。未知schema検体3→4の1行だけを修正し、
   対象1件・build・format・Commit check成功。worktree/outputも削除済み。
3. #243の初回検証候補は`d2d3dba`で、その後mutation未達を受けて同境界のテストを補強した。
   最終test候補は`0b9f5ba4d906f3ba63ea747cf5aa2cbc4950076e`。追加テストと証拠は日報に記録する。
   文書更新やmergeだけで検証を繰り返さない。

## 保持する契約と証拠

唯一の実行経路は既存gateway → shared tree verifier → source-provider `DeleteAsync`。
コピー先に保存されていないsourceの観測済み変更を拒否し、残sourceと部分effectを正しく示す。
全surviving directoryと外側ancestorのfull identityを維持し、成功した自分の直下child削除の
直後だけ、そのparentをA/membership/Bで再取得する。object成分だけへの弱体化や任意のbaseline
更新を行わない。sourceは記録済みentryだけを非再帰削除する。

元のdeep run `38920109653`は既存schema検体1失敗で終了したfailureのままである。
ADR-0061で成功証拠を再利用し、Infrastructureの未完了項目を補完した。
通常testは627成功・0失敗・live16skip、branch92.52%、異常系は300成功・0失敗・live3skipを3反復。
その後に追加した5file・44ケースは最終入力ですべて成功し、うち異常系33ケースは各3回成功。
case単位の成功再利用と最終assert変更後の実行は`guard-reinforcement/verification-index.json`へ対応づけた。
初回mutationは87.59%・exit2で未達、そのrawと補強後の証拠を区別する。
最終mutationは90.32%、基準90。Stryker5.0.0/Complete/vstest/初期失敗時停止/対象・除外は不変で、
今回の実行だけ`--concurrency 1`にした。shared TEMP祖先へのfixture干渉を避けるためで、
過去の並列scoreの誤りを実証したものではない。config/CIの並列数4は不変。
正式rawはKilled1435/Timeout21/Survived99/NoCoverage57/CompileError408/自動Ignored400。局所の新規40Killedは正式では39Killed＋914の1Timeoutで、914は同じ最終入力の局所Killedを補足証拠として保持する。他の新Timeout4件をassertion killと扱わない。全2420tuple/source一致、未検出への後退0、405/439は未実行境界のまま。
他layerのbranchはDomain/Application/Presentation順100/100/94.10%、mutation95.54/95.64/95.17%。
4XML閾値照合と検証前後のclean-tree確認も成功。新規中核guardのsurvivor確認結果は日報に記録する。

CodeQLの旧入力`1968566448`は164規則・666/666 file・結果0、open branch alert0、#115 fixed。
テスト補強で新しいC#入力が加わるため、最終候補のcanonical解析を更新した。
最終候補`0b9f5ba4d906f3ba63ea747cf5aa2cbc4950076e`をCodeQL2.27.2で解析し、
164規則・671/671 C# file（追加5fileをすべて含む）・結果0件を確認した。
GitHub canonical analysisは`1972096782`で、commit/ref一致、API error/warning空、
branch open alert0、#115 fixedを読戻した。build-mode noneの既存抽出error8群は
payloadを順序無視で照合して前回と完全一致し、追加テストの抽出errorはない。
この8群を「errorなし」とは記録しない。不変の製品codeについては前回のcompiler-fed補完を
再利用した。同補完は抽出error/warning・compile error0で、native呼出3件のnoteは
SEC-014/ADR-0063の受理済み境界である。canonicalのraw、exact argv/exit、抽出照合、
SARIF SHA-256、upload/API読戻しは`20261017/codeql-final/`に保持する。

恒久evidenceは`D:/NeNeCommander/evidence/issue-243/20261016/`と`20261017/`、
#257は`D:/NeNeCommander/evidence/issue-257/`。
検証失敗・CLI拒否・中断を含む正確なargv/exit、raw XML/JSON/SARIFを保持する。
固定SDKは`D:/NeNeCommander/tools/dotnet-10.0.401`。追加worktree/outputは整理済みで、対象は
`D:/NeNeCommander/worktrees/issue-243`と`D:/NeNeCommander/outputs/issue-243`だけである。
main取込・唯一の成果物・稼働参照・linkを確認して削除した。branch/commit/evidence/SDKは保持している。

## 続く独立Issueと残存範囲

- #250: 旧残留tmpを用意した新storeの保存失敗を先にred再現する。foreign tmpの無条件削除は禁止。
- #251: 通常close、OS終了、kill/crash、取消、書込失敗の保証を既存shutdown ownerで分けて設計する。
- #244〜#248: P4として維持する。#94の手動release画面確認、#181のmapped-drive調査も別件。

最終queryからpath mutation、およびowned child削除から直後のparent再取得までのraceは残る。
外側ancestorの無関係な更新でも安全側に停止する。tree凍結・writer排他・live9P実証は主張しない。
今回のAstra設計・Sol/Luna担当分担はhideがこの作業で明示した範囲に限る。

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
