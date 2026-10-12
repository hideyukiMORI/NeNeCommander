# 引き継ぎ書 2026年10月12日 プロバイダー機能の実装

Status: informational

NeNe Commanderサナから次回担当への引き継ぎ。
製品実装はmain `89ff1729df95255a7d556e0ff5e2686190d611b2` まで完了した。
WSLからWindowsへの転送、ごみ箱削除、WSLとUNCの関連付け起動、WSLディストリビューション間転送はすべて統合済み。
今回の文書Issue #230以外に進行中の実装はない。詳細な成果と証拠は[日報](../reports/2026-10-12-provider-features-daily-report.md)に記載した。

## 再開時に確認すること

1. `AGENTS.md`、`docs/PROJECT_STATE.md`、[ADR-0061](../adr/0061-impact-scoped-verification.md)を読む。
2. `git status --short --branch`、`git worktree list`、GitHubのopen Issue/PRを読み、今回の文書PRの統合結果も確認する。
   製品の基準は上記commitだが、文書PRによってmain SHAは進む。その差分を製品未検証と取り違えない。
3. 次の依頼の範囲をIssueに切り出し、対応するADR・provider境界を読んでから実装する。
   古い日報のDraft、CI待ち、#216未着手という記述を現在の作業へ戻さない。

調査・設計・実装・検証は現在のサナが単体で進める。過去のAstra/Sol/Luna分担は既定ではない。
追加worktreeはDドライブのプロジェクト領域に置き、Cに代替を作らない。

## 完了済みの経路と守る条件

