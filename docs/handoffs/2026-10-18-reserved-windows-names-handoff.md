# 引き継ぎ 2026年10月18日 Windows予約名の上付き数字

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、[FS-001](../FILESYSTEM_BOUNDARIES.md#fs-001--normalize-without-changing-identity)、
[日報](../reports/2026-10-18-reserved-windows-names-daily-report.md)。
hideの「対応を進めて」により[Issue #245](https://github.com/hideyukiMORI/NeNeCommander/issues/245)を実装修正へ進めた。
以前のP4「記録のみ」は当時のphaseであり、本件の現行状態はPRと完了記録から読む。

## 維持する契約

- `FileSystemPath.IsReservedWindowsName`だけに6つのCOM/LPT上付き¹²³を追加した。製品差分は1行。
- Windows local/UNCのchild segmentはcase・拡張子・ancestor位置によらず拒否する。
  UNC server/share、WSL distributionのroot規則は変更しない。
- WSL Parse/Childは名前とLinuxPathを保持する。同じWindows predicateが`HasWindowsCompatibleNames`を非互換にし、
  既存Shell admissionとWindows宛transferのtyped child/preflightを通じて拒否する。
- 一般的なUnicode数字への拡張や名前の正規化はない。既存COM0/LPT0は変えず、CONIN$は未立証事項として残す。
- 既存の命名契約を補完する修正で、新ADR・第二のvalidator・新しいAPIやprovider能力は追加していない。

## 検証の読み方

Domain 132、Application 21、Infrastructure 29の182 distinct caseが成功し、失敗・skipは0。
Infrastructureは27件と重複しない既存対照2件である。3対象Release buildは警告・エラー0、
変更9 C# fileのformatはexit 0・警告出力なし。
影響するparser/Child/WSL互換性、名前要求、Shell、listing、transfer preflightだけを実行した。
全件test・full/deep CI・coverage/mutation・live provider/UI・#94は未実行で、成功とは扱わない。
成功済みの証拠は関連入力が変わらない限り、文書更新・push・mergeのために再実行しない。

元productionのREDは133件中24失敗・109成功、exit 2。Parse 12、Child 6、WSL互換性6の反例である。
当時のsource実体・patch・SHA・argv・rawを`red/`へ保持する。
RED時にあったCONIN$成功rowは未立証の契約を固定しないため除き、最終Domainは132件となった。
初回修正buildのIDE0048はpattern括弧で解消し、失敗原本は`green-build-failed-ide0048/`に残す。
UNCはinjected enumerator、WSL転送は所有NTFS fixtureであり、実share/9Pの証明ではない。

恒久証拠は`D:/NeNeCommander/evidence/issue-245/`。
`implementation-inventory.json`、stage別command JSON/raw log、`final-source/`、
`final-source-hashes.json`、`implementation.patch`を日報の件数・commandと照合する。
独立レビューは最終9 sourceのhash/patch、REDとbaseline、filterと182件の対応を照合し、具体的P1/P2なし。
通常Commit hookは112規約・24security登録とcommit-message検査が成功し、`root/commit.log`に保持する。

## 再開時の確認

1. AGENTS.md、PROJECT_STATE、現在のhide指示、実際のIssue/PRとmainを確認する。
2. [PR #278](https://github.com/hideyukiMORI/NeNeCommander/pull/278)は12:32:52 JSTに
   `18a694571e6ea7eaa5b58b2156aae8bc6e5a3b44`へsquash統合済み。main同期・cleanと受理taskの同一treeを確認した。
   Issue #245はclosed。`D:/NeNeCommander/evidence/issue-245/completion.json`で統合と整理を分けて読む。
   古いP4記録から未着手に戻さない。
3. 作業用pathは`D:/NeNeCommander/worktrees/issue-245`、一時出力は`D:/NeNeCommander/outputs/issue-245`。
   branch `fix/245-windows-reserved-superscripts`・commit・証拠・共有SDKは保持する。
   worktreeは削除済み。outputは自動承認拒否により残っているため、末尾の準備済みcommandを引き継ぐ。
   完了記録で削除が確認されたpathへ同じ削除を再依頼しない。
4. P4 #244・#246・#247・#248は別件。#94はhide在席時の手動release確認で、開発阻害ではない。
   実share/mapping/removableの環境証明も本件から成功扱いしない。

検証用SDKは`D:/NeNeCommander/tools/dotnet-10.0.401`。必要なprocessにだけ指定し、system SDKやglobal PATHは変更しない。

## ! hide実行待ち：後片付け

対象は`D:/NeNeCommander/outputs/issue-245`だけ。
所有出力・取込・未保存物・稼働参照・linkを確認し、本文2 fileも恒久証拠へ収載した後の削除を、
自動承認レビューが`blocked by policy`で拒否した。commandは開始されず、サナによる別手段の再試行はない。
12:35:35 JSTの読み取り確認では対象出力が残り、Issue #245のworktreeは不在だった。
恒久証拠`D:/NeNeCommander/evidence/issue-245/`、branch・commit・共有SDKを保持する。
Issue #279がこの保留記録を扱う。hideの実行と完了確認までは引き継ぎから消さない。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-245/cleanup-completed-output.ps1
```
