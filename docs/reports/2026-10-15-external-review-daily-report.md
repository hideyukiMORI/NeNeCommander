# 日報 2026年10月15日 外部講評の初回事実確認

Status: informational

NeNe Commanderサナが、hideの「まず事実確認の表から」という指示に従い、外部講評をmain
`ff528176e49884acf8b74dab43b3e1903aa225e4`のコード・既存test・ADRと照合した。
別アカウントClaudeの講評はWeb経由の要約によるもので、講評者によるbuild・実行はない。
講評の【確認】を実証済みと読み替えず、事実・誤り・一部事実・未確認を分けた。
指定指示書の共通指示§1〜3を先に適用した。外部サービスへrepo内容を送っていない。

この[Issue #242](https://github.com/hideyukiMORI/NeNeCommander/issues/242)は、日報・
[引き継ぎ書](../handoffs/2026-10-15-external-review-handoff.md)とPROJECT_STATEの現在参照だけを扱う。
製品修正とは別PRである。既に統合済みのUIA対応は[前の日報](2026-10-15-ui-automation-daily-report.md)と
[PR #241](https://github.com/hideyukiMORI/NeNeCommander/pull/241)を参照する。

## 事実確認表

根拠の行は、別記した#243の未統合testを除き上記mainの位置を指す。
「直す」は今回の文書PRで製品を変更した意味ではない。P0/P1は事実確認と再現testを先に行い、1修正1PRとする。

| # | 指摘・サブ主張 | 判定 | 根拠（repo内の完全なパス:行） | 直すか・次の扱い |
|---|---|---|---|---|
| P0-1a | 非atomic moveはcopy→verify→元の永久削除 | 事実 | `src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:554-580` | 既存方式。recycleへの変更をこの講評だけで決めない。同一mounted volumeのatomic moveは別経路。 |
| P0-1b | file検証は型・非link・lengthを見て、bytes/mtimeを比較しない | 事実 | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:126-137` | 子孫更新の喪失を再現してから既存ownerで対処する。 |
| P0-1c | directory検証は中身の「数だけ」 | 誤り | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:216-244` | 件数に加えtyped child名、型、各file lengthを再帰比較する。この誤認を根拠に別verifierを追加しない。 |
| P0-1d | root identityが不変のままcopy後に子孫を同長更新すると更新済sourceが消える | 事実（所有mapped fixtureで再現） | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalFileOperationAdapter.cs:531-558`、`src/NeNeCommander.Infrastructure.Windows/FileOperations/WslFileOperationAdapter.cs:316-328`。#243の`tests/NeNeCommander.Infrastructure.Windows.Tests/VerifiedMoveSourceRaceTests.cs:19-47` | [#243](https://github.com/hideyukiMORI/NeNeCommander/issues/243)でred test開始・失敗確認済み。製品修正は未完了。live 9P/異volumeの実証ではない。 |
| P0-1e | verify後・delete前の子孫更新もcopied-stateと比較されない | 事実（所有mapped fixtureで再現） | `src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:534-575`。#243の`tests/NeNeCommander.Infrastructure.Windows.Tests/VerifiedMoveSourceRaceTests.cs:54-82` | 長さが変わる更新でも再現。再verifyだけではscan中更新raceが残るため、全面安全・小修正と断言しない。 |
| P0-1f | 全providerのmoveが同じ経路に到達する | 誤り（補助確認） | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalFileOperationAdapter.cs:189-201`、`src/NeNeCommander.Infrastructure.Windows/FileOperations/TransferRoute.cs:38-46` | localのatomic capabilityとUNC mutation refusalを区別する。講評自身が全moveと述べたわけではない。 |
| P0-2a | settingsの一時名は固定`.tmp`で、既存なら衝突して保存拒否 | 事実 | `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsStore.cs:22,247-252,761-768` | availability/recoveryの課題。既存のforeign artifactを無条件に消さない。 |
| P0-2b | startup/readで古いtmpを回収しない | 事実 | `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsStore.cs:57-110`、`src/NeNeCommander.App/CommanderApplication.xaml.cs:84` | [#250](https://github.com/hideyukiMORI/NeNeCommander/issues/250)で追跡。ADR-0040と整合する回復設計とred testが必要。今回未実装。 |
| P0-2c | crash残留が次の保存を継続して妨げ得る | 事実（静的経路、crash実験は未実施） | `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsStore.cs:263,909-921`、`tests/NeNeCommander.Infrastructure.Windows.Tests/WindowsLocalSettingsStoreTests.cs:1013` | 新しい選択の永続化阻害を確認。古い設定本体が壊れることや実crashからの回復成功は立証していない。 |
| P0-2d（補助確認） | 通常失敗後のowned cleanupと保存失敗の通知が存在する | 事実 | `src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsStore.cs:662-697`、`src/NeNeCommander.Application/Settings/SettingsSession.cs:355-368`、`src/NeNeCommander.Presentation.WinUI/Settings/SettingsPresenter.cs:36-52` | 講評の直接の主張ではなく、回復を設計する前の追加確認。identity/bytes/parentが合う場合だけcleanupし、不確実ならtyped residue。現windowに未保存警告を出す。 |
| P1-1a | 通常のsource自身/子孫destinationを検査していない | 誤り | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalFileOperationAdapter.cs:375-377`、`src/NeNeCommander.Infrastructure.Windows/FileOperations/WslFileOperationAdapter.cs:207-210`、`src/NeNeCommander.Infrastructure.Windows/Paths/ProviderPathContainment.cs:13-74` | 既存preflightがeffect前にConflict。Domainへ重複guardを追加しない。 |
| P1-1b | target directory作成後にsource childrenを列挙する | 事実 | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:171-187` | 通常のtyped pathでは先行containmentが拒否するため、この順序だけで無限copyは証明できない。 |
| P1-1c | 通常のtyped pathで自己再帰copyする可能性（講評の推測） | 誤り（通常経路は既存guardで反証） | `tests/NeNeCommander.Infrastructure.Windows.Tests/WindowsLocalFileOperationAdapterTests.cs:121`、`tests/NeNeCommander.Infrastructure.Windows.Tests/WslFileOperationAdapterTests.cs:423,568` | 講評は可能性と留保している。既存の反証testを保持し、物理aliasの未確認範囲は次行に分ける。危険な実機copyは行わない。 |
| P1-1d | junction祖先などの物理aliasも全て防げる | 未確認（補助確認） | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalFileOperationAdapter.cs:354-361`、`src/NeNeCommander.Infrastructure.Windows/Paths/ProviderPathContainment.cs:13-74` | 現比較はprovider/segment。alias候補は通常経路と分離し、未再現を事実に格上げしない。 |
| P1-2a | Closing handlerと実行中操作のpre-close確認がない | 事実（狭義） | `src/NeNeCommander.App/CommanderApplication.xaml.cs:91,175-198` | ClosingではなくClosedでowned shutdownを開始する。確認を増やす前に終わり方のcontractを決める。 |
| P1-2b（補助確認） | Closed後のowned cancel/await/settings drainが存在する | 事実 | `src/NeNeCommander.App/Views/CommanderWindow.xaml.cs:788-791`、`src/NeNeCommander.Presentation.WinUI/Lifecycle/AsyncWorkOwner.cs:163-190`、`src/NeNeCommander.Application/Settings/SettingsSession.cs:278-300` | 講評のClosing記述から終了処理全体の欠如を導かず、既存ownerを拡張する。shutdown経路を二重に作らない。 |
| P1-2c（補助確認） | 実window/OS終了時に既存drainが完遂するか | 未確認 | `src/NeNeCommander.App/CommanderApplication.xaml.cs:175-198`、`tests/NeNeCommander.Architecture.Tests/AsyncLifecycleOwnershipTests.cs:18-28` | [#251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)で追跡。講評者のassertではなく追加の証拠確認。neutral ownerと実process寿命を分ける。 |
| P1-2d | I/O層に取消が届かない、Noneで起動するためとの説明 | 一部事実 | `src/NeNeCommander.Infrastructure.Windows/Execution/WindowsLocalIoExecutionBoundary.cs:51-57`、`src/NeNeCommander.App/Views/CommanderWindow.xaml.cs:692-693`、`src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:517-534` | Noneはscheduler tokenで原因説明が異なる。操作tokenとgatewayのstep前後の観測は存在し、copy内部にtokenがない事実は次行で確認する。 |
| P1-2e | 開始済み大fileやdirectory tree copyを途中中断できない | 事実 | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalFileOperationAdapter.cs:73-82`、`src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:156-214` | 同期provider stepの既定制約。細分化するなら同じtree engine・gatewayのeffects/取消contractを先に定める。 |
| P1-2f | コピー失敗後の不完全targetが残る | 事実・意図した契約 | `src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalTreeCopy.cs:265-267`、`src/NeNeCommander.Application/FileOperations/FileOperationGateway.cs:521-551` | ADR-0029。PartiallyCompletedとして通知し、verify/次source/move元削除を止める。自動rollback削除は行わない。 |
| P2 | 通常規模、低速network/大file、1万件を1件ずつ選ぶO(n²)の性能評 | 未確認（未計測） | `src/NeNeCommander.Application/Directories/DirectoryListing.cs:15`、`src/NeNeCommander.Application/Panes/PaneReducer.cs:290-307`、`src/NeNeCommander.Presentation.WinUI/Panes/PaneListingPresenter.cs:130-148` | 上限やloopの存在は性能測定ではない。性能改善を今回始めない。 |

## P0-1のred proofと未完了範囲

初回の静的調査は条件付きの子孫更新喪失経路を示した。その後、別の[#243](https://github.com/hideyukiMORI/NeNeCommander/issues/243)
`fix/243-verified-move-source`で所有mapped fixtureを使う2件を先に追加し、両方が期待通りredとなった。
root identity不変とtargetの旧bytesのassertを通過した後、更新済sourceの存在期待で失敗し、
実行stepは両方とも`capability,copy,verify,delete`だった。結果は0成功・2失敗・skip 0、exit 2。
これは修正前の再現証拠であり、passing evidenceや製品修正完了ではない。

再現はDraft [PR #249](https://github.com/hideyukiMORI/NeNeCommander/pull/249)、test commit
`4bd47d245d70b8d0d1210e6baa6758736ed911d1`で設計担当が読んで受理した。修正済みPRには数えず、
この初回事実確認phaseでは製品実装もmainへのmergeも行っていない。#243 worktree/outputは未完了の再現作業として保持する。

証拠は`D:/NeNeCommander/evidence/issue-243/red-tests.log`と
`D:/NeNeCommander/evidence/issue-243/test-results/verified-move-source-red.trx`。
test sourceは#243 worktreeの`tests/NeNeCommander.Infrastructure.Windows.Tests/VerifiedMoveSourceRaceTests.cs`で、
この文書PRにもmainにも含まれない。実9P、live異volume、ユーザーデータで試した証拠ではない。
子孫snapshotをどこから削除admissionまで運ぶかは既存gateway・共通tree engine・source provider境界で設計する。
サイズ/mtimeの再scanだけで競合全体が消える、またはrecycleへ変えれば全providerで解決するとは主張しない。
P0-2のcrash/restart再現・回復設計は未完了。P1-1の通常経路は誤検知として修正しない。

追加の追跡は[#250](https://github.com/hideyukiMORI/NeNeCommander/issues/250)と
[#251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)。作成時点では実装・testを開始していない。
#250はunique候補によって旧残留が新保存を妨げない案を記録し、ownershipのない旧tmpは保持する。
startup回収の条件はADR-0040/SEC-011のCreateNew・Flush・identity/exact bytes・ancestor guardを維持して設計する。
TemporaryClosedへのテスト専用例外→新storeでread/writeを先にred化する案と、同時writer proofを記録した。
`Guid.NewGuid`の直接使用はCS-010に反するためnamed ownerと限定されたconformance proofが必要である。
#251はuser close、OS session end、kill/crash、render lost、cancel、write failureごとに保証・非保証と
故障注入最低1件を先に記述する。新promptや取消粒度の変更は製品仕様の判断が必要で、今回追加しない。

## P4はIssueを作るだけ

作成前に全111既存Issueのタイトルを確認し、同じ未完了Issueがないことを確認した。
#184のpane read abandonと、下表のUNC provider worker制約は区別した。P4の実装・test・CIは開始しない。

| # | 指摘 | 判定 | 根拠（repo内の完全なパス:行） | 記録先・直すか |
|---|---|---|---|---|
| P4-1 | record equalityとfilesystem identity comparerが一致しない | 事実 | `src/NeNeCommander.Domain/Paths/WindowsLocalPath.cs:8,25-34`、`src/NeNeCommander.Domain/Paths/FileSystemPathIdentityComparer.cs:17-27` | [#244](https://github.com/hideyukiMORI/NeNeCommander/issues/244)。誤用箇所・実害は未立証。Issueのみ。 |
| P4-2 | COM0・COM¹・CONIN$が予約名判定から漏れる | 一部事実 | `src/NeNeCommander.Domain/Paths/FileSystemPath.cs:239-246`、`tests/NeNeCommander.Domain.Tests/FileSystemPathTests.cs:130-140` | [#245](https://github.com/hideyukiMORI/NeNeCommander/issues/245)。COM¹の漏れを確認。COM0/CONIN$を予約basenameとして拒否すべき主張は未立証。既存COM0受理testを勝手に変更しない。 |
| P4-3 | file10/file2を数値で比べる自然順ではない | 事実 | `src/NeNeCommander.Application/Directories/EntryOrdering.cs:90-94`、`docs/adr/0053-pane-sort-order.md:24-30` | [#246](https://github.com/hideyukiMORI/NeNeCommander/issues/246)。仕様判断待ち、Issueのみ。 |
| P4-4 | UNC最初の同期列挙にアプリ独自timeoutがない | 一部事実 | `src/NeNeCommander.Infrastructure.Windows/Directories/WindowsUncDirectoryReader.cs:35-47`、`src/NeNeCommander.Infrastructure.Windows/Directories/WindowsDirectoryReadOperation.cs:31-39,89-94` | [#247](https://github.com/hideyukiMORI/NeNeCommander/issues/247)。worker中断保証なしは事実、実待ち時間は未計測。pane側にはEscape abandonがある。 |
| P4-5 | BookmarkEditorSessionに残余の明示castがある | 事実（危険性は限定） | `src/NeNeCommander.Application/Bookmarks/BookmarkEditorSession.cs:298-314`、`src/NeNeCommander.Application/Bookmarks/BookmarkCategoryFilter.cs:3-20` | [#248](https://github.com/hideyukiMORI/NeNeCommander/issues/248)。closed hierarchy上のcastであり、任意外部型による脆弱性と扱わない。Issueのみ。 |

予約名の一次資料はMicrosoftの[Naming Files, Paths, and Namespaces](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file)。
上付き数字の予約名説明を確認した。device namespaceの説明を通常basenameの禁止一覧と同一視しない。

## hideの判断に残す4項目

| 項目 | 現状・根拠 | 推しと理由 | 今回の扱い |
|---|---|---|---|
| full gateの自動化 | `docs/adr/0061-impact-scoped-verification.md:28-54`、`.github/workflows/quality.yml:3-4`、`.github/workflows/security-deep-review.yml:3-4` | 現行ADR-0061のimpact scopeを維持する。bounded変更のfocused evidenceを再利用し、広範影響時だけdispatchする方がhideの費用/範囲指定に合う。 | 方針変更なし。自動化の変更はhide判断と別ADR。 |
| .exe/.lnkのEnter確認 | `src/NeNeCommander.Application/Panes/PaneSession.cs:186-203`、`src/NeNeCommander.Infrastructure.Windows/Launching/WindowsShellFileLauncher.cs:67-78` | 明示的Enterから既存Shell handoffを維持する。確認追加は新しい挙動であり、対象拡張子・責務を先に決める必要がある。 | 確認dialogを追加しない。 |
| license | `git ls-files '*LICENSE*' '*LICENCE*' '*COPYING*'`で該当tracked fileなし。public repoは`docs/PROJECT_STATE.md:22` | 再利用を許諾する方針なら配布前に明示licenseを選ぶことを推す。許諾範囲と条件はhideが決め、publicであるだけで暗黙許諾としない。 | licenseを選択・追加しない。 |
| 自然順ソート | `src/NeNeCommander.Application/Directories/EntryOrdering.cs:90-94`、`docs/adr/0053-pane-sort-order.md:24-30` | 現行を維持し[#246](https://github.com/hideyukiMORI/NeNeCommander/issues/246)で保留。変更希望があれば数字・ゼロ埋め・Unicodeとtie-breakの仕様を独立して決める。 | 実装しない。 |

## 称賛された点の適用範囲

- WSLのfilesystem read/mutationはtyped pathと9P/no-follow境界を使い、shell mutationへfallbackしない。
  一方、distribution catalogは`src/NeNeCommander.Infrastructure.Windows/Wsl/WslDistributionProcess.cs:40,48-49`で
  `wsl.exe --list --quiet`を起動する。「WSLはプロセスを一切起動しない」とは言えない。
- links/reparse拒否はrecursive mutation admissionの範囲で読む。listingがlinkの存在を示すことや、
  Shell activationのpath handoffまで全操作一律拒否する主張ではない。後者は
  `src/NeNeCommander.Infrastructure.Windows/Launching/WindowsShellFileLauncher.cs:67-78`とADR-0046/0064/0065の別契約。
- coverage/mutationの100/90・95/90という基準と、現mainで全tierを新しく実行した結果は区別する。
  今回は全件成功を主張せず、既存evidenceの関係入力が不変の範囲だけ再利用する。
  UIAの宣言・source/build proofも実UIA hierarchyや#94手動表示確認の成功へ読み替えない。

## 証拠と今回の検証

初回の読み取り証拠は`D:/NeNeCommander/evidence/external-review-20261015/transfer-facts.md`と
`settings-lifecycle-facts.md`。後者の保存は成功した。minor調査の`minor-facts.md`保存は実行policyに
`blocked by policy`で拒否され、同じ保存を別経路で再試行していない。指示書§2-4が求める日報収載として、
既存報告とrepo根拠を記録した。minor調査の既存報告を受領し、該当コードと予約名の一次資料を照合した。

#232のsettings/collision/drain/warning、#228のgateway/partial effect等の既存成功証拠は、関係入力が
不変である範囲の契約証拠として再利用できる。新しいrecoveryや実window-close完遂は証明しない。
P0-1の2redは別Issueの新しい失敗証拠であり、以前の正常系成功を無効な成功へ読み替えて隠さない。

この文書PRはADR-0061のdocumentation-only scope。CI、build、製品test、coverage、mutationを開始しない。
文書の静的リンク・参照パス/行・BOMなしUTF-8/CRLF・diffと、既存hookのCommit modeだけを確認する。
正確なcommands/resultsとPRは`D:/NeNeCommander/evidence/issue-242/`へ保存し、mergeは設計担当が行う。
共通指示§5の索引・active/planned実態の全量監査や文書再編は今回行わず、PROJECT_STATEの現在参照だけを更新した。

## ! hide実行待ち：後片付け

現在の削除待ちは`D:/NeNeCommander/outputs/issue-240`のみ。残りはSDK zipと中間テキストで、
固有成果物・ログ・hash・TRX・reviewは`D:/NeNeCommander/evidence/issue-240/`へ収載済み。
過去の他のcleanupは完了している。固定SDK `D:/NeNeCommander/tools/dotnet-10.0.401`、
branch、commit、恒久evidence、進行中#242/#243のworktreeはこの削除対象に含まない。

自動承認レビューがこのディレクトリのrecursive削除を`blocked by policy`で拒否し、詳細理由は返していない。
削除は未実行で、別方式の再試行もしない。hideの実行結果または読み取り専用の不在確認まで保留を引き継ぐ。
今回はこの対象を削除していない。hide用の実行一行は次のとおり。

```powershell
if (Test-Path -LiteralPath 'D:\NeNeCommander\outputs\issue-240') { Remove-Item -LiteralPath 'D:\NeNeCommander\outputs\issue-240' -Recurse -Force }
```
