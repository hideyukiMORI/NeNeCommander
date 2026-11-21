# 2026-11-21 ディレクトリ一覧の所有権修正・引き継ぎ

hideは11月21日に停止指示までの自走を依頼。単体実行を維持する。

- #510はDirectoryListing/EntryOrdering公開結果のSyncRoot経由の変更を閉じる。
  元listingとVisibleEntriesの不一致で起動対象が見つからなくなる7ケースを修正前に再現。
- #508のprivate VisibleEntrySnapshotを内部DirectoryEntrySnapshotへ移し、旧実装を削除。
  DirectoryListing.Create/EntryOrdering.Apply/PaneState.SelectVisibleの3ownerから使う。
  汎用化や別経路は増やさず、検証・並べ替え・hidden判定とprivate入力のcopy方針は維持。
- 宣言IReadOnlyList、generic read/copyとentry/path参照を保つ。
  ReadOnlyCollection/generic IList/non-generic ICollection/IList castの差分は品質記録に明記。
- Application212+起動14、Presentation36、Infrastructure reader67成功。Release build警告/error0。
  変更format成功、既存CS-013全26finding一致。1,007入力を固定して成功証拠を再利用する。
- [品質記録](../quality/2026-11-21-directory-entry-snapshots.md)と恒久evidence
  `D:/NeNeCommander/evidence/entry-snapshots-20261121`が正確なコマンド・結果・限界を保持。
- #510を閉じるPRと`completion.json`から実際の統合・main同期・保全・整理結果を読む。
  対象は`D:/NeNeCommander/worktrees/issue-510`と`D:/NeNeCommander/outputs/issue-510`のみ。
  受理/取込/clean/untracked/ignored/唯一成果物/稼働参照/リンク/恒久保全を確認して限定整理し、
  branch/commit/evidenceを保持する。未確認の完了を推定しない。

#508/PR509はmain f0f7b31へ通常統合、700成果物を全件保全・検証して専用作業物を整理済み。
PR402のhide画面受理、#491の新しい比較計画、#453の体感待ち時間、AppSDK PR498は別件。
停止済みの2計測系列を続行・合算しない。公開beta.2と通常Appの差替えは含めない。
旧#454/#459の4cleanup対象は11月21日に不在確認済み。過去の実行待ちを再依頼しない。
