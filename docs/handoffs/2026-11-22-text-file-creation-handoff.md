# 2026-11-22 引き継ぎ — 新規テキストファイル作成の設計

Issue #544、docs/544-text-file-creation-design、main7ef9b96起点。
D:/NeNeCommander/worktrees/issue-544。
[提案ADR-0085](../adr/0085-create-empty-text-file.md)、
[品質記録](../quality/2026-11-22-text-file-creation-design.md)、
[報告](../reports/2026-11-22-text-file-creation-report.md)を読む。
ADR-0084は#547に割当済み。0085はまだproposed。独立読み取りレビューを依頼済み。
製品変更・build/test/CI・外部write・main統合は行っていない。

永久証拠 D:/NeNeCommander/evidence/text-file-creation-20261122。
22実装入力のmanifestと3probe、最初のCS8632 compile失敗を保存済み。
通常BCL CreateNewはdangling finalsymlinkをたどる: 9pass/1fail/親link residual1。
native CREATE_NEW+OPEN_REPARSE_POINTは10pass/親link residual1。
追加6境界はpass、255文字leaf/561文字path成功、256leaf native123拒否。
既存directory/後発directoryはnative5。全access-deniedをconflict扱いしない。
script exit0はBCL試験のall-passを意味しない。原結果を変更しない。

不変条件は、frozen locationのdirect childへzero-byteを一度だけ作り、overwrite/
link-follow/delete-on-cancel/retryをしないこと。既存name entry/gateway/router/scheduler/
refreshを伸ばす。新provider stepとeffectの全consumerを原子的に移行し、ADV負例、
初期名resource化、Ctrl+F7、F7/F2保持、palette/help、mouse入口の#530/#531を確認する。
排他no-followはfinal componentだけ。既存の親検査～path reopen raceは隠さない。
通常I/O schedulerはadmitted atomic stepをcancelしないことも契約へ記録済み。

次は技術レビューの必須修正とprovider scopeの受理、実装/因果負例/対象build/tests。
新共有port/effectとnative mutationなので統合前に既存deep-review一回が必要と提案した。
WSL opt-in root環境変数は現在未指定。未試験WSLを対応済みとして扱わない。
外部送信/公開承認は#542/#547のpendingを迂回しない。native key/focusには隔離desktopが必要。

未完の試験領域 D:/NeNeCommander/outputs/text-file-creation-20261122/fixture-01,
fixture-02,fixture-03 は保持する。実link/readonly/hidden/長いpathがあり、
link先はすべて各fixture内。後の削除前に絶対path・唯一成果物・稼働参照・linkを検査し、
成果物保全/受理/main統合後のみ専用worktree/outputを整理、branch/commit/evidenceは残す。
現在後片付け拒否なし。hideの継続指示は有効、ユーザーApp/settings/他の作業は保持する。

20:42更新: 独立レビューの2点（Windows+WSL全体を完成条件、late cancelはSucceeded）を修正し、
追加必須修正なしを得た。ADR-0085を技術受理。機能の実装/製品受理/出荷ではない。
同じworktreeでfeat/544-create-empty-text-fileへ進む。docs branch/commitを保持する。
最初の22hashは調査時baseline。製品編集後に同じ証拠をfinal proofとは扱わない。
