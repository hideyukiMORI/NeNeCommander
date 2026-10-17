# 引き継ぎ書 2026年10月17日 設定保存の残留tmp対策

Status: informational

[日報](../reports/2026-10-17-settings-temporary-recovery-daily-report.md)と
[ADR-0070](../adr/0070-attempt-scoped-settings-temporary-files.md)を参照する。

## 再開した範囲

#243・#257・#259・#261は完了済み。開始mainは`66fc114`で同期・clean、open PRは0だった。
hideの現在の委譲を受けて#250を再開し、初回red再現から設定保存の候補名を変更した。
Issue本文の2026-10-15「追跡のみ」は過去phaseの記録であり、今回の実装範囲は先頭に追記済み。

## 維持する契約

- 唯一の経路は既存`ISettingsStore`→`WindowsLocalSettingsStore`とcomplete settings queue。
- Appが`IIdentifierSource`の唯一の実装`WindowsIdentifierSource`を注入する。
  constructorは最大候補pathの形だけ検証し、admitted writeが一回だけ`NextIdentifier()`を呼ぶ。
- 候補は`<document>.<Guid:N>.tmp`。衝突はclosed rejectionで、別候補へ逃げない。
- 旧固定tmpと別attemptの残留は所有不明のため保持する。startup回収、昇格、復元、列挙はない。
- CreateNew、flush、handle identity、exact bytes、ancestor anchor、最終destination照合、
  owned cleanup、既存取消境界を弱めない。
- 別storeの候補は同時に存在できる。検出した先行保存は拒否するが、双方が最終照合を通過した後の
  last-writer raceは残る。固定tmpの偶発的直列化は廃止され、CAS/mergeの保証はない。

CS-010の例外は専用ownerの正確なpathと識別子生成のconcernだけである。
他pathの直接参照（method groupも含む）、Guid alias/static import、owner内のclock/environmentは拒否する。
`eng/prove-gates.ps1 -Rule CS-010`で今回のidentifier proofだけを実行でき、既定Allからも失われていない。

## 証拠と整理

145 unique case（store 80、直接consumer 63、architecture/lifecycle 2）が成功した。
変更したassertionだけを追加実行し、変化のない成功証拠を再利用した。
影響するRelease buildは0 warnings/errors、変更ファイルformatはexit 0（workspace load warningあり）。
CS-010限定proofは11 fixture成功。最終19ファイルの独立レビューに具体的P1/P2指摘はない。
正確な検証command、失敗履歴と成功再利用は日報・恒久証拠を参照する。
PR #263はmerged、#250はclosed。main `477310e`は同期・cleanで検証したtask treeと一致した。
通常Commit hookとcommit-message validatorも成功。詳細は`completion.json`を参照する。
恒久証拠は`D:/NeNeCommander/evidence/issue-250/`に保持する。
redのlog・当時のhash・setupは保持するが、red時点のraw source/patchは未収載である。
CS-010初回10 fixtureのraw logも後続runで上書きされ未保存。最終11 fixtureのlogは保持する。
未実行のfull/deep・coverage/mutation・live UIを成功扱いしない。
成功済みcaseは関連入力が変わらなければ工程変更や文書更新だけで再実行しない。

作業用コピーは`D:/NeNeCommander/worktrees/issue-250`、一時出力は
`D:/NeNeCommander/outputs/issue-250`。取込・未保存物・唯一の成果物・稼働参照・linkを確認し、
追加worktreeは削除済み。一時出力もhideの実行後、末尾の通り不存在を確認した。
branch/commit/evidenceと`D:/NeNeCommander/tools/dotnet-10.0.401`は保持する。

## 次の独立Issue #251

現行Issue本文は未実装の終了契約を追跡している。最初に`CommanderApplication.ShutdownAsync`、
`CommanderWindow`のclose、`AsyncWorkOwner.StopAsync`、`SettingsSession.ShutdownAsync`と関連testsを読む。
通常closeと最終WinUI window/message loopの寿命を、neutral ownerのcancel/await証拠と混同しない。
OS終了、kill/crash、描画喪失、cancel、write failureの保証と非保証をADRで分ける。
kill/crashでawait完遂を保証しない。partial copyの自動削除、未実装の即時取消、新promptを勝手に追加しない。
host/OS終了の実験はこの作業ではしていない。

P4 #244〜#248、#181、hide立会いの手動release確認#94は別作業として維持する。
今回の設計担当とSol/Lunaの分担は現在のhide指定範囲に限る。

## 後片付け完了

hideが20:10:04 JSTに準備済みscriptを実行した。
20:11:17 JSTの読み取り専用確認で`D:/NeNeCommander/outputs/issue-250`の不存在を確認済み。
Issue #250と#264の追加worktreeも削除済みで、branch、commit、恒久証拠、固定SDKは保持されている。
以前の`blocked by policy`は履歴。サナは拒否された削除を別手段で再試行していない。

`D:/NeNeCommander/evidence/issue-250/`の`cleanup-pending-completed.json`、`cleanup-confirmation.json`、
更新した`completion.json`が証拠である。Issue #266が完了確認の記録更新を扱う。
後片付け待ちはなく、同じcommandの再実行を求めない。
