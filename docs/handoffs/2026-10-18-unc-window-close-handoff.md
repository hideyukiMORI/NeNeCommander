# 引き継ぎ 2026年10月18日 UNC read中の実WinUI close

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、今回の範囲と結果は
[日報](../reports/2026-10-18-unc-window-close-daily-report.md)を参照する。
子[Issue #297](https://github.com/hideyukiMORI/NeNeCommander/issues/297)は
親[Issue #247](https://github.com/hideyukiMORI/NeNeCommander/issues/247)の実WinUI/終了境界を扱う。
baselineは`bafa828c863ff93046dda6352a048df2acebdd33`。

## 維持する境界

- production adapterをsource linkし、既存read/session/AsyncWorkOwner/WindowShutdownOwnerを使う独立fixture。
- system Closingのcancel、drain、Window.Closed、ProcessExit、保持したprocess handleのexitを別々に観測。
- 未放棄readはdrain対象、既にEscapeで放棄したreadは再加入させない。製品の挙動・設定・依存・gate変更なし。
- 専用NASの32項目以内の直下一覧だけを読み、切断/復旧はhideが操作。実target設定はprivate一時fileだけに置く。
- 全CommanderWindow、製品key mapping、settings drain、native/kernel内の終了、release proofは主張しない。

## 証拠と受け入れ

恒久証拠は`D:/NeNeCommander/evidence/issue-247/window-close-20261018/`。
exact source/binary 353 file、manifest、build/raw event/終了記録、起動拒否6件、未成立の理由を保存する。
接続中controlはsystem closeから正常exitまで受理済み。
最初のactiveはreadが先に終了したため待機中closeとして不受理。
NeNeFolio中断時のabandoned/active-02はreadなしで正常終了し、測定回数へ加算しない。

最終受理はcontrol、active-03、abandoned-02の3条件。
通常closeからClosedまで接続中5.080 ms、未放棄4,170.482 ms、Escape後6.161 ms。
3条件とも実system Closingをcancelし、drain→final close→Closed→ProcessExit→外部exit 0を確認した。
Escape後はClosed/ProcessExitでも実public provider Taskが未完了であり、元providerを再awaitしていない。
未放棄はprovider failureとcallerの完了を待った。`measurement-summary.json`とraw eventが正本で、
外部exit時間をClosed時間やnative call時間に読み替えない。
15:33:09 JSTにNAS復旧read成功を確認し、全測定processが終了した。network待機はない。

fixture Release buildは警告・エラー0、admission 6件成功。
#291のoffline 7件と#251の既存証拠は不変入力として参照し、再実行・新規件数への加算はしない。
通常Commit hookと文面/リンク/差分を確認し、製品test、full/deep CI、coverage/mutationは実行しない。

## 終了時の扱い

[PR #298](https://github.com/hideyukiMORI/NeNeCommander/pull/298)は15:37:08 JSTにsquash mergeされ、
#297と#247はclosed。main `0e99ec5c7f84737b706fdc9d006e389b41b176ea`の同期・clean・task tree一致を確認した。
worktree `D:/NeNeCommander/worktrees/issue-297`は状態・取込・ignored 109件・link・稼働参照を確認して削除済み。
出力436件は保存先とSHA一致359件、再生成可能73件、破棄するprivate設定4件に分類した。
`D:/NeNeCommander/outputs/issue-297`の削除は自動承認の`blocked by policy`で開始前に拒否された。
別手段の再試行は行わず、[Issue #299](https://github.com/hideyukiMORI/NeNeCommander/issues/299) /
[PR #300](https://github.com/hideyukiMORI/NeNeCommander/pull/300)で当時の保留状態を記録した。
PR #300は15:42:07 JSTに`f2d1158faf62a2e4c0238342b2ad15dabf98a5de`へsquash merge済み。
`cleanup-pending.json`と`report-closure/completion.json`は当時の履歴であり、現在の実行待ちではない。
現行契約を維持し、独自timeout/自動abandonは採用しない。
親#247も調査・実測・採否判断が揃ったものとして同じPRで完了する。
native内訳、広い環境分布、全製品compositionやreleaseは未計測の限界であり、今回へ追加しない。
将来のtimeout/native cancellation/並列度/新ownerの具体的要求は別Issue/ADRと証拠を必要とする。

## 後片付け完了

hideが15:44:58 JSTに用意済みscriptを実行し、15:45:47 JSTの読み取り専用確認で
`D:/NeNeCommander/outputs/issue-297`と`D:/NeNeCommander/worktrees/issue-297`の不在を確認した。
branch、commit、恒久証拠、共通SDKは保持されている。削除再試行や残る実行依頼はない。
現在の状態は`cleanup-executed-by-hide.json`、`cleanup-confirmation.json`、`completion.json`で確認する。

[Issue #301](https://github.com/hideyukiMORI/NeNeCommander/issues/301)の完了記録は既存checkoutの文書3件のみ。
追加worktree/output、build/test/CI、NAS測定はなく、文面/link/diffと通常Commit hookだけを確認する。
文書更新のPRと`cleanup-closure/completion.json`に最終統合・main同期を記録する。
