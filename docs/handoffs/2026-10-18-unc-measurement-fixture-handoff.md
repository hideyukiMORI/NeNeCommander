# 引き継ぎ 2026年10月18日 UNC測定fixture

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、ADR-0027/0057/0058/0071。
測定の詳細・限界・コマンドは[日報](../reports/2026-10-18-unc-measurement-fixture-daily-report.md)。
Issue #291は、hideの「進めて」と専用NAS root/停止復旧方法の指定を受けた作業である。
rootの実値は公開記録へ転記しない。

## 用意できたもの

- 既存read/session/lifecycle public APIを組み合わせたheadless fixture。製品変更、別の列挙経路、依存追加はない。
- 明示barrierを使った通信なしの7ケースが成功。初版の入口10ケース、専用root明示確認へ変更した最終12ケースも期待通り。
- final Release buildは0 warnings/errors。live入力受付だけの変更なので、offlineの7ケースは入力不変の証拠を再利用した。
- source15ファイル、binary19ファイル、hash、初版の保存、log/result/exitとREADMEを恒久収載。
- 指定NASの各1 sampleで、正常readは19.214 ms、切断readは67.011秒後にProviderUnavailable。
- Escape後は3.977 msでcallerが戻り、providerを残したままlocal navigationが成功。元のproviderは40.859秒後にCancelled。
- 未放棄owner-closeは41.342秒待機し、provider failure→caller→drain→callbackの順。放棄後のcallbackは1.396 msで、元のproviderは残る。
- 5ケースの観測器はexit 0で退出済み。差し戻し後の3回の復旧readが成功し、最終確認は14:21:05 JST。測定の完走とSMB readの成功/失敗を混同しない。

## 維持する境界

fixtureは直下32 entryまでの読み取り専用。Windowsの既存logon sessionを使い、資格情報やhost設定を変更しない。
測定configのdedicatedRootConfirmedとnetwork opt-inは、新たな接続先への許可を代行しない。
未指定のNASへ探査を広げず、結果にUNC、entry名、例外messageを出さない。

Escape intentとowner-closeはpublic APIであり、実キー入力/system close/Window.Closedではない。
worker/native開始は未観測。headless fixtureはowner-close後もproviderをawaitするので、
放棄providerが残ったまま製品processが退出する証拠は別途必要。
120秒は観測窓で、provider timeoutではない。未完了と完了、typed failureと取消を区別する。
少数sampleの時間を一般化しない。親#247はopenで、timeout採用やowner変更は未決定。

## 統合と配置

baseline `4cccad04eff1c1ed477022290f60d7a0a7c787d7`。
恒久保存先は`D:/NeNeCommander/evidence/issue-247/fixture-preparation-20261018/`。
最終状態は`completion.json`とIssue PRを読む。通常Commit hook以外の製品gate/CIは起動しない。
repo変更は4文書。TST-011では実施した限定環境観測と、canonical live UNC gate非搭載を区別する。
worktree `D:/NeNeCommander/worktrees/issue-291`、output `D:/NeNeCommander/outputs/issue-291`は
統合・収載と稼働参照/未保存物/link確認を経て、下記のとおり整理済み。sourceのprojectにある既定参照先は履歴となり、
再buildはREADMEのRepositoryRoot指定を使う。保持したbinaryからの起動には元worktreeは不要。
branch/commit/恒久証拠/共有SDKは保持する。#246/#94は別件として残す。

## 実際の統合と整理完了

#291 / PR #292は2026-10-18 14:24:07 JSTにsquash mergeし、mainを
`8eb48350615f28bf65612b9cd960b597ac9ccd0a`へ同期、clean/tree一致を確認した。
task worktreeは削除済み。fixtureのsource/binary/結果と各版のhashは恒久収載済みで、
稼働process/module参照とlinkは観測されなかった。通常Commitの112 rules / 24 adversarial登録確認も通過。

outputのdirectory-level削除だけが自動承認レビューの`blocked by policy`で実行前に拒否された。
サナは別手段で再試行していない。Issue #293 / PR #294は拒否時点の状態を記録した履歴。
その後、hideが2026-10-18 14:31:05 JSTにprepared scriptを実行した。
14:31:24 JSTに`cleanup-executed-by-hide.json`と対象absenceを読み取り確認し、
output/worktreeの両方が存在しないことと、恒久source/evidence/SDKの保持を確認した。
`cleanup-confirmation.json`とroot `completion.json`が現在の完了状態を示し、`cleanup-pending.json`は履歴として残す。

後片付け待ちはない。以下は実行済みコマンドで、再実行は不要。

Issue #295で既存3文書を完了へ更新し、新しいworktree/outputは作らない。
文書/link/diff/通常Commit hookだけを確認し、製品test/CI/NAS測定は再実行しない。
文書closureの最終状態はIssue PRと`cleanup-closure/completion.json`に記録する。
NASは復旧済み。共有SDK、branch/commit、恒久source/evidenceは保持する。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-247/fixture-preparation-20261018/cleanup-completed-output.ps1
```
