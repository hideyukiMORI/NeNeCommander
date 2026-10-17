# 引き継ぎ 2026年10月17日 通常closeの所有と待機

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、
[ADR-0071](../adr/0071-drain-before-normal-window-close.md)、
[日報](../reports/2026-10-17-owned-window-shutdown-daily-report.md)。
[Issue #251](https://github.com/hideyukiMORI/NeNeCommander/issues/251)は
`e0ef4b4918c501f550d397857d2419aebdc6370f`から再開した。
先行#250・#264・#266の統合と整理は完了済みで、再依頼や削除再試行をしない。

## 維持する契約

- `AppWindowCloseAdapter`はClosingを同期cancelして一つの要求へ変換する。
  `WindowShutdownOwner`は既存`AsyncWorkOwner`で一度だけdrainし、成功後のUI queueでadapterを外して閉じる。
- Stopは新規work/interruptを拒否するterminal操作。取消callbackの例外でcleanup待機を飛ばさず、
  defect observerの呼出しを終えるまで完了通知を出さない。
- Windowは停止開始後の入力を閉じ、pane停止の例外時もfinallyでsettingsをdrainする。
  Appは既存のnested finallyでstartupを停止しgatewayを解放する。
- settings typed failureを保存成功に変えない。取消粒度、partial-effect通知と所有artifactだけのcleanupは既存どおり。
- 非協調的な同期I/Oのため通常closeが待つ場合がある。timeoutや強制rollbackはない。
  放棄済みの非変更PaneRead、OS終了、kill/crash、dispatcher喪失までawait保証を広げない。

## 証拠の読み方

`D:/NeNeCommander/evidence/issue-251/`に実行引数、raw log、source/patch原本、SHAとfixtureを保持する。
修正前のterminal Stop 3件とcancel callback 1件は失敗を確認した。
observer通知中にStopが先行完了する1件も追加redで再現した。
途中の27件成功と、observer順序の追加修正後の結果を区別する。
最終は中立owner 31件、settings/session 11件、architecture 2件の計44件成功。
31件は後半の変更なし27件と、callbackの順序を決定化した4件の限定runを合わせる。
AppとArchitecture.Tests・Application.TestsのRelease buildは警告・エラー0。
中立ownerのformatはexit 0だがworkspace-load warningを別記する。
正確なargv・hash・再利用は`neutral/presentation-final-observer/proof.json`と
`neutral/presentation-final-replacement/proof.json`を参照する。
runtimeは二つを組み合わせる。run-001は実system Closing 2回の保留とdrain→Closedを確認したが、
process handleを保持せず終了コードnull。run-002は専用marker→UI dispatcher→同じRequestCloseと
drain/final Window.Closeで終了コード0を取得した。run-002はAppWindow.Closingの独立実証ではない。
この二つを単一のsystem-close/exit-0試験と記述しない。旧記録とfixture両版は保持する。
v1検証scriptは後のcopyから再構成したもので、実行時の原本とは主張しない。
App側のformatもexit 0。

専用WinUI fixtureはproduction adapterとPresentation ownerのhost寿命を証明する。
設定queueは本物のSettingsSessionとfake storeを使うが、gateway release callbackはfixtureの代替実装。
製品全体を実操作した証拠、実providerの取消、OS終了やkill/crashの証拠ではない。
#94のmanual release proofは未実行のまま別件に保つ。
初回のsystem close操作にはcomputer-useを使ったが、hideの指摘どおり必須ではなかった。
既存`eng/ui-evidence`のUI Automation経路を優先し、通常の試験をcomputer-useへ広げない。

## 統合・整理の確認先

`completion.json`とIssue PRの実状態からmain統合、同期と整理を読み戻す。
作業物は`D:/NeNeCommander/worktrees/issue-251`と`D:/NeNeCommander/outputs/issue-251`。
整理前に唯一のfixture/log/sourceを恒久証拠へ保存し、未保存file、ignored output、稼働参照、linkを確認する。
branch、commit、evidenceと共通SDKは保持する。

次は未着手P4 Issues #244–#248などの別件選定。通常closeの今回scopeに新機能を混ぜない。
