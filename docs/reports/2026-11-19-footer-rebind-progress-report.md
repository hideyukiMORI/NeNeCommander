# 2026-11-19 フッター再bind削減・進捗報告

Issue #467で、内容・順序が同じフッターキー案内の参照を再利用し、画面側の不要なItemsSource再設定を
避ける修正を完了した。正典キー表・表示設定・F1/入力案内・進捗・provider/安全確認は維持する。
変更は製品2ファイルと関連試験2ファイル。詳細と全試行・制約は
[品質記録](../quality/2026-11-19-footer-hint-rebinding.md)にまとめた。

- 変更前は3つの参照同一性試験が期待どおり失敗。変更後はPresentation46件、画面所有18件成功。
  App/試験buildは警告・エラー0。4ファイルのformat exit0。Complexityは既存26指摘のまま、新規なし。
- 同じRelease構成、各1回の準備運転後に2回ずつ測定。全4回で欠落0、対象PIDのみ、全区間対応。
  最初の2秒に始まるApplyTemplate区間は689/689から357/357、最長Frameは36.2998/35.7001msから
  14.4743/14.4317ms。各2回の観測であり、F6全体の待ち時間や原因の確定ではない。#453は未完了。
- 案内の表示/非表示、F1保持、名前入力Enter/Escと戻り、配色追従を専用窓で確認。
  追加F8確認ではWindowsの既存recycle方針により専用SQL1個がごみ箱へ移った。これは確認画面の
  成功証拠ではない。changed-02の移動後SQLハッシュ未取得を明記し、他の完全照合済み試行と区別した。
- 専用app/traceは終了、通常PID38360は継続。元設定218BはSHA256まで復元。公開beta.2は変更なし。
- 恒久証拠`D:/NeNeCommander/evidence/footer-rebind-20261119/`に、全73 outputファイル
  /6,592,842Bのsize/hash一致を確認して保存した。追加worktree/outputは統合後に整理する。

ADR-0061の限定範囲を選択し、全件/深いCIは追加しない。通常commit hookと文書リンク/diff確認を行う。
実装を変更しない限り成功した検証を再利用する。統合と整理の最終結果は
[引き継ぎ](../handoffs/2026-11-19-footer-rebind-handoff.md)に追記する。
PR402のBookmarkは画面受理待ちのDraft、公開Previewへの取込みは別。hideの停止まで継続する。

## ! hide実行待ち：後片付け

次の4対象は既に自動承認レビューで起動前に`blocked by policy`となった削除。今回は再試行していない。
証拠・branch/commit・公式candidate・通常アプリ・未完了Bookmarkは保持する。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
