# 日報 2026年10月16日 検証済み移動元の削除

Status: informational

NeNe Commanderサナはhideの2026-10-16の委譲を受け、設計・判断を担当し、
実装と関連検証をSol、次Issueの読み取り調査をLunaへ依頼した。
[前回の引き継ぎ](../handoffs/2026-10-15-external-review-handoff.md)に従い、
最優先の[Issue #243](https://github.com/hideyukiMORI/NeNeCommander/issues/243)を
既存[Draft PR #249](https://github.com/hideyukiMORI/NeNeCommander/pull/249)で再開した。
開始時のmainは`66fc30d`でclean・同期済み。再現commit `4bd47d2`を保持し、
mainを`770d7dd`で作業branchへ取り込んだ。

## 修正の契約

[ADR-0069](../adr/0069-verified-move-source-deletion.md)を設計判断として受理した。
既存gateway、共有tree engine、source providerを唯一の実行経路として維持する。
コピー後にsource/targetの子孫identity・正確なmembership・実bytesを照合し、
immutableな証拠を既存`DeleteAsync`へ渡す。削除直前に再確認し、記録済みの子だけを
下から削除する。directoryは非再帰削除で、後から追加された子を巻き込まない。
一部削除後の停止は`SourcePartiallyDeleted`として完全削除と分けて表示する。

独立レビューで、WSLのinode成分だけを残す案ではdirectoryの再利用を識別できない点を確認した。
全directoryのfull identityを維持し、成功した自分の直下child削除の直後だけ、
そのparentのobject成分・残membership・前後二つの同一full captureを確認して更新する。
未処理のsiblingや外側ancestorの基準を更新しない。

pathの最終確認からmutationまでの既存raceと、owned child削除から直後のparent再取得までの
狭い追加raceは残る。tree凍結、9Pの排他、完全な競合防止は主張しない。
外側ancestorのfull metadata変化でも安全側に停止するため、無関係なsibling更新により
転送が中断される場合がある。live 9PやIssue #94の実画面確認は今回のmapped fixtureとは別の証拠である。

## 検証と現在地

関連buildとselected testが成功した。Applicationの85件と追加取消1件、Presentationの31件は
成功、失敗・skipとも0。App Release buildも警告・errorとも0で成功した。
元の2件のredは修正後に成功し、その失敗証拠も保持している。
Infrastructureの初回151件は139成功・12失敗。診断により、長いlocal pathのnative query、
NTFSで模擬したWSL ancestor directoryの非zero EOF、旧`PermanentMode`期待の移行漏れを
特定した。productionの9P異常値拒否を維持し、typed local pathのnative表現とtest用factsを
修正した後の関連40件は成功した。初期の並列実行仮説は未立証であり、原因と扱わない。
無関係なdirectoryの再固定禁止はmembership不変のctime変更で直接検証した。
その補強とstream/token/null guard等を含む最終19件、最終membership検査後の新規childを
非再帰削除が保持することを含む3件も成功した。40/19/3のbatchは一部重複する実行数で、
distinct test数として合算しない。すべて失敗・skipとも0。
Commit checkのCS-012 filename違反は、verifierを既存tree engineの一つのfileへまとめて
修正した。最終formatとCommit checkは成功（112規則、23adversarial登録）。規約の例外や
閾値変更は追加していない。失敗を再試行で隠さず、変更された入力に関係する証拠だけを取り直した。

この変更は全composite moveの共有された破壊的操作境界に関わるため、ADR-0061に従い
最終候補`e13330d`でdeep-reviewを一度明示dispatchした。deepはfull canonical gateを含むので、
同じ入力に対するquality CIは重ねていない。
正確なcommands、失敗履歴、TRX、最終結果は`D:/NeNeCommander/evidence/issue-243/20261016/`へ保持する。

2026-10-17 00:16 JST、[run 38920109653](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/38920109653)は
失敗で終了した。canonicalのtestは1630成功・1失敗・live環境16skip。
唯一の失敗はmainにも存在する設定テストで、対応済みschemaVersion 3を未知バージョンと
期待する古い検体だった。[Issue #257](https://github.com/hideyukiMORI/NeNeCommander/issues/257)へ
分離し、今回の移動処理へ混ぜて修正していない。coverage以降のdeep項目は未実行であり、
成功を主張しない。

CodeQL analysis `1968236380`の結果は1件、branchのopen alert #115
(`cs/complex-condition`)も1件だった。新規テストのmetadata注入条件を小さなswitch述語へ
整理し、root/siblingの対象2行が成功、Release buildは警告・errorとも0、formatとCommit
checkも成功した。製品コードには変更がない。CodeQL再解析は未実行。
成功済みの証拠を保持し、未実行項目と変更した入力に関係する検証だけを進めている。
技術受理、merge、cleanupは未完了。

続けて元のcoverage collector/settingsで未実行項目を確認した。Applicationは657成功・
branch 100.00%、Presentationは242成功・branch 94.10%で、ともに失敗・skipとも0、
変更コードの未通過branchなし。必要基準100%/90%を満たす。raw Coberturaと実行commandは
恒久evidenceの`coverage/`に保持し、以後入力が変わらなければ再利用する。

Domainは前回のdeep成功後に#224の変更があるため、古い結果をそのまま使わず確認した。
100件成功・branch/lineとも100%。Domain/Application/Presentationの異常系は各回
60/88/12件、3反復すべて成功（計480実行、失敗・skipとも0）。直接・推移的依存の監査は
脆弱性なし。固定Stryker 5.0.0、既存Complete/vstest/concurrency4の設定で、mutationは
同順95.54/95.64/95.17%となり、基準95/95/90を満たした。元の全体結果とtoolが自動分類した
compile-error/ignored/timeoutもraw reportに残す。閾値変更や新しい除外はない。

Infrastructureの未実行coverageは除外なしで一度だけ取得した。626成功・既知の#257が1失敗・
live 16skip、exit 2のまま。Coberturaのbranchは92.07%で基準90%を超えるが、stage成功とは
扱わない。変更コードには未通過の防御分岐があり、全分岐の通過を主張しない。
削除owner `VerifiedSourceDeletion`には未通過branch entryはなかった。
Infrastructureの異常系反復とmutationは、初期テストの既存失敗が未解決のため開始していない。

内訳レビューで、directory専用callerしか持たない`TargetMatches`に残っていたfile用分岐を
削除し、引数を`VerifiedDirectory`へ絞った。実行されるcapture/membership/captureは不変で、
関連buildと既存7ケースが成功した。Applicationには64文字中1文字だけが不正なhashの
拒否ケースを追加し、対象methodの5行が成功。mutationの部分範囲指定を行番号と誤認した
最初の実行は選択0件だったため、exit 0でも成功証拠に含めない。公式仕様の文字位置
`635..718`でguardの83文字を選び直した補完は、対象4変異すべてKilled、局所score100%で成功。
`Any`→`All`も検出した。元のApplication全体score95.64%を局所scoreで置き換えず、
例外message変更のsurvivorも元のraw reportに保持する。CodeQL再解析はまだ未完了。
正確なcommands、raw reportと
選択0件の履歴は`D:/NeNeCommander/evidence/issue-243/20261016/supplemental-deep/`へ保持する。

## 次の独立作業と保持物

設定一時ファイル残留の[#250](https://github.com/hideyukiMORI/NeNeCommander/issues/250)と
終了契約の[#251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)は未実装。
#250は旧残留を用意した新storeの保存失敗を先に再現する。通常例外のcleanupをcrash残留と混同しない。
#251は既存Closed後drainと、実際の最終window/OS終了までの保証を分けて設計する。
P4の#244〜#248、手動release確認#94、低優先度#181も別Issueのまま維持する。

`D:/NeNeCommander/worktrees/issue-243`と`D:/NeNeCommander/outputs/issue-243`は
現在の実装・検証が未完了のため保持する。恒久evidenceと固定SDKは別領域に残す。
完了後は取込・唯一の成果物・未追跡/無視ファイル・稼働参照・linksを確認して整理する。
過去のissue-240 cleanupは2026-10-15に不在確認済みで、再要求しない。
