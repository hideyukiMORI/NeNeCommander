# 引き継ぎ書 2026年10月15日 外部講評の初回事実確認

Status: informational

NeNe Commanderサナから次回担当への引き継ぎ。[日報](../reports/2026-10-15-external-review-daily-report.md)に
初回事実表、P0のred proof、P4の各Issue、hide判断4点、cleanup保留を収載した。
文書Issueは[#242](https://github.com/hideyukiMORI/NeNeCommander/issues/242)、branchは
`docs/242-external-review-facts`、worktreeは`D:/NeNeCommander/worktrees/issue-242`。
開始時mainは`ff528176e49884acf8b74dab43b3e1903aa225e4`でclean。PR #241 / Issue #240は統合済み。
今回の文書PRは製品修正・P0修正PRと分離し、最終PR状態とmerge結果は#242のPRで確認する。

## 再開手順

1. `AGENTS.md`、`docs/PROJECT_STATE.md`、ADR-0061と現在のhide指示を読む。
2. `git status --short --branch`、worktree一覧、#242/#243とopen PRを確認する。古い未完了記述を現在へ引き継がない。
3. 日報の詳細なサブ主張表を読んで、下表の誤検知に新しいguardやrollbackを追加しない。
4. #243の最新test・設計・製品修正・検証状態を確認する。本書のredは修正成功の証拠ではない。
5. 末尾の完了記録を確認する。`outputs/issue-240`はhide実行後に不在確認済みで、削除を再要求しない。

今回hideが明示した役割分担の範囲で調査と文書化を担当した。次回の既定は現在のサナの単体実行であり、
過去のAstra/Sol/Luna分担を無条件に継承しない。追加作業はDドライブのproject領域へ置く。

## 引き継ぐ事実確認表

行は基準mainの位置。全サブ主張の判定・provider別の区別は日報の表を正本として参照する。

| # | 指摘 | 判定 | 根拠（repo内の完全なパス:行） | 直すか・次の扱い |
|---|---|---|---|---|
| P0-1 | composite moveでcopy後の子孫更新が失われ得る | 事実（所有mapped fixtureで2red） | `src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:534-580`、`src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:126-137` | [#243](https://github.com/hideyukiMORI/NeNeCommander/issues/243)。red作成・失敗確認まで。製品修正未完了。 |
| P0-1補足 | verifierはdirectory件数だけ | 誤り | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:216-244` | 名前・型・child lengthの再帰比較もある。既存防御を壊さない。 |
| P0-2 | 固定settings.tmpの残留が次の保存を阻害し、startup回収がない | 事実（crash実験未実施） | `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsStore.cs:22,57-110,247-252,761-768` | [#250](https://github.com/hideyukiMORI/NeNeCommander/issues/250)。回復設計とred test未完了。foreign tmpを削除しない。 |
| P0-2補助確認 | 通常失敗後のowned cleanupと未保存警告がある | 事実 | `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsStore.cs:662-697`、`src/NeNeCommander.Presentation.WinUI/Settings/SettingsPresenter.cs:36-52` | 講評の直接の主張とは分ける。既存の安全なcleanupと警告を保持する。 |
| P1-1 | 通常の自己/子孫copy guardがなく自己再帰する可能性（講評の推測） | 誤り（通常経路は既存guardで反証） | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalFileOperationAdapter.cs:375-377`、`src/NeNeCommander.Infrastructure.Windows/Paths/ProviderPathContainment.cs:13-74` | 講評の留保を保持し、Domainへ重複guardを作らない。物理alias候補は未確認として分離。 |
| P1-2 close | Closing handlerと実行中操作のpre-close確認がない | 事実（狭義） | `src/NeNeCommander.App/CommanderApplication.xaml.cs:91,175-198` | [#251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)。補助確認でClosed後owned drainは存在。実window/OS終了までの完遂は未確認。 |
| P1-2 cancel | I/O層に取消が届かない、Noneで起動するためとの説明 | 一部事実 | `src/NeNeCommander.Infrastructure.Windows/Execution/WindowsLocalIoExecutionBoundary.cs:51-57`、`src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:517-534` | Noneはscheduler token。操作tokenのstep境界観測はあり、開始済みfile/tree stepの内部にtokenがないのは事実。 |
| P1-2 partial | コピー失敗後に不完全targetが残る | 事実 | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:265-267`、`src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:521-551` | ADR-0029の契約。補助確認でPartiallyCompleted通知と元削除停止も存在する。自動rollbackしない。 |
| P2 | 通常/大規模/network性能と選択O(n²) | 未確認（未計測） | `src/NeNeCommander.Application/Panes/PaneReducer.cs:290-307`、`src/NeNeCommander.Presentation.WinUI/Panes/PaneListingPresenter.cs:130-148` | 数値測定なしで改善しない。 |
| P4-1 | record equalityとpath comparerの意味が異なる | 事実 | `src/NeNeCommander.Domain/Paths/WindowsLocalPath.cs:8,25-34`、`src/NeNeCommander.Domain/Paths/FileSystemPathIdentityComparer.cs:17-27` | [#244](https://github.com/hideyukiMORI/NeNeCommander/issues/244)。Issue記録のみ。 |
| P4-2 | COM0/COM¹/CONIN$の予約名扱い | 一部事実 | `src/NeNeCommander.Domain/Paths/FileSystemPath.cs:239-246`、`tests/NeNeCommander.Domain.Tests/FileSystemPathTests.cs:130-140` | [#245](https://github.com/hideyukiMORI/NeNeCommander/issues/245)。COM¹漏れは確認、COM0/CONIN$は未立証。Issueのみ。 |
| P4-3 | 自然順sortでない | 事実 | `src/NeNeCommander.Application/Directories/EntryOrdering.cs:90-94` | [#246](https://github.com/hideyukiMORI/NeNeCommander/issues/246)。hide判断待ち、Issueのみ。 |
| P4-4 | UNC最初のOS待機とtimeoutなし | 一部事実 | `src/NeNeCommander.Infrastructure.Windows/Directories/WindowsDirectoryReadOperation.cs:31-39,89-94`、`src/NeNeCommander.Infrastructure.Windows/Execution/WindowsLocalIoExecutionBoundary.cs:51-57` | [#247](https://github.com/hideyukiMORI/NeNeCommander/issues/247)。provider stepと既存pane Escape abandonを区別。実待ち時間未計測。 |
| P4-5 | bookmarks残余cast | 事実（危険性は限定） | `src/NeNeCommander.Application/Bookmarks/BookmarkEditorSession.cs:298-314`、`src/NeNeCommander.Application/Bookmarks/BookmarkCategoryFilter.cs:3-20` | [#248](https://github.com/hideyukiMORI/NeNeCommander/issues/248)。closed hierarchyの範囲。Issueのみ。 |

## P0の現在地と最小owner

#243 worktreeは`D:/NeNeCommander/worktrees/issue-243`、branchは`fix/243-verified-move-source`。
`tests/NeNeCommander.Infrastructure.Windows.Tests/VerifiedMoveSourceRaceTests.cs:19-47,54-82`の2件は
AfterCopyの同長子孫更新とAfterVerifyの長さが変わる更新を扱い、root identity不変とtarget旧bytesを確認後、
source存在期待で失敗した。0成功・2失敗・skip 0、exit 2。両方のstepは`capability,copy,verify,delete`。
証拠は`D:/NeNeCommander/evidence/issue-243/red-tests.log`と`test-results/verified-move-source-red.trx`。
実9P/live異volumeの証拠ではなく、製品修正も受理も未完了である。

再現のDraft [PR #249](https://github.com/hideyukiMORI/NeNeCommander/pull/249)、test commit
`4bd47d245d70b8d0d1210e6baa6758736ed911d1`とred証拠は設計担当が受理した。これは製品修正受理ではない。
初回調査phaseではP0実装もmergeも行わず、`D:/NeNeCommander/worktrees/issue-243`と
`D:/NeNeCommander/outputs/issue-243`は未完了の再現作業・証拠確認のため保持する。

move方針は既存`FileOperationGateway`、子孫の内容/identity証拠は共有`WindowsLocalTreeCopy`とprovider inspection、
削除admissionはsource providerがowner。再scan中の更新raceも扱うため「小さな再verifyだけで全面安全」としない。
P0-2は既存`WindowsLocalSettingsStore`／ADR-0040内でone-attempt ownershipと回復を設計する。
closed lifecycleは既存App shutdown・`AsyncWorkOwner`・`SettingsSession`を拡張し、終了経路ごとの保証を先に定める。

追跡[#250](https://github.com/hideyukiMORI/NeNeCommander/issues/250)はunique tmp候補と回復設計の記録のみ。
旧.tmpには前processのownership proofがないため無条件startup deleteはしない。
TemporaryClosedにテスト専用例外→新store read/writeを先にred化する案、同時writer proof、
CS-010に従うidentifierのnamed ownerと限定conformance proofを先に検討する。
[#251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)は終了経路ごとのADRとfault proofを追跡するだけで、
新prompt、instant File.Copy/tree取消、partial target rollbackを実装済み・承認済みとしない。

## hide判断に残すもの

| 項目 | 推し・理由 | 根拠 |
|---|---|---|
| full gate自動化 | ADR-0061を維持。bounded変更のfocused proof再利用と、広範影響時の明示dispatchを継続する。 | `docs/adr/0061-impact-scoped-verification.md:28-54` |
| .exe/.lnk確認 | 明示Enter→Shell handoffを維持。確認追加は新挙動なので対象と責務をhideが決めるまで作らない。 | `src/NeNeCommander.Infrastructure.Windows/Launching/WindowsShellFileLauncher.cs:67-78` |
| license | 再利用許諾の方針なら配布前に明示licenseを選ぶことを推す。許諾範囲はhide判断。 | tracked LICENSE/LICENCE/COPYINGなし、`docs/PROJECT_STATE.md:22`にpublic repo記載 |
| 自然順 | 現行を維持し#246で保留。変更希望があれば数字・ゼロ埋め・Unicode・tie-break仕様を別ADRで決める。 | `src/NeNeCommander.Application/Directories/EntryOrdering.cs:90-94` |

この4点について実装・方針変更をしていない。P4も5Issueの作成だけである。

## 証拠の扱いと検証

外部講評の「WSLはプロセス起動なし」はfilesystem操作の範囲に限定する。catalogは
`src/NeNeCommander.Infrastructure.Windows/Wsl/WslDistributionProcess.cs:40,48-49`で`wsl.exe --list --quiet`を起動する。
link拒否はrecursive mutation admissionの範囲でありlistingやShell activationまで一律拒否ではない。
coverage/mutationの基準と現mainの実行済み結果を分け、UIA宣言を実画面成功へ読み替えない。

初回証拠は`D:/NeNeCommander/evidence/external-review-20261015/transfer-facts.md`と
`settings-lifecycle-facts.md`。minor-facts.md保存はpolicy拒否され、同じ保存を再試行していない。
指示書§2-4が求める日報収載として、minor agentの既存報告とrepo根拠を照合して記録した。
#232/#228の関係入力が不変の成功証拠は再利用できるが、新しい回復や実window-close完遂のproofではない。

#242はdocumentation-only。CI/build/製品test/coverage/mutationは開始しない。
既存hookの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`と文書の静的links/encoding/diffだけを確認する。
固定SDKは`D:/NeNeCommander/tools/dotnet-10.0.401`で、当該processだけPATH先頭へ追加する。
結果・PR本文は`D:/NeNeCommander/evidence/issue-242/`。merge後は取り込み・unique/untracked/ignored成果物・
稼働参照・linksを監査して追加worktreeと不要outputを整理する。branch/commit/恒久evidence/SDKは保持する。
共通指示§5の索引・active/planned実態の全量監査や文書再編は未実施で、PROJECT_STATEの現在参照と次作業欄の現在地だけを更新した。

#94はhideがいる日の配布前手動確認、#181は低優先度mapped drivesの別件である。

## 後片付け完了

hideの実行報告を受け、2026-10-15 22:50:56 JSTに読み取り専用の`Test-Path`で
`D:/NeNeCommander/outputs/issue-240`の不在を確認した。親outputs、恒久証拠、固定SDK、
未完了#243のworktreeは存在し、完了した作業の削除待ちはない。
#242のworktree/outputも統合後に整理済み。#243のworktree/outputは未完了のため保持し、
branch・commit・恒久evidenceも削除しない。

以前の自動承認拒否は経緯として残すが、サナは削除を再試行していない。確認証拠は
`D:/NeNeCommander/evidence/cleanup-confirmation-20261015/completion-confirmed.json`。
[Issue #253](https://github.com/hideyukiMORI/NeNeCommander/issues/253)で完了記録を反映し、
次回へ削除依頼を持ち越さない。