| 経路 | 正本 | 再開時に崩してはいけない条件 |
|---|---|---|
| WSLからWindows | [ADR-0062](../adr/0062-wsl-to-windows-transfer.md)、[PR #221](https://github.com/hideyukiMORI/NeNeCommander/pull/221) | 全階層の名前・衝突を開始前に確認し、copy/verifyでも同じ型付き子パスを導出する。検証失敗時は元を削除しない |
| Windowsごみ箱 | [ADR-0063](../adr/0063-windows-recycle-boundary.md)、[PR #223](https://github.com/hideyukiMORI/NeNeCommander/pull/223) | 1つの所有STAでShell操作を完結させる。pre-deleteでidentity等を再確認し、永久削除へのfallbackを拒否する |
| WSLのファイル起動 | [ADR-0064](../adr/0064-wsl-file-shell-handoff.md)、[PR #225](https://github.com/hideyukiMORI/NeNeCommander/pull/225) | Windows互換名、260文字未満、非root。既存Shell経路へ正規パスを1回だけ渡す |
| UNCのファイル起動 | [ADR-0065](../adr/0065-unc-file-shell-handoff.md)、[PR #227](https://github.com/hideyukiMORI/NeNeCommander/pull/227) | サーバー・共有・パスを保持する。資格情報入力、再試行、drive mappingを追加しない |
| WSLから別のWSL | [ADR-0066](../adr/0066-wsl-cross-distribution-transfer.md)、[PR #229](https://github.com/hideyukiMORI/NeNeCommander/pull/229) | 既存WslFileOperationAdapterを使う。元は1ディストリビューション、各パスの所属ディストリビューションを保持し、重複targetは開始前に拒否する |

転送は `FileOperationGateway` のcopy → verify → source deleteだけで構成する。
WSL間の直接atomic move、上書き、rollback、Linuxアプリ起動、UNCの変更操作を実装済みと扱わない。
検証はkind・entry set・byte lengthの契約であり、hashやUnix metadataの同一性を保証しない。
通常のパス再openに伴う競合の限界は各ADRのまま残る。

PR #225は最初のmerge要求がHTTP 502となった際にmainへ `92789b9` が入り、その後の通常mergeで
`4d2921a` がPR完了commitになった。両commit間のtree差分は空で、最終treeと確認済みbranchの一致を確認した。
履歴を修復目的で書き換えない。経緯はPR本文と `evidence/issue-224/verification.json` に記録済み。

## 検証の再利用

文書はCI・build・製品testを開始しない。通常変更は対象機能と直接影響する境界だけを検証する。
full/deepは必要性をPRに記録して明示実行し、deepに含まれるfullをqualityで重複実行しない。
commit hookの `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit` は残る。
mainのPR・squash・review thread解消・force push禁止も維持されている。

#216はdeep全体が成功した。#222はdeep run自体がfailureで、その失敗tierを個別修正して通過した。
前者の成功と後者の組み合わせた証拠を混同しない。#222のCodeQL正規解析は結果0、追加診断には
評価済みnote 3件がある。#224の238件、#226の161件、#228の182件は対象を絞った証拠で、
現在のmain全体の新規deep成功を表さない。正確なコマンドと失敗・修正・再利用の対応は各PRと日報を参照する。

配布時には必要な環境確認とdeep-reviewの鮮度条件を別途確認する。今回の文書更新だけを理由に取り直さない。

## 残件と環境の制限

- [Issue #181](https://github.com/hideyukiMORI/NeNeCommander/issues/181)はネットワークドライブをlocal NTFS扱いしない方針。低優先度を維持する。
- [Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94)はhideがいる日に行う配布前の画面確認。
  100%または200%の画面とhigh contrastを手動で確認する。開発blockerではなく、自動で表示設定を変えない。
- WSL/UNCの実関連付けアプリ起動、UNCの実共有、実2ディストリビューション間転送は未確認。
  独立NTFS領域でのテストをWSL live証拠に読み替えない。
- UNCの変更操作、sort永続化、Locationsの共有列挙、WSLの上書き・リンク項目削除等は未実装範囲。
  次の実装Issueは未選定で、PROJECT_STATE末尾の未承認候補も残してある。

WSL liveは専用 `Ubuntu` の所有rootだけを使う。hideの作業用 `Ubuntu-22.04` を使用しない。
停止・再起動で `/tmp` が消える既知の環境条件があるため、必要なlive作業時だけ既存手順でkeepaliveを所有する。
前回の空rootが今も存在するとは仮定せず、現在状態と所有条件を確認する。別ディストリビューションを勝手に導入しない。

## 保存した証拠と保持領域

恒久証拠は `D:/NeNeCommander/evidence/` にある。各機能の `issue-216`、`issue-222`、`issue-224`、
`issue-226`、`issue-228` にログ、TRX、CodeQLやmutationの該当結果、PR記録、検証・整理の記録を収載した。
5件の追加worktreeは削除済みで、ブランチとcommitは保持した。本引き継ぎの開始時点で登録worktreeはCのmainだけだった。
文書用worktreeの最終整理はIssue #230のPR本文を参照する。

以下は自動承認レビューが再帰削除を `blocked by policy` で拒否したため保持した一時出力であり、
未完了の製品実装ではない。別の削除方法で拒否を迂回しない。

| 作業 | 保持パス |
|---|---|
| #190と当日再開 | `D:/NeNeCommander/outputs/wt-190`、`D:/NeNeCommander/outputs/resume-20261012-astra` |
| #216 | `D:/NeNeCommander/outputs/issue-216` |
| #222 | `D:/NeNeCommander/outputs/issue-222` |
| #224 | `D:/NeNeCommander/outputs/issue-224`、`D:/NeNeCommander/outputs/wsl-launch-20261012` |
| #226 | `D:/NeNeCommander/outputs/issue-226`、`D:/NeNeCommander/outputs/unc-launch-20261012` |
| #228 | `D:/NeNeCommander/outputs/issue-228`、`D:/NeNeCommander/outputs/wsl-distributions-20261012` |

`outputs/wt-190/tmp` 内には、整理記録時点で10個の所有テストjunctionがある。
各targetは各所有root内だが、親から一括削除しない。詳細は
`D:/NeNeCommander/evidence/integration-213/2026-10-12/cleanup-inventory.json` に保存済み。

#222の初回診断では、ごみ箱に所有ファイル `source.txt` 21バイトを残した記録があり、回収完了は未確認。
元rootは `D:/NeNeCommander/outputs/issue-222/tmp/NeNeCommander-Test-jeztfdgu.nxs`。
正確なreceiptは取得できていない。hideのごみ箱の列挙・一括削除で探さず、残留記録を保持する。
後続のlive成功と、この初回残留物の未解消を混同しない。
