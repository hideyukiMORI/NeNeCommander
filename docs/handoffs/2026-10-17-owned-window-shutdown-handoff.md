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

[PR #268](https://github.com/hideyukiMORI/NeNeCommander/pull/268)は2026-10-17 21:04:02 JSTに
squash mergeされ、Issue #251はclosed。
main `0a92009f44756cd9f00110a5d59572838e65ce93`への同期、clean・origin一致と
task/mainの同一tree `3a78fef9c47cf6cca72b7623cce7d3e9d6491204`を確認済み。
`D:/NeNeCommander/worktrees/issue-251`は削除済み。
`D:/NeNeCommander/outputs/issue-251`の唯一の39 fileは恒久証拠`cleanup-archive/`へSHA一致で収載済み。
未保存・未追跡file、ignored output、link、稼働参照と専用host両PIDの終了も確認した。
一度自動承認に削除を拒否された出力も、下記のとおりhideの実行と不在確認で整理を完了した。
branch、commit、evidenceと共通SDKは保持済み。
正確な記録は`D:/NeNeCommander/evidence/issue-251/completion.json`と`cleanup-inspection.json`。
[Issue #269](https://github.com/hideyukiMORI/NeNeCommander/issues/269) / PR #270は統合と削除待ちを記録済み。
[Issue #271](https://github.com/hideyukiMORI/NeNeCommander/issues/271)は今回の後片付け完了記録だけを扱う。
文書更新・push・mergeのために成功済みの製品試験を繰り返さない。

次は未着手P4 Issues #244–#248などの別件選定。通常closeの今回scopeに新機能を混ぜない。

## 後片付け完了

hideが用意済みscriptを実行し、2026-10-17 21:30:27 JSTに正常完了した。
21:31:33 JSTの読み取り専用確認で`D:/NeNeCommander/outputs/issue-251`は不在。
Issue #251と#269のworktreeも不在で、恒久証拠39 file、branch、commitと共通SDKは保持されている。
`D:/NeNeCommander/evidence/issue-251/cleanup-pending-completed.json`、`cleanup-confirmation.json`、
`completion.json`に実行・確認・最終状態を保持する。
自動承認の`blocked by policy`は履歴として残し、サナによる別手段の削除再試行はない。
後片付け待ちは完了した。同じ実行依頼を次の引き継ぎへ持ち越さない。
