# 日報 2026年10月17日 設定一時ファイルの残留から保存を再開する

Status: informational

hideの再開指示と役割分担に従い、NeNe Commanderサナが設計・判断を担当し、
Solが実装と関連検証、Lunaが識別子生成の規約チェック、別のSolが読み取り専用レビューを担当した。
[直前の日報](2026-10-17-verified-move-daily-report.md)と
[引き継ぎ](../handoffs/2026-10-17-verified-move-handoff.md)から再開した。
開始時のmainは`66fc114e68cb1749ff358fede6cd70ecbe305455`でGitHubと同期・clean、open PRは0。
#243・#257・#259・#261とその後片付けは完了済みで、再実行していない。

## 対象と設計

[Issue #250](https://github.com/hideyukiMORI/NeNeCommander/issues/250)を再開した。
初回事実調査時の「追跡のみ」は履歴として残し、今回の修正・検証範囲をIssueの先頭に追記した。
[ADR-0070](../adr/0070-attempt-scoped-settings-temporary-files.md)が設計判断の正本である。

固定`settings.json.tmp`を残して保存が中断すると、新しいstoreのreadはできても、
次のwriteが`TemporaryArtifactCollision`で失敗することをテストで再現した。
テスト専用`TemporaryClosed`の例外を用いており、実ユーザーの設定や稼働中アプリへの故障注入はしていない。

既存の`ISettingsStore`、complete settings queue、atomic publish、persistent warningを維持し、
書込みごとに`<document>.<Guid:N>.tmp`を一つだけ選ぶ。
識別子はApplicationの`IIdentifierSource`から受け取り、AppがInfrastructureの
`WindowsIdentifierSource`を明示的に組み立てる。専用owner以外の直接生成、method group、
aliasとstatic importをCS-010の限定したpositive/negative proofで拒否する。

不変条件は、元の設定と所有不明のartifactを守ること。
`CreateNew`、flush、handleから得たidentity、exact bytes、承認済みancestor anchor、
publish直前のdocument baseline照合、所有を証明したtmpだけのcleanup、取消境界を維持した。
選んだ候補で衝突した場合の再試行はない。旧固定tmpや別attemptの残留を列挙・読取り・昇格・削除しない。

固定tmpによる別store間の偶発的な直列化はなくなる。
他writerが最終照合前に保存を完了した場合はstale writerを拒否するが、両writerが照合を通過した後の
最後の置換が勝つ競合は残る。cross-process CAS、merge、treeの凍結は保証しない。
この変更点と既存のpath reopen raceをADR-0040/0070とSEC-011に明記した。

## 検証と証拠

ADR-0061に従い、影響するRelease build、設定保存と直接consumer、構成/依存境界、変更fileのformat、
CS-010限定proof、通常Commit hookを選択した。無関係なprovider、schema、UI、command routingは不変で、
full/deep CI、all-layer coverage/mutation、live provider、#94の画面確認は実行対象にしていない。

初回redは対象1件失敗・exit 2で、期待した保存成功に対して
`SettingsWriteRejected / TemporaryArtifactCollisionFailure`だった。
Release locked restoreとbuildはexit 0。正確なargv、当時のtest file SHA、logは
`D:/NeNeCommander/evidence/issue-250/red-reproduction.json`と`red-*.log`に保持する。
当時のtest source/patch自体は別途保存しておらず、後続変更前のraw sourceを収載済みとは主張しない。

実装中の失敗として、interface名`Next`のCA1716を`NextIdentifier`へ変更し、
testのcollection expressionに対するCS9174を明示した配列で修正した。
新規3ファイルのLFに対するformat失敗はCRLFへ直して解消した。抑止や閾値変更はない。

最終的に145件の異なる選択caseが成功した（store 80、直接consumer 63、architecture/lifecycle 2）。
変更のない成功caseは再利用し、後から追加・変更したassertionだけを対象実行した。
App、Infrastructure.Tests、Architecture.TestsのRelease buildは0 warnings/errors。
変更C#ファイルのformatはexit 0だが原因未診断のworkspace load warningを出しており、build結果と区別する。
CS-010限定proofはpositive 1・negative 10の計11 fixture、exit 0。
最終19ファイルの独立した読み取り専用レビューには具体的なP1/P2指摘がない。

正確なargv/filter、caseと最終成功runの対応、ファイルhashは`implementation-inventory.json`、
raw logは同じ恒久証拠ディレクトリに保持する。
CS-010は`tooling/evidence-index.json`、独立レビューは`review/independent-review-snapshot.json`を参照する。
CS-010初回10 fixtureのlogは最終11 fixtureのrunで上書きされ、初回rawは未保存である。
最終11 fixtureのlogと結果は保存済み。未保存の証拠を復元済みとは扱わない。

## 統合と次の作業

最終PR、squash commit、main同期・task/main tree一致、作業物整理の確定結果は
Issue #250のPRと恒久証拠の`completion.json`に記録する。
通常のcommitはrepository hookの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`と
commit-message validatorを通す。結果は`commit.log`と完了記録を参照する。
恒久証拠は`D:/NeNeCommander/evidence/issue-250/`、作業用コピーは
`D:/NeNeCommander/worktrees/issue-250`、一時出力は`D:/NeNeCommander/outputs/issue-250`。
branch、commit、恒久証拠、固定SDKを保持し、取込・未保存物・唯一の成果物・稼働参照・linkを確認して整理する。

次の独立作業は#251の終了契約である。通常close、OS終了、kill/crash、取消、書込失敗の保証を分け、
既存shutdown ownerと実WinUIの最終window寿命を別々に確認する。
P4 #244〜#248、低優先度#181、手動release確認#94は別Issueのまま維持する。
