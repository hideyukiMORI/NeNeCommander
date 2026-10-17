# 日報 2026年10月17日 通常closeより先に所有処理を終了する

Status: informational

hideの「次の作業を進めて」を受け、[直前の日報](2026-10-17-settings-temporary-recovery-daily-report.md)と
[引き継ぎ](../handoffs/2026-10-17-settings-temporary-recovery-handoff.md)から
[Issue #251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)を再開した。
開始時のmainは`e0ef4b4918c501f550d397857d2419aebdc6370f`でclean・originと同期していた。
#250・#264・#266の統合と整理は完了済みで、再実行していない。
NeNe Commanderサナが設計と判断、Solが中立ownerとWinUI hostの実装・対象検証、
別のSolが読み取り専用レビューを担当した。Lunaの事前調査も参照した。

## 対象と設計

[ADR-0071](../adr/0071-drain-before-normal-window-close.md)が今回の終了契約の正本である。
Issueの初回「追跡のみ」は時点付き履歴として残し、今回の受け入れ範囲を先頭に追記した。

従来は最後の`Window.Closed`から非同期drainを開始していた。
taskを所有するだけでは最後のwindowとmessage loopが生存する保証にならないため、
`AppWindow.Closing`で同期的にcancelし、`WindowShutdownOwner`が既存`AsyncWorkOwner`で
一度だけdrainを所有する。正常なdrain後にUI queueへ最終closeを予約し、adapterを外して閉じる。
新しいcleanup経路、保存経路、取消ポリシーは作らない。

停止開始後はpane workとEscape interruptの受付を閉じる。
hostはpointer hit testingを止め、key/characterを消費し、Loadedとintentの後発callbackを拒否する。
既存の同期window adjustment経路も止まり、XAMLの表示木は変更しない。
pane停止が例外になってもsettings queueをfinallyでdrainし、startup停止とgateway解放へ進む。
取消callbackが例外を出しても、両方の所有runの取消・await・cleanupを飛ばさない。
defect observerの実行を終える前にStopを完了させないよう、完了通知の順序も同じowner内で修正する。

通常closeは、cancelを観測しない同期filesystem呼出しの終了まで待つ場合がある。
設定保存がtyped failureで完了した場合も、保存成功へ読み替えず既存warningを保つ。
即時File.Copy取消、新prompt、timeout、partial copyの自動削除は追加しない。
既にEscapeで放棄した非変更PaneRead、OS session-end、kill/crash、dispatcher喪失は
await完遂保証の対象外である。予期しない例外には既存のfatal報告を使い、プロセス生存を保証しない。

## 検証と証拠

ADR-0061で、中立lifecycle、settings/sessionの直接境界、Appと関連test projectのRelease build、
architecture、変更C#のformat、実WinUI host、通常Commit hookを選んだ。
provider mutation、filesystem trust boundary、dependency、gateを変更しないため、
full/deep CI、全層coverage/mutation、無関係provider suite、#94のrelease画面確認は選んでいない。

停止後にworkを開始できる3件と、取消callback例外でawaitを飛ばす1件は、修正前にexit 2で失敗した。
実装前source、test source、patch、SHA、実行引数とraw logを
`D:/NeNeCommander/evidence/issue-251/neutral/red-terminal-stop/`と
`red-cancel-drain/`に保存した。`red-observer-order/`ではobserver実行中にStopが完了する1件の失敗を
確認した（2件選択のうちthrowing observerの対照1件は成功）。こちらもsourceとraw logを保持する。

途中の27件成功は旧candidateの証拠として保持し、observer順序の変更後の最終結果と混同しない。
最終的に中立owner 31件、settings/session境界11件、architecture 2件の計44件が成功した。
ownerの31件はobserver修正後の成功runのうち不変27件と、完了callbackの競合を取り除いた4件の
限定runを合わせた異なるcase数であり、繰り返し実行を加算していない。
4件は既存のcontrolled contextでcallbackの順序を固定したtest変更で、製品sourceは不変。
App・Architecture.Tests・Application.TestsのRelease buildは警告・エラー0。
中立ownerの変更file formatはexit 0だが、原因未診断のworkspace-load warningをbuild結果とは分けて記録した。
正確なコマンド、sourceの時点と再利用対応は`neutral/presentation-final-observer/proof.json`、
`neutral/presentation-final-replacement/proof.json`、host配下の証拠に保存する。
WinUIは製品profileを使わない専用hostで、production adapter sourceとPresentation ownerを使う。
本物のAsyncWorkOwner、SettingsSessionとfake storeのbarrierを分離し、pending中の二重close、
pane完了、2件の設定write、fixtureのresource-release callback、Closed、正常process exitを観測する。
resource releaseはfixtureの代替実装であり、製品全体のmutationやrelease証拠とは主張しない。
初回window操作にはcomputer-useを使い、検証専用の対象windowだけを選んだ。
hideの指摘どおり、既存`eng/ui-evidence`はUI Automationの`WindowPattern.Close`を使っており、
今回computer-useを選ぶ必要はなかった。追加のGUI操作は行わず、以後の検証手段は既存harnessを優先する。

実WinUIは二つの境界証拠として受理する。`run-001`は実際のtitle-bar closeとAlt+F4を保留し、
各barrierでwindow/processが生存し、pane→settings 2件→fixture resource release→Closedとなった。
ただし外部スクリプトがprocess handleを保持しておらず、終了コードはnullで未取得。
`run-002`は追加画面操作をせず、専用file markerをUI dispatcherから同じRequestCloseへ転送し、
同じdrainとqueued Window.Closeを通って、保持したhandleから終了コード0を取得した。
後者単独でAppWindow.Closingを試したとは主張せず、前者を終了コード0の一貫試験とも主張しない。
Verifyはnullを0へcastせず拒否するよう修正した。旧記録とfixture両版を保持する。
v1の検証scriptは変更後のcopyから再構成したもので、実行時原本の保存とは区別する。
App側の変更file formatもexit 0。hostの実行引数・raw log・fixture両版とsource hashは
`D:/NeNeCommander/evidence/issue-251/host/README.md`から参照できる。
独立した読み取り専用レビューに製品のP1/P2指摘はない。最終review recordも恒久証拠へ収載する。
初回のmissing using、helperのIDE0058、改行format、CsWinRT1028とCA2213の修正履歴はraw logを保持する。
規約や警告を抑止して通す変更はない。

## 統合と整理

作業branchは`fix/251-owned-window-shutdown`、worktreeは
`D:/NeNeCommander/worktrees/issue-251`、一時出力は`D:/NeNeCommander/outputs/issue-251`。
恒久証拠は`D:/NeNeCommander/evidence/issue-251`へ保存する。
PR、squash commit、main同期、対象の状態確認と整理実績は同配下の`completion.json`へ収載する。
証拠原本、branch、commit、共通SDKは整理対象に含めない。

## 次の作業

このIssueの実装・証拠・統合を完了してから、未着手のP4 Issues #244–#248などを別件として判断する。
#94はhide同席のrelease画面確認であり、この通常closeの開発証拠とは分ける。
