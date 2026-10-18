# 日報 2026年10月18日 UNC待機の調査と測定計画

Status: informational

Issue [#289](https://github.com/hideyukiMORI/NeNeCommander/issues/289)は、hideの「推しの進め方で」に基づく
[#247](https://github.com/hideyukiMORI/NeNeCommander/issues/247)の調査部分である。
baselineは`6708712a7c847d4d188d73cb9a4c1c666ae8be39`。コードと既存テストを読み、測定条件を具体化した。
live SMBの待ち時間・発生頻度・実行中worker数は未計測。timeoutの採用や製品動作の変更は決めていない。
調査Issueだけを完了し、親Issue #247は実測と判断が残るためopenを維持する。

## 確認できたこと

| 境界 | baselineの根拠 | 読み取れることと限界 |
|---|---|---|
| 読み込み | [WindowsUncDirectoryReader](../../src/NeNeCommander.Infrastructure.Windows/Directories/WindowsUncDirectoryReader.cs)のReadAsync、[共有operation](../../src/NeNeCommander.Infrastructure.Windows/Directories/WindowsDirectoryReadOperation.cs)のRead/Enumerate | UNCを既存routerから共有同期列挙へ渡す。tokenは列挙開始前、各entry、列挙終了後に観測する。呼出中のOS待機をtokenで割り込ませる経路はない。 |
| 同期列挙の開始 | [WindowsDirectoryEnumerator](../../src/NeNeCommander.Infrastructure.Windows/Directories/WindowsDirectoryEnumerator.cs)のEnumerate | 外側iteratorの最初のMoveNextには、BCL列挙の生成・開始・最初のentry取得が含まれる。どのnative呼出で待つかは未観測で、後続MoveNextも待たないとは保証しない。 |
| 実行場所 | [WindowsLocalIoExecutionBoundary](../../src/NeNeCommander.Infrastructure.Windows/Execution/WindowsLocalIoExecutionBoundary.cs)のDefaultWindowsLocalIoScheduler | TaskScheduler.Defaultへ一度scheduleする。scheduler-level cancellation、独自timeout、UNC read専用の同時実行数制限はない。共有boundaryはmutationにも使われる。 |
| Escape | [PaneSession](../../src/NeNeCommander.Application/Panes/PaneSession.cs)のAbandonRead/NavigateAsync、[PaneRead](../../src/NeNeCommander.Application/Panes/PaneRead.cs)のAbandon/Release | navigationをsupersedeし、以前のcontentを保ったPaneReadAbandonedを返してcallerを解放する。provider tokenをcancelしてもprovider taskは残り得る。完了callbackがtoken sourceをdisposeし、放棄後のfaultは次のpane入口で再送出する。 |
| 通常終了 | [AsyncWorkOwner](../../src/NeNeCommander.Presentation.WinUI/Lifecycle/AsyncWorkOwner.cs)のStopAsync、[WindowShutdownOwner](../../src/NeNeCommander.Presentation.WinUI/Lifecycle/WindowShutdownOwner.cs)、[CommanderWindow](../../src/NeNeCommander.App/Views/CommanderWindow.xaml.cs)のStopAsync、[App](../../src/NeNeCommander.App/CommanderApplication.xaml.cs)のShutdownAsync | 未放棄readはhost workに含まれ、cancel後も完了を待つ。Escapeでcallerが完了済みのreadは再登録されず通常終了のdrain対象外。閉じ始めると新しいEscapeも受理しない。 |

最後の違いは[ADR-0071](../adr/0071-drain-before-normal-window-close.md)で既に明記されている契約である。
「閉じると常に全UNC workerを待つ」「EscapeがOS呼出を停止する」のどちらも正しくない。
通常終了後も放棄providerの完了callbackが必ず実行される、というプロセス寿命の保証はこの契約にない。
またcancel後にOSエラーが戻れば、共有operationはtyped failureを返し得る。cancelを送っただけで
DirectoryReadCancelledになったと記録せず、実際のoutcomeとpane側の結果破棄を分ける。

## 既存証拠の範囲

- [UNC reader tests](../../tests/NeNeCommander.Infrastructure.Windows.Tests/WindowsUncDirectoryReaderTests.cs)の
  `ReadAsyncWhenCancelledBeforeOrBetweenEntriesReturnsCancelled`は注入enumeratorを使う。
  HRESULT表もscripted failureであり、実SMBの待ち時間ではない。
- [execution boundary test](../../tests/NeNeCommander.Infrastructure.Windows.Tests/WindowsLocalIoExecutionBoundaryTests.cs)は
  manual schedulerでcallerからの分離を確認する構造で、thread poolの実負荷測定ではない。
- [pane abandonment tests](../../tests/NeNeCommander.Application.Tests/PaneSessionAbandonmentTests.cs)の
  `NavigateAsyncWhenReadIsAbandonedReturnsBeforeProviderCompletes`と遅延結果/faultのケースは、
  providerを未完了に保ったままcallerが戻る契約を対象にしている。
- [AsyncWorkOwner tests](../../tests/NeNeCommander.Presentation.WinUI.Tests/AsyncWorkOwnerTests.cs)と
  [WindowShutdownOwner tests](../../tests/NeNeCommander.Presentation.WinUI.Tests/WindowShutdownOwnerTests.cs)は
  terminal Stop、work/interruptのdrain、drain完了前のclose拒否を個別に対象とする。
  ADR-0071の過去のWinUI記録も実SMB待機を組み合わせた証拠ではない。

今回はこれらのsourceを静的確認した。新しいテスト実行成功やlive証拠として数えない。
実測で必要なのは、実UNC providerとこれらのownerを組み合わせたときの時間と残存状態である。

## 外部APIから確認した制約

Microsoftの[EnumerateFileSystemInfos](https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefilesysteminfos?view=net-10.0)
は同期IEnumerableを返し、使用中overloadにCancellationTokenやtimeout引数はない。
これと現在の呼出コードから、tokenの通知だけで実行中の列挙呼出が中断されるとは扱わない。

[CancelSynchronousIo](https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-cancelsynchronousio)
はWindowsに存在し、thread handleを対象に取消を要求するが、完了を待つAPIではない。
Microsoftの[取消時の注意](https://learn.microsoft.com/en-us/windows/win32/fileio/canceling-pending-i-o-operations)
はdriver側対応の限界と、共有thread pool上で別のI/Oを取り消すraceを明記している。
したがって「Windowsでは同期I/O取消が一切不可能」とは結論しない。
現行schedulerへこのAPIを足すだけでは所有対象を保証できず、導入には実測と別の設計判断が必要である。
いずれも2026-10-18参照。OSの一定timeout値や端末での実効時間をこれらの文書から推定しない。

## 実測計画

### 実行前に固定する入力

1. 許可された専用SMB server/shareの配下に、既知の少量entryだけを持つread-only測定rootを用意する。
   実際の対象root、所有者、アクセスするテストidentityを実行前に指定する。
   Windowsがそのidentityを送る相手を限定し、任意の存在しないserver名や外部IPを探査しない。
2. 同じendpointに対して正常状態と、専用ラボ側で応答を止めて復旧できる状態を用意する。
   DNS不明、接続拒否、応答なしを混ぜず、初回は正常と応答なしの2条件に限定する。
   host全体のfirewall/SMB timeout/資格情報/mappingを変更して条件を作らない。
3. client OS build、runtime、製品commit、fixture hash、SMB dialect（取得できた場合）、
   sessionが既存か初回か、entry boundary、条件切替と復旧手順を記録する。
   既存session/cacheを勝手に破棄せず、「初回測定」をcold接続と同一視しない。
4. fixtureは既存read port/router/providerを使い、共有の列挙algorithmを再実装しない。
   pane/host部分も既存PaneSession、AsyncWorkOwner、WindowShutdownOwnerを組み合わせる。
   実WinUI closeの証拠を取る場合は既存AppWindowCloseAdapterと所有windowを使い、
   session APIだけの測定をkeyboardやsystem-closeの証拠と呼ばない。
   専用fixtureのsettingsはメモリ上の代替とし、通常の製品profileを開かない。
   そのclose測定はUNC readとhost ownerの結合までで、製品settings保存全体の実証には数えない。
   fixtureは未実装であり、別の製品経路や恒久的production hookは追加しない。

### 時間点と記録

fixture内のmonotonic clockで、read投入、worker開始、最初の列挙呼出前/復帰、
Escape受理、navigation caller完了、provider task完了、通常close要求、drain完了、Closedを区別する。
process終了は外部controllerが保持したprocess handleの終了とexit codeで別に確認する。
別processのtimestampを直接減算しない。

worker開始から最初の列挙復帰までにはBCLとOSの両方が含まれる。
native内部の待機時間とは呼ばず、fixtureのcallback観測遅延も記録の限界とする。
thread総数は他のworkを含むため、read投入数からprovider完了数を引いた未完了read数と分ける。
想定した待機中にEscape/closeを投入できなかったケースは、その境界の実証にならない。

記録はcase ID、条件ID、sample番号、相対時間、typed outcome、caller/providerの完了有無、
未完了read数、process終了結果、復旧結果に限定する。server/share/root/entry名、資格情報、
生の例外message、UNCを含むcommand lineは診断出力や公開Issueへ書かない（SEC-010）。
対象の設定を読むことと、その値をlogへ転記することを分ける。

### 初回の5ケース

| ケース | 条件・操作 | 観測対象 |
|---|---|---|
| M1 | 正常な専用rootを1回読む | queue待ち、列挙開始から結果まで、成功listing。基準値であり全SMB環境の性能ではない。 |
| M2 | ラボで応答を止め、Escapeせず1回読む | provider完了までの時間とtyped failure、または観測上限時の未完了。 |
| M3 | 同条件のpending readへEscapeを1回送り、caller完了後に所有local fixtureへnavigate | Escapeからcaller復帰まで、providerが残るか、新しいpane結果が旧結果に上書きされないか。provider復帰まで追う。 |
| M4 | pending readを放棄せず通常closeを要求 | 入力停止、closeの保留、復旧後のprovider/caller/drain/Closed/process終了の順。 |
| M5 | pending readをEscapeで放棄し、caller完了確認後に通常closeを要求 | 放棄済みproviderがdrainに再登録されないこと。process終了をproviderの正常完了と読み替えない。 |

初回は各1 sample。想定条件を作れなかった実行も残し、成功するまで繰り返さない。
追加sampleや最大2件のread重複観測は、初回で変動や残存が分かった場合に範囲を決める。
少数sampleから発生頻度、p95や一般的なtimeout値を出さない。

### 観測上限・復旧・配置

初回の観測窓は1ケース120秒を提案値とし、実行前に確定する。製品timeoutではない。
超過時は「120秒以上・未完了」と記録して新しいreadを止め、ラボ側を復旧して完了を追う。
復旧後も完了が取れない場合はそのcaseを未完了とし、専用processのPID・残存物を記録して停止する。
強制終了を通常closeの成功に換算しない。process退出したM5では未観測provider完了を補わない。

追加作業copy/fixtureは`D:/NeNeCommander/worktrees/<実測Issue>/`、
一時出力は`D:/NeNeCommander/outputs/<実測Issue>/`、恒久証拠は同プロジェクトの`evidence`配下へ置く。
SMB側は測定rootを読むだけで、setup・条件復旧はラボの所有者の手順に従う。
完了後にprocess、未保存物、唯一の証拠、linkと取込状態を確認してローカルの不要物だけを整理する。
実行するfixture/commandはまだ存在せず、架空の実行コマンドを記載しない。

## 実測後の判断

| 観測された問題 | 次の判断 |
|---|---|
| Escapeで実用上十分に戻り、残存readの影響も小さい | 現行契約を維持して制約と測定条件を記録する。 |
| UIの待機解除だけが必要 | 自動abandonの要否と表示/手動Escapeとの関係を検討する。OS I/Oを止める対策ではない。 |
| 再試行で未完了readが蓄積し、他workに具体的影響がある | readに限定した同時実行数/admission/公平性をADRで検討する。mutationの共有schedulerを一律timeoutにしない。 |
| 未放棄readが通常closeを長く止める | readの放棄と終了所有権をADR-0058/0071に沿って再検討する。settingsやmutationを期限で切り離さない。 |
| provider自体の停止が必要 | native cancellationの対象thread/handle、race、結果観測、driver対応を独立spikeで検証してからADRを判断する。 |

この調査の推奨は、現在の契約を維持してM1〜M5を専用環境で測ること。
実測対象と復旧可能な環境はまだ指定されていない。これが次段階の必要入力であり、現在の調査の未完了ではない。

## 変更・検証・統合

変更は本日報、対応する[引き継ぎ](../handoffs/2026-10-18-unc-wait-investigation-handoff.md)、
[PROJECT_STATE](../PROJECT_STATE.md)だけ。現在の未着手一覧に残っていた完了済み#244/#248も訂正する。
不変条件は、単一read経路・取消と完了の区別・既存ownerによる結果観測。
ADR-0027/0057/0058/0071を変更せず、新ADR、製品code/test、API、依存、gateの追加はない。

ADR-0061により文書の内容・相対リンク・diffと通常Commit hookを確認する。
正確な検証command/resultは`D:/NeNeCommander/evidence/issue-247/investigation-20261018/`の
`document-checks.json`、`commit.log`、`commit.json`に保存する。
製品build/test、format、CI、coverage/mutation、live UNC、network設定変更、UI操作は実行しない。
失敗した探索commandは、存在しないADR名/README指定とPowerShellのpath wildcard指定であり、
実在pathを`rg --files`等で確認して読み直した。製品検証の失敗ではない。

既存checkoutの`docs/289-unc-wait-investigation`を使い、追加worktree/一時outputは作らない。
本IssueのPRと`completion.json`が最終squash・main同期・親Issue openの確認を記録する。
恒久証拠とbranch/commitを保持し、この調査に後片付け待ちはない。
