# 引き継ぎ書 2026年10月16日 検証済み移動元の削除

Status: informational

[日報](../reports/2026-10-16-verified-move-daily-report.md)と
[ADR-0069](../adr/0069-verified-move-source-deletion.md)が今回の契約・証拠の正本。
Issue #243 / PR #249は実装と対象検証が完了し、最終deep-reviewと統合が残る。
古い引き継ぎの「再現のみ」はこの実装開始で更新するが、最終受理を先取りしない。

## 再開時に確認すること

1. 現在のhide指示、`AGENTS.md`、`PROJECT_STATE.md`とGitHubのPR実態を確認する。
2. `fix/243-verified-move-source`の最新treeと恒久evidenceを読み、未完了の検証だけ進める。
3. 外側ancestorと全surviving directoryのfull identityを維持する。object成分だけへの弱体化や
   任意のbaseline更新を行わず、ADR-0069の直接parent再取得条件を守る。
4. 選択したdeep-reviewの実行結果、CodeQL解析とopen alertsを照合してから技術受理する。
5. mainへのsquash merge後、通常の取込・成果物・稼働参照監査を経て追加worktree/outputを整理する。

## 続く別Issue

- #250: 新storeが既存の残留tmpに阻害されるredを先に作る。foreign tmpの無条件削除は禁止。
- #251: 通常close、OS終了、kill/crash、取消、書込失敗の保証・非保証を既存shutdown ownerで定める。
- #244〜#248: 前回登録したP4。#94の手動表示確認、#181のmapped-drive調査も別作業。

今回hideが明示した分担はこの作業の範囲で適用した。次回は最新の指示と適用範囲を再確認する。
作業領域は`D:/NeNeCommander/worktrees/issue-243`、一時出力は`D:/NeNeCommander/outputs/issue-243`。
未完了のため保持し、branch/commit/恒久evidence/固定SDKも残す。
過去のcleanup待ちを復活させない。新たな自動承認拒否が起きた場合だけ、正確な対象と
hide用の一行commandを日報・引き継ぎの末尾へ記録する。
