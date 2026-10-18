# 日報 2026年10月18日 UNC read中の実WinUI closeを測定する

Status: informational

hideの「次の対応に進めて」を受け、[Issue #247](https://github.com/hideyukiMORI/NeNeCommander/issues/247)の
残るhost境界を[Issue #297](https://github.com/hideyukiMORI/NeNeCommander/issues/297)として測定した。
開始時のmainは`bafa828c863ff93046dda6352a048df2acebdd33`でclean・originと同期していた。
[直前の日報](2026-10-18-unc-measurement-fixture-daily-report.md)のheadless測定と、
[通常closeの既存証拠](2026-10-17-owned-window-shutdown-daily-report.md)を区別して使う。
NeNe Commanderサナが単体で設計・fixture実装・測定・レビューを担当した。

## 不変条件と測定範囲

[ADR-0071](../adr/0071-drain-before-normal-window-close.md)の既存経路
`AppWindowCloseAdapter → WindowShutdownOwner → AsyncWorkOwner.StopAsync → queued Window.Close`
を使う。未放棄のreadはdrain対象で、Escapeで放棄済みの非変更readを再加入させない。
独立WinUI fixtureがproduction adapterをsource linkし、既存Presentation/Application/Infrastructureを参照する。
製品のソース、テスト、graph、依存、owner、取消方針、timeout、設定形式、gateは変更しない。

同じrun内でsystem Closingのcancel、caller/provider状態、drain、Window.Closed、ProcessExit、
外部から保持したprocess handleによる終了コードを個別に記録する。
public provider Taskの未完了を直接読み、native callやkernel内の終了とは同一視しない。
fixtureはCommanderWindow全体を起動せず、通常profile/settingsやmutation、Shellを開かない。
EscapeはfixtureのKeyDownから既存owner/sessionへ送るため、製品全体のkey mappingの証拠ではない。

hideが指定した専用NASと現在のWindows identityを使い、NAS側LANの切断・復旧はhideが操作する。
readは最大32項目の直下一覧だけで、再帰、内容読取、remote mutation、認証変更、接続reset、host設定変更はない。
実UNC・entry名・資格情報・例外messageは記録せず、実target設定は一時出力だけに置く。
network readは測定窓のStart readを操作した時だけ開始する。

既存`eng/ui-evidence/UiEvidence.psm1`の`Stop-OwnedProcess`は30秒待機後にkillするため、
今回の待機観測では使わない。computer-useで返された一意の`NeNeCommander #297`窓のみを選び、
通常closeをAlt+F4で送る。直接のPowerShell UI Automationは併用しない。
外部recorderは起動直後からprocess handleを保持し、強制終了やread timeoutを使わない。
120秒のnoticeには操作待ち時間も含み、その後も観測を継続する。

## 結果と未成立の試行

接続中の`control`はread成功後に通常closeし、Closingのcancelからdrain、Window.Closed、
ProcessExit、終了コード0まで同一runで確認した。public providerは1,332.647 msで完了し、
close要求からWindow.Closedは5.080 ms、外部の終了観測は111.428 msだった。
最初のaccessibility-only clickはgeometry不足で拒否され、readは始まらなかった。
新しい画面観測後の1回の開始だけをraw traceで確認している。

最初の`active`は2.734秒でProviderUnavailableになり、closeの送信前にreadが完了した。
Computer Useは利用者入力を検知して古い観測からのkey送信を拒否し、再観測後に通常終了した。
終了コード0だが「待機中close」の前提が未成立なので受理しない。原記録を残す。
その後のNAS復旧readは成功した。

NeNeFolioへ計測環境を渡すため14:49–15:26 JSTは計測・buildを中断した。
中断時の`abandoned`と`active-02`はreadを一度も開始せず、hideの通常closeにより終了コード0で終了した。
これらも測定回数へ数えない。hideの再開指示後、15:27:27 JSTに接続中readの成功を再確認した。

最終的に次の3条件を受理した。時間は同一processのmonotonic clockで求め、
外部の終了観測にはrecorderの観測遅延も含む。異なる経路の測定開始時点を混ぜない。

| 条件・run | public provider | close要求→Window.Closed | close要求→外部exit観測 | exit |
| --- | --- | --- | --- | --- |
| 接続中 `control` | 成功、1,332.647 ms | 5.080 ms | 111.428 ms | 0 |
| 未放棄 `active-03` | ProviderUnavailable、9,051.344 ms | 4,170.482 ms | 4,276.852 ms | 0 |
| Escape後 `abandoned-02` | ProcessExit時も未完了 | 6.161 ms | 106.238 ms | 0 |

`active-03`はprovider未完了時のsystem Closingをcancelし、provider failure→caller完了→
drain完了→final close→Closed→ProcessExitの順だった。
`abandoned-02`は実Escapeの受付からcaller復帰が4.335 msで、providerが残る間に通常closeした。
Closed時とProcessExit時にも実public provider Taskが未完了で、provider-completed eventはない。
保持したhandleから正常exit 0を確認し、headless fixtureがproviderをawaitした以前の証拠とは区別する。
どのprocessもkillしていない。2条件は同じ切断中に順次実施しており、独立した遅延分布の標本とは扱わない。

成立した3条件の順序・cancel・未完了状態・正常exitを`Verify-WindowMeasurements.ps1`で確認した。
NAS側LAN差し戻し後、15:33:09 JSTにread成功・exit 0を確認した。
最終的に測定processは全て終了し、network待機を残していない。
計3回の接続/復旧確認は以前の#291 final binaryを使い、新たな性能標本へ加算しない。
物理的な切断/差し戻し時刻やSMB session/dialectは観測していない。

## 検証と恒久証拠

恒久証拠は`D:/NeNeCommander/evidence/issue-247/window-close-20261018/`。
exact source/lockfile、実行可能な全binary、直接参照する終了処理3source、計353 fileをSHA-256付きで保存した。
`artifact-manifest.json`、`build-and-environment.json`、各runの`launch.json`、`events.jsonl`、
`exit.json`、未成立理由の`acceptance.json`から追跡できる。
一時実target設定は恒久証拠へ収載しない。

固定実行ファイルは`D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe`。
以下の`dotnet`はそのPATH指定を略記し、TEMP/TMPは`D:/NeNeCommander/outputs/issue-297/tmp`に固定した。

```powershell
dotnet restore D:/NeNeCommander/outputs/issue-297/fixture/HostProof.csproj --configfile D:/NeNeCommander/outputs/issue-297/fixture/NuGet.Config
dotnet restore D:/NeNeCommander/outputs/issue-297/fixture/HostProof.csproj --locked-mode --configfile D:/NeNeCommander/outputs/issue-297/fixture/NuGet.Config
dotnet build D:/NeNeCommander/outputs/issue-297/fixture/HostProof.csproj -c Release --no-restore
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-247/window-close-20261018/Test-Admission.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-247/window-close-20261018/Verify-WindowMeasurements.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-247/window-close-20261018/Check-Documentation.ps1
```

両restoreとRelease buildは成功、buildは警告・エラー0。
opt-in、専用root確認、share root、scenario、出力範囲、既存出力拒否の6件は全て期待するexit 2で拒否し、
networkアクセス・canary targetの出力・意図しない出力作成がないことを確認した。
actual system closeの実行引数と画面操作、復旧readの引数は証拠READMEに保存する。

ADR-0061でfixtureのbuild/起動境界、今回のhost測定、文面・リンク・差分、通常Commit hookだけを選んだ。
製品入力は不変のため、#291のoffline 7件、#251の既存owner/barrier証拠は再実行せず参照する。
それらを今回新しく成功した件数へ加算しない。製品suite、full/deep CI、coverage/mutationは実行しない。

## 解釈・統合・次の判断

各条件は単発観測であり、p95、一般的なSMB timeout、製品全体の終了証明、#94のrelease proofではない。
native worker開始・最初のnative call・SMB内部状態・設定保存・製品compositionのresource解放は未計測である。
設計判断は現行契約を維持し、独自timeoutや自動abandonは採用しない。
Escapeによるcaller離脱と放棄後の終了が既存経路で機能し、未放棄readの通常close待機も
ADR-0071どおりだった。単なるtimeoutは同期OS呼出し自体の取消を証明せず、
別ownerや新たな待機方針を導入する根拠は今回の範囲から得られていない。
これは既存契約を変えない採否判断なので新ADRは作らない。
親#247はsource調査、専用NAS測定、host境界確認と採否判断が揃ったものとして、このPRで完了する。
未計測のnative内訳や広い環境分布は限界として残し、今回の完了条件へ後から追加しない。
将来native cancellationや並列度を変える具体的要求が出た場合は、別Issue/ADRと影響に応じた証拠を先行させる。

Issue PRと恒久証拠の`completion.json`がsquash統合、main同期、tree一致、整理の最終状態を記録する。
task専用worktreeは`D:/NeNeCommander/worktrees/issue-297`、一時出力は`D:/NeNeCommander/outputs/issue-297`。
統合後に未保存・未追跡・ignored file、唯一の成果物、稼働参照、linkを確認して整理し、branch/commit/恒久証拠/SDKを保持する。
