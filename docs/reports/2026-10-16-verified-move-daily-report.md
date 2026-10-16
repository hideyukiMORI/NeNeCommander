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
最終候補でdeep-reviewを明示dispatchする。deepはfull canonical gateを含むので、
同じ入力に対するquality CIは重ねない。現時点ではdeep完了、技術受理、merge、cleanupを主張しない。
正確なcommands、失敗履歴、TRX、最終結果は`D:/NeNeCommander/evidence/issue-243/20261016/`へ保持する。

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
