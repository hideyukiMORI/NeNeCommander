# 日報 2026年10月18日 UNC測定fixture

Status: informational

Issue [#291](https://github.com/hideyukiMORI/NeNeCommander/issues/291)は、hideの「進めて」に基づく
[#247](https://github.com/hideyukiMORI/NeNeCommander/issues/247)の測定準備と、指定された専用NASでの読み取り観測である。
baselineは`4cccad04eff1c1ed477022290f60d7a0a7c787d7`。
前段の[調査と5ケースの計画](2026-10-18-unc-wait-investigation-daily-report.md)を受け、
既存のpublic APIを呼ぶ独立fixtureを用意した。製品のtimeoutや終了契約は変更しない。

## 構成と不変条件

fixtureは既存の[ProviderDirectoryReadPort](../../src/NeNeCommander.Infrastructure.Windows/Directories/ProviderDirectoryReadPort.cs)、
[PaneSession](../../src/NeNeCommander.Application/Panes/PaneSession.cs)、
[AsyncWorkOwner](../../src/NeNeCommander.Presentation.WinUI/Lifecycle/AsyncWorkOwner.cs)、
[WindowShutdownOwner](../../src/NeNeCommander.Presentation.WinUI/Lifecycle/WindowShutdownOwner.cs)を組み合わせる。
public read portの前後だけを観測し、列挙algorithm、native呼出、取消・完了の所有者を再実装しない。
headlessの直列contextはfixtureのcallback順を保つためのもので、WinUI dispatcherの証拠ではない。
設定profile、Shell起動、NAS側の作成/更新/削除は使わない。

製品code/test、project graph、API、friend grant、依存、gateに変更はない。
独立fixtureの11個のC#ファイルとproject/SDK/NuGet/lockの計15ファイルはD側の恒久証拠に収載する。
repositoryの変更は日報・引き継ぎ・PROJECT_STATEとTEST_STRATEGYの4文書だけ。
TST-011では、従来の「live UNC未実施」を今回の限定観測とcanonical gate非搭載の区別へ更新する。
ADR-0027/0057/0058/0071の既存契約を維持し、新ADR/waiverはない。

## 通信なしの確認

| ケース | 確認した結果 |
|---|---|
| S1 | 成功listingをpaneへ反映し、ownerを終了できる。 |
| S2 | providerを明示barrierで保留し、解除後のtyped failureを反映する。 |
| S3 success/cancel/failure | Escapeでcallerが完了し、tokenは取消済みでもproviderは残る。次のnavigation後に旧結果を返しても新しいsnapshotを上書きしない。 |
| S4 | 未放棄readが残る間はowner-closeが待ち、新規work/重複closeを拒否する。provider解放後に一度だけclose callbackを呼ぶ。 |
| S5 | 放棄済みproviderがpendingでもowner-closeが完了する。その後fixture内でproviderを解放して観測を終える。 |

合計7ケース、exit 0。時間閾値を成功条件にせず、明示barrierと状態/参照で判定した。
実SMBの待機時間、実キー入力、実Window.Closedや製品process退出をこれらの成功から推定しない。

初版のCLI入口10ケースも成功した。hideが専用rootを具体的に指定した後、fixture独自の
`NeNeCommander-Live-`名前prefix制限を、入力の`dedicatedRootConfirmed: true`へ置き換えた。
rootは既存parserで非share-rootのUNCに限定し、明示的なnetwork opt-in、新規D側output、link祖先/上書き拒否を維持する。
この変更後の12ケースは全件期待したexit 0/2になり、診断に合成UNCを出していない。
旧source/binary/manifestを`offline-verified-version/`に残す。M1/M2版は`m1-m2-verified-version/`に保存し、
その後、切断から復旧へ進む操作を調整するためlive操作直後の`action.json`保存と固定メッセージを加えた。
最終版との差はlive入力受付とこの観測記録だけ。offlineの実行経路に変更がないため7ケースは再実行していない。
M1/M2のread経路とCLI入口12件も不変なので、操作記録の追加だけで再実行していない。

## 実NASの観測

hideが指定した専用NAS内のrootを、既存Windows logon sessionで読み取る。
実際のUNCとファイル名は公開文書や測定logに転記せず、環境IDを`nas-01`とする。
資格情報、mapping、firewall、SMB timeoutを変更しない。接続条件の変更はhideがNAS側LANケーブルを抜き差しする。
各readは既存providerによる直下の列挙、entry boundary 32。再帰や内容読み取りはない。

| ケース | 観測 |
|---|---|
| M1 接続中 | 1 sample成功。public port投入から完了観測まで19.214 ms、navigation開始からcaller完了観測まで25.548 ms。正常なowner終了とfixture process exit 0を確認。 |
| M2 切断中のread | 1 sample。providerの完了観測まで67.011秒後にProviderUnavailableFailure、callerはPaneReadFailed。取消を送っていないこのケースで約67秒の待機を観測した。 |
| M3 Escape後navigate | 1 sample。Escape intent受付からcaller復帰まで3.977 msで、providerはpending。新しい所有local fixtureへのreadは成功。その後、元のproviderは投入から40.859秒後にCancelledで完了。 |
| M4 未放棄owner-close | 1 sample。close要求からcallbackまで41.342秒。providerがProviderUnavailableFailureで完了し、caller完了→drain完了→close callbackの順。token取消の通知をtyped Cancelledと読み替えない。 |
| M5 放棄後owner-close | 1 sample。Escapeからcaller復帰は4.877 ms。元のproviderが残ったまま、close要求からcallbackまで1.396 ms。fixture内で元のproviderを追い、投入から42.603秒後にProviderUnavailableFailureで完了した。 |

5ケースとも観測器はexit 0。これは測定の完走であり、失敗結果を含むSMB readすべての成功ではない。
120秒を超えたケースや、pending条件が作れず拒否されたケースは今回はなかった。
3回の差し戻し後に各1回の復旧readが成功し、最後の確認は2026-10-18 14:21:05 JSTに完了した。
全測定processは退出済み。NAS側の内容変更やhost設定変更はない。

計測値は1つのmonotonic clock内の差分。providerのworker開始、最初の列挙/native呼出は未観測であり、
19.214 msをnative I/O単体の時間とは呼ばない。初回実行をcold接続と同一視しない。
Get-SmbConnectionはCimExceptionで情報を取れず、既存sessionとdialectは不明として残した。権限や接続状態を変えて再試行していない。
LAN差し戻しの物理時刻は計測器で観測していないため、差し戻しからprovider完了までの時間を算出しない。
M3ではlocal navigationの成功と元providerの取消完了を記録したが、最後のpane snapshot一致をliveでassertしていない。
遅延結果が新しいsnapshotを上書きしないassertは通信なしのS3の証拠と区別する。
各ケース後の接続復旧確認は、変わった環境が戻ったことを確認する別readであり、正常系の性能sampleへ合算しない。
1 sampleから発生頻度、p95や一般的なSMB timeoutを結論しない。

120秒は観測窓で、到達時は`pending.json`を保存し、そのままprovider/操作の完了を待つ。
製品timeoutや強制終了ではない。操作待ちもこの時間に含む。
pending状態を確認できなかった実行は、そのEscape/close境界の証拠にしない。
headlessのclose callbackはWindow.Closedではなく、fixtureはcallback後も元のproviderを明示的にawaitする。
したがって、放棄providerが残ったまま実製品が退出するM5のprocess寿命確認には使えない。
settings保存全体や#94のrelease確認も未実施。

## コマンド・保存先・統合

恒久証拠は`D:/NeNeCommander/evidence/issue-247/fixture-preparation-20261018/`。
`README.md`に起動方法と限界、`fixture-source/`と`fixture-bin/`に最終source/19 binaryファイル、
manifestにSHA-256、各結果JSONにevent列と実際のexitを保存する。
実対象を含むprivate configは恒久診断へ収載しない。

実行した主なコマンドは以下。SDKは`D:/NeNeCommander/tools/dotnet-10.0.401`をprocess環境で選択した。

```powershell
dotnet restore D:/NeNeCommander/outputs/issue-291/fixture/UncMeasurement.csproj --locked-mode -p:Configuration=Release
dotnet build D:/NeNeCommander/outputs/issue-291/fixture/UncMeasurement.csproj -c Release --no-restore --nologo
dotnet D:/NeNeCommander/outputs/issue-291/fixture/bin/Release/net10.0-windows10.0.26100.0/UncMeasurement.dll --offline D:/NeNeCommander/outputs/issue-291/offline-001
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-247/fixture-preparation-20261018/check-designated-root-admission.ps1
```

locked restoreと最終fixture Release buildはexit 0、buildは0 warnings/errors。
初回restore/build、read識別/観測記録を整えたbuild、専用root受付変更後のbuildを区別して保存した。
live操作checkpoint追加後の最終buildは`build-action-checkpoint.log`。sourceとbinaryの保存版を実行別に対応させる。
liveコマンドは`dotnet <fixture.dll> --live <private-config> <new-local-output> --allow-network`の形で、
実引数にUNCを入れず、case別記録とprivate configを分ける。
ADR-0061によりfixtureの該当動作と文書link/diff/通常Commit hookを確認し、
製品suite、full/deep CI、coverage/mutationは実行しない。

branchは`test/291-unc-measurement-fixture`、worktreeは`D:/NeNeCommander/worktrees/issue-291`、
一時出力は`D:/NeNeCommander/outputs/issue-291`。
Issue PRと`completion.json`がsquash統合、main同期、恒久収載、終了したtaskの整理結果を記録する。
親#247は未観測境界と必要な判断を残すためopenを維持する。
今回の推奨は、観測値を仕様上のtimeoutへ直結せず、現行動作を維持すること。
未放棄readが実際の通常window closeへ与える影響を次の限定確認候補とし、
自動abandon、native取消、read admission変更は別の設計判断として扱う。

## 統合完了と後片付け

PR [#292](https://github.com/hideyukiMORI/NeNeCommander/pull/292)を2026-10-18 14:24:07 JSTにsquash mergeした。
mainは`8eb48350615f28bf65612b9cd960b597ac9ccd0a`へ同期し、cleanかつtask treeと一致した。
#291はclosed、親#247はopen。通常Commit hookは112 normative rules / 24 adversarial登録とcommit message確認を通過した。
文書確認は125 relative linksでbroken 0、保存版102 hash一致、実対象の診断混入0。
新しいgate/CIや製品suiteの実行はない。

worktreeはtracked/untracked clean、ignored 109件はbin/objのみ、outputは100ファイル。
対象内のlinkと観測できるprocess/module参照は0。唯一の観測結果を含むsource/binary/log/resultは恒久収載し、
生成obj/analyzer cacheは再生成可能、private接続入力4件は公開・恒久診断へ残さず破棄する分類とした。
追加worktreeの削除は成功した。続く一時outputの削除は、自動承認レビューが`blocked by policy`で拒否し、
コマンドは開始されていない。別手段で再試行していない。

Issue #293はこの実状態を日報/引き継ぎ/PROJECT_STATEへ記録する文書だけのclosure。
既存checkoutを使い、追加worktree/outputを作らず、NAS測定や製品検証を再実行しない。
恒久証拠の`cleanup-preflight.json`、`cleanup-output-inventory.json`、`cleanup-pending.json`が根拠。
準備したscriptは構文確認だけで、サナは実行していない。実行またはabsence確認までは未削除として扱う。
branch/commit、`D:/NeNeCommander/evidence/issue-247/fixture-preparation-20261018/`、共有SDKは保持する。

! hide実行待ち：後片付け

対象: `D:/NeNeCommander/outputs/issue-291` のみ。
自動承認レビューによる拒否理由: `blocked by policy`。worktreeは削除済み、恒久証拠とSDKは削除しない。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-247/fixture-preparation-20261018/cleanup-completed-output.ps1
```
