# 日報 2026年10月18日 ウィンドウ調整の長押し調査

Status: informational

[Issue #303](https://github.com/hideyukiMORI/NeNeCommander/issues/303)で、旧記録の
「合成keydown 15回に対して `h` は15 step、`l` は11 step」の意味を確認し、
現在の受信イベントから移動までを独立WinUI fixtureで実測した。
今回の実長押しでは `h` 32/32、`l` 55/55、Left 40/40、Right 62/62の
受信actionイベントがすべて1回40 pxの移動になった。旧現象の原因は未確定であり、修正済みとはしない。

baselineは`93a9b7b7157da32d7afae68d188fb9c6f8b4ab82`。
製品code/test、依存、gate、key repeat設定は変更していない。
今回の採否判断は現行契約を維持し、RepeatCountの乗算や独自input injectorを追加しないこと。
調査Issueとして観測結果と限界を確定し、旧条件の完全再現を今回の完了条件へ追加しない。

## 不変条件と正規経路

`WinUiKeyboardInputTranslator` → `KeyboardIntentMapper` → `WindowAdjustmentRoute` →
`CommanderSession` / `WindowAdjustmentSession` / `WindowAdjustmentPlanner` →
`AppWindowPlacementAdapter`が唯一の挙動経路である。
KBD-003/005、ARC-001、ADR-0050/0052、QLT-015/ADR-0061を維持する。
文字キーはCharacterReceived、矢印はKeyDownから1 actionへ変換される。
同期window routeは`AsyncWorkOwner`の入力抑止を通らず、最新placementの取得、判断、適用を行う。

WinUI translator、route、adapterのGit blobはPR #157時点
`41a682cc7f4c6213f7515cbb0ce6249e0975426f`と一致した。
内部の決定的translatorには他キーの追加があるが、今回のh/l/矢印の変換は変わっていない。
WinUI変換はWasKeyDownをInitial/Repeatedへ渡し、RepeatCountは読み取っていない。
これだけを理由に不具合や修正方針を断定しない。

## 旧証拠から分かること

Issue #100の`20261007-013527`は、Ctrl+Wのウィンドウ調整の記録であり、
file-list navigationの取りこぼしを示す証拠ではなかった。
旧driverは初回keydown 1回と追加keydown 14回を合成し、最後にkeyupを送った。
追加待機33 msにhelper内部の15 msが加わるため、公称間隔は48 msである。
最後のkeyup後は300 ms待って座標を観測した。

当時のwindowは2880 × 1550 px、stepは40 px。
`h` のnet変位−600 pxは15 step、`l` の+440 pxは11 stepに相当する。
ただし受信KeyDown/CharacterReceived数、RepeatCount、各actionの前後座標、拒否状態は記録されていない。
「投入した15回」と「受信した15イベント」は同じ主張ではなく、4回分の差の発生箇所は特定できない。
旧driverは読み取り専用で保管し、実行していない。

Microsoftは、処理速度を超えるキー反復が結合されrepeat countが増えることを説明している。
これは候補となる機構であって、旧現象の原因を示す証拠ではない。
出典は[Keyboard Input / Repeat Count](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input#repeat-count)、
[WM_CHAR](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-char)、
[CharacterReceived.KeyStatus](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.characterreceivedroutedeventargs.keystatus)。

## 測定条件と結果

Windows build 26300、SDK 10.0.401、Windows App SDK 2.5.1、PerMonitorV2、scale 1.25。
fixtureの初期windowは800 × 400 px。変更していない4つの製品sourceを直接取り込み、
既存Presentation/Application/Domainを参照した。
通常profile/settings/filesystem/provider/Shellを使う入口はthrowする診断portで閉じている。
scopeを既存public APIで直接開くため、Ctrl+Wの入場処理や全CommanderWindowのrenderingは測定していない。
イベントはメモリへ蓄え、keyup/close時に保存した。

`Observe only`は受信・mappingのみ、`Move using production route`は同じmappingから既存routeを呼ぶ。
hideが物理キーを長押しし、別々に離した区間を受理した。
既存harness/Computer Useにはhold APIを追加せず、合成holdも実行していない。
Computer Useでの事前controlは単発pressであり、実長押しの件数には含めない。

| mode | キー | run | KeyDown | CharacterReceived | mapped action | 実移動 / net変位 |
| --- | --- | --- | ---: | ---: | ---: | --- |
| Observe | h | manual-02 | 95（63/10/22の3区間） | 95 | 95 | 0 / 0 px |
| Observe | l | manual-02 | 69 | 69 | 69 | 0 / 0 px |
| Observe | Left | manual-01 | 48 | 0 | 48 | 0 / 0 px |
| Observe | Right | manual-01 | 78 | 0 | 78 | 0 / 0 px |
| Move | h | manual-02 | 32 | 32 | 32 | 32 / −1280 px |
| Move | l | manual-02 | 55 | 55 | 55 | 55 / +2200 px |
| Move | Left | manual-01 | 40 | 0 | 40 | 40 / −1600 px |
| Move | Right | manual-01 | 62 | 0 | 62 | 62 / +2480 px |

最初のmanual-01では文字入力がj/kだったため、h/lだけをmanual-02で追加測定した。
j/kもObserveで66/73、Moveで23/40イベントを受信し、MoveのY変位は+920/−1600 pxだった。
manual-01の途中に重なったLeft/Right単発各1回があり、独立した長押しの集計から両区間を除外した。
その後の独立したLeft/Right長押しは表のとおり受理できた。

2つのmanual runの全254適用actionは、重なった単発2回を含め、各actionのXY変位が期待と一致した。
受理した独立区間は14、重なりによる除外は2。各独立区間の受信イベント数とmapped action数、
net変位も一致し、区間中のfocus離脱・deactivation、拒否、diagnostic defectはなかった。
全記録のRepeatCountは1で、repeat count増加によるイベント結合は今回観測されなかった。
manual-01は1466記録、manual-02は1115記録で、双方ともWindow.Closed、ProcessExit、外部handleのexit 0を確認した。
長押し時間は同一でないため、h/lの回数の差自体を性能差や欠落率として比較しない。

## 測定上の修正と検証

初回buildは内部translatorのアクセス制限で失敗した。
製品の公開範囲を変えず、変更しないsourceをfixtureへ直接取り込んで解決した。
setup-01はキー操作前に終了した準備用runで、終了時COMExceptionがあり受理していない。
stock WinUI resources、製品と同じDPI manifest、破棄後のwindow読取り回避をfixtureへ反映した。
最終build-03は警告0・エラー0。その同一binaryでcontrol-01と両manual runを実行した。

初版の解析はfixtureのglobal press番号でkeyupを対応付け、XYのうちXだけを確認していた。
raw記録の重なりを発見し、解析を実keyと時系列による対応付け、重複区間除外、各actionのXY確認へ直した。
旧解析script/結果は`*-v1-superseded.*`として保管し、受理証拠には使わない。
v2で3つの保存済みrunを解析し直した。これは解析修正の検証で、実キー操作の再実行ではない。
controlの単発5区間とmanualの独立14区間が成立し、重なり2区間が除外されることを確認した。

恒久証拠は`D:/NeNeCommander/evidence/window-repeat-20261018/`。
`artifact-manifest.json`に最終source/binary 354 fileのSHA-256、`runs/`にsetup/control/manualのraw記録、
`launch-*.json` / `exit-*.json`に実行引数・binary hash・終了結果を保存した。
旧driver/source/結果、失敗したsetup版、最終fixture source、解析script、出力境界3件の結果も保持する。

固定dotnetは`D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe`。
以下のdotnetはそのPATH指定を略記し、TEMP/TMPは`D:/NeNeCommander/outputs/issue-303/tmp`に固定した。

```powershell
dotnet restore D:/NeNeCommander/outputs/issue-303/fixture/RepeatProof.csproj --configfile D:/NeNeCommander/outputs/issue-303/fixture/NuGet.Config
dotnet restore D:/NeNeCommander/outputs/issue-303/fixture/RepeatProof.csproj --locked-mode --configfile D:/NeNeCommander/outputs/issue-303/fixture/NuGet.Config
dotnet build D:/NeNeCommander/outputs/issue-303/fixture/RepeatProof.csproj -c Release --no-restore
pwsh -NoProfile -File D:/NeNeCommander/evidence/window-repeat-20261018/Analyze-RepeatEvents.ps1 -Run control-01
pwsh -NoProfile -File D:/NeNeCommander/evidence/window-repeat-20261018/Analyze-RepeatEvents.ps1 -Run manual-01
pwsh -NoProfile -File D:/NeNeCommander/evidence/window-repeat-20261018/Analyze-RepeatEvents.ps1 -Run manual-02
pwsh -NoProfile -File D:/NeNeCommander/evidence/window-repeat-20261018/Verify-RepeatFindings.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/window-repeat-20261018/Check-Documentation.ps1
```

両restoreと最終build、v2解析は成功した。引数欠落、所有範囲外出力、既存出力rootの3件はexit 2で拒否した。
出力境界の実装はUI準備修正で変わらず、その成功済み証拠を再利用した。
controlはObserveのh 1回とMoveのh/l/Left/Right各1回、計5区間・4移動・exit 0。
文面/link/diffと通常Commit hookも選択する。製品suite、full/deep CI、coverage/mutation、NAS/WSL測定は選ばない。
製品入力が不変であり、ADR-0061の範囲は独立fixture、解析、文書に限られるためである。

## 結論と終了時の扱い

現在のfixtureでは、受信イベント以降の欠落、mapper拒否、幾何学上の拒否、適用とnet観測の不一致を認めなかった。
旧合成入力と同じ刺激、当時の全UI負荷・サイズ・OS入力状態、hardware scan数は未観測である。
旧15/11の原因をOS結合、描画負荷、入力注入側のいずれかへ帰属する根拠はない。
RepeatCountの乗算は新しい挙動方針を必要とするため、今回の調査からは採用しない。
将来実利用で再現した場合は、その条件の受信・RepeatCount・action・各変位を同時記録し、
製品計測や新input手段が必要なら具体的な範囲とADRを先に決める。
全製品UIと#94のrelease proofは今回の証拠に含めない。

全測定processは終了済み。PRで調査結果を統合し、main同期・tree一致後にtask worktree/outputを整理する。
branch、commit、恒久証拠、共通SDKを残す。
実際のPR番号、merge/mainの状態、整理結果はIssue PRと恒久証拠の`completion.json`に記録し、
本文の測定完了を統合・整理完了と読み替えない。
