# 引き継ぎ 2026年10月18日 ブックマークの型分岐整理

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、[ADR-0041](../adr/0041-category-bookmarks-through-settings-and-navigation.md)、
[日報](../reports/2026-10-18-bookmark-variant-dispatch-daily-report.md)。
hideの「うん、次に進めて」によりIssue #248を限定refactorとして扱った。

## 維持する判断

- 既存`BookmarkEditorSession`が唯一のowner。ApplyのCancelActionを明示し、cast-only `ApplyCancel`を削除した。
- ResolveFilterはbuilt-inを維持し、UserCategoryをtype patternで確認して既存catalogへrebindする。
- 未知action/filterは`InvalidOperationException`でstate不変。旧実装も`InvalidCastException`で拒否していた。
  既存の通常操作に不具合や脆弱性を見つけたとの主張ではない。
- 14 action/3 filterの動作、pending freeze、nested Cancel、stale category、rename/delete後filterを維持する。
- 新API・owner・schema・依存・gate・ADRはない。type hierarchyの言語上の閉鎖を強化したものでもない。

## 検証の読み方

元productionの新2ケースREDは2件失敗、exit 2で、どちらも旧InvalidCastExceptionが原因。
最終34件は成功、失敗・skip0。owner31、null guard1、settings直接consumer2である。
対象Application Release buildは警告・エラー0、変更4 C# formatはexit0・警告出力なし。
restoreは依存不変のためRED時の成功を再利用した。

未知variantテストはfriend test assemblyの専用派生型を使い、例外型とCurrent参照不変を確認する。
既存Cancelテストは3つのnested stateから同じbrowse contextとStateChangedへ戻ることを確認する。
RED後に未到達だったmessage一致assertを除き、Cancel transition assertを強化した。RED自体は再実行していない。
source差と再利用根拠はinventoryに残している。

恒久証拠は`D:/NeNeCommander/evidence/issue-248/`。
`design-and-verification-plan.md`、`run-selected.ps1`、stage別JSON/raw、RED/final sourceとhash/patch、
`implementation-inventory.json`、rootのレビュー/commit記録を日報と照合する。
full/deep CI、全bookmarks/settings回帰、coverage/mutation、live UI/provider、#94は未実行。
文書・push・mergeだけで成功した製品検証を再実行しない。

## 次回の入口

1. 現行hide指示、AGENTS、PROJECT_STATE、実際のPRと`completion.json`を確認する。
   [PR #284](https://github.com/hideyukiMORI/NeNeCommander/pull/284)は13:14:50 JSTに
   `8c68c60127f89953a364ecad80a8f3e30d96fa75`へsquash統合済み。main同期・cleanと受理taskとのtree一致を確認した。
   Issue #248はclosed。古いP4「記録のみ」から未着手に戻さず、統合と整理を分けて読む。
2. branchは`refactor/248-bookmark-variant-dispatch`。作業copyは`D:/NeNeCommander/worktrees/issue-248`、
   一時outputは`D:/NeNeCommander/outputs/issue-248`。worktreeは削除済みで、outputだけ自動承認拒否により残る。
   削除確認済みの対象を再削除しない。branch・commit・恒久証拠・共有SDKは保持する。
3. #244/#245は統合・整理済み。残件は#246の自然順sort、#247のUNC同期列挙待機、#94の手動release確認。
   本件から新しいsort仕様や実環境計測の成功を推測しない。

## ! hide実行待ち：後片付け

対象は`D:/NeNeCommander/outputs/issue-248`だけ。中身はcompiler/analyzer cache 21 DLLで、唯一の証拠は残っていない。
385 ignored bin/obj、未保存物、取込、link、稼働参照を確認した。
taskのidle compiler/MSBuild serverは`dotnet build-server shutdown --vbcscompiler --msbuild`で正常終了し、
観測されたmodule参照も解放された。初回/終了後preflightとshutdown記録を恒久証拠に残す。
出力の再帰削除は自動承認レビューが`blocked by policy`で拒否した。commandは開始されず、サナによる再試行はない。
13:17:18 JSTの読み取り確認では出力が残り、worktreeは不在だった。
恒久証拠`D:/NeNeCommander/evidence/issue-248/`、branch・commit・共有SDKを保持する。
Issue #285がこの保留記録を扱う。準備済みscriptは構文確認のみで、サナは実行していない。
hideの実行と完了確認まではこの記録を引き継ぐ。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-248/cleanup-completed-output.ps1
```
