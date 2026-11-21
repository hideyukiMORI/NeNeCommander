# 2026-11-21 表示一覧の所有権修正・引き継ぎ

hideは11月21日に停止指示までの自走を依頼。単体実行を維持する。
現在のIssue #508は、PaneState.SelectVisibleの公開snapshot所有権を修正する。

- 製品差分はPaneState.csのみ。private VisibleEntrySnapshotが同じfilterの結果を一度だけ所有する。
- public IReadOnlyListとgeneric collectionの読み取り/コピー、順序・focus・選択・履歴は維持。
  宣言API外のReadOnlyCollection/IList/non-generic ICollection castは非対応になる。
- 修正前9件中4件が狙った内容不一致で失敗。修正後Application139/Presentation36成功、
  対象Release build警告/error0、変更format成功。既存CS-013は同じ26件。
- 正確な範囲・失敗・コマンドは[品質記録](../quality/2026-11-21-visible-entry-ownership.md)。
  他snapshot全般、UI速度、provider安全境界の新しい保証へ広げない。
- #508を閉じるPRと`D:/NeNeCommander/evidence/visible-entry-ownership-20261121/completion.json`
  から実際の統合・main同期・全保全・整理結果を読む。成功検証だけで統合完了を推定しない。
- 作業木`D:/NeNeCommander/worktrees/issue-508`、出力`D:/NeNeCommander/outputs/issue-508`は、
  受理/統合/clean/ignored/固有成果物/稼働参照/リンクと恒久保全を確認してから限定整理する。
  branch/commitと恒久evidenceを保持する。

PR402のhide画面受理、#491の新しい比較計画、#453の体感待ち時間、AppSDK PR498は別件。
停止済みの2計測系列を続行しない。公開beta.2と通常Appの差替えは本修正に含めない。

旧#454/#459の4cleanup対象は11月21日の読取時にすべて不在。
既存cleanup-result.jsonは11月20日23:53:59/23:56:26の完了を記録している。
過去の「hide実行待ち」を再依頼しない。設定は428B/hash一致、App processなしを確認した。
