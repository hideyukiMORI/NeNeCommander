# 日報 2026年10月12日 プロバイダー機能の実装

Status: informational

NeNe Commanderサナが、hideの継続指示に基づいて5件の機能を実装し、mainへ統合した。
製品コードの到達点は `89ff1729df95255a7d556e0ff5e2686190d611b2`。
記録開始時のローカルmainはorigin/mainと一致し、未コミット変更と未完了PRはない。
この日報と[引き継ぎ書](../handoffs/2026-10-12-provider-features-handoff.md)はIssue #230の文書変更である。

## 本日の完了事項

| 対応 | 統合先 | mainのcommit | 統合日時 JST |
|---|---|---|---|
| WSLからWindowsへのコピーと移動、ADR-0062 | [Issue #216 / PR #221](https://github.com/hideyukiMORI/NeNeCommander/pull/221) | `09cfe5f` | 19:02:32 |
| Windowsのごみ箱削除、ADR-0063 | [Issue #222 / PR #223](https://github.com/hideyukiMORI/NeNeCommander/pull/223) | `3243b7f` | 21:12:37 |
| WSLファイルのWindows関連付け起動、ADR-0064 | [Issue #224 / PR #225](https://github.com/hideyukiMORI/NeNeCommander/pull/225) | `4d2921a` | 21:42:13 |
| UNC共有ファイルの関連付け起動、ADR-0065 | [Issue #226 / PR #227](https://github.com/hideyukiMORI/NeNeCommander/pull/227) | `31c6d31` | 22:12:03 |
| WSLディストリビューション間のコピーと移動、ADR-0066 | [Issue #228 / PR #229](https://github.com/hideyukiMORI/NeNeCommander/pull/229) | `89ff172` | 22:37:59 |

転送は既存の `FileOperationGateway` に集約したまま、コピーと検証が成功した場合だけ
移動元を削除する。WSLからWindowsへの転送は、Windowsで表現できない名前、既存の転送先、
大文字小文字の衝突を開始前に拒否する。WSL間転送は各パスのディストリビューションを保持し、
1バッチの元項目を1ディストリビューションに限定する。

Windowsの通常削除は、適格な項目について既存の削除経路からShellのごみ箱操作を行う。
永久削除への自動切り替えは許さず、失敗やキャンセルより前に確認できた効果を保持する。
WSLとUNCのファイル起動は既存の `IFileLauncher` を使い、正規パスが260文字未満であること、
WSLではWindows互換の名前であることを条件にする。Linuxアプリの起動は追加していない。

同日先行分のWSL末尾ドット修正は[PR #217](https://github.com/hideyukiMORI/NeNeCommander/pull/217)で
`3e148bd`へ統合済み。検証範囲の見直しは[PR #219](https://github.com/hideyukiMORI/NeNeCommander/pull/219)、
その文書記録は[PR #220](https://github.com/hideyukiMORI/NeNeCommander/pull/220)で完了した。
[前の日報](2026-10-12-verification-policy-daily-report.md)に残るclosure待ち・#216未着手の記述は、
その記録時点の状態である。

## 検証結果

| 対応 | 完了した検証 | 証拠の適用範囲 |
|---|---|---|
| #216 | deep-review `38228563355`成功。通常テスト1338件成功、専用live 13件skip。coverage 100.00 / 100.00 / 92.66 / 94.06、mutation 95.52 / 96.10 / 90.00 / 95.11 | 専用Ubuntuでは選択10件成功。長いパスの修正後に逆方向copy/move 2件を再確認し、それ以外は入力不変の証拠を再利用 |
| #222 | full gateは1415件成功、専用live 16件skip。coverage 100.00 / 100.00 / 93.08 / 94.06。所有fixtureのごみ箱投入・復元・永久削除拒否のlive 3件成功 | deep全体の失敗と、後述する個別tier修正を区別して受理 |
| #224 | Release build成功。対象238件成功、skipなし | Domain 100、Application 79、Infrastructure 29、Presentation 28、Architecture 2。後のテスト規約修正に関係する19件だけ再確認 |
| #226 | Release build成功。対象161件成功、skipなし | Application 83、Infrastructure 47、Presentation 29、Architecture 2。Domainの変更はなく、#224の証拠を再利用 |
| #228 | Release build成功。対象182件に成功証拠あり、skipなし | Infrastructure 134、Application 42、Domain 4、Architecture 2。旧仕様の期待値1件を修正して単独再確認し、成功済み133件を再利用 |

coverageとmutationの並びはDomain、Application、Infrastructure.Windows、Presentation.WinUI。
各Release buildは警告・エラー0件。各PRに対象選定理由、正確なコマンド、ログと再利用条件を記録した。
成功数は機能ごとの選択範囲であり、現在のmainを全件再実行した合計ではない。

### ごみ箱対応の失敗と修正

#222のdeep run `38241438752` はInfrastructure mutationが87.91%で止まり、全体の結論はfailureのまま。
境界テストを追加した最終製品・テストhead `242767c` でInfrastructureを90.06%まで修正し、
未到達だったPresentationも個別に95.02%で通過した。既に成功したDomain 95.52%、Application 96.16%、
full gate、他の不変tierは再実行していない。全体成功へ読み替える記録は作っていない。

#216のCodeQL analysis `1935312766` は結果0、error/warningなし、open branch alert 0。
#222の正規buildless analysis `1936018863` は164 rulesで結果0、open branch alert 0。
追加のcompiler-fed診断は634/634ファイルを抽出し、抽出error/warningなしだったが、
native呼び出しに関するnote 3件がある。評価はADR-0063に記録し、raw SARIFも保持している。

### 未実施の環境確認

#224と#226の実際の関連付けアプリ起動、#226の実共有での確認、#228の実際の2ディストリビューション間転送は未実施。
#228は独立した2つの所有NTFS領域で実ファイルのcopy・verify・deleteを確認したが、9Pの実機証拠ではない。
[Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94)の配布前画面確認も残る。

## 現在の運用と整理状況

検証は[ADR-0061](../adr/0061-impact-scoped-verification.md)に従う。文書変更はCI・製品テストなし、
通常変更は影響範囲に限定し、full/deepは必要性を記録して明示実行する。成功済み証拠を工程変更だけで取り直さない。
この5件はNeNe Commanderサナが単体で実装・検証した。過去の分担指定を次の作業の既定にしない。

5件の追加worktreeは削除済みで、ブランチとcommitは保持した。必要証拠は
`D:/NeNeCommander/evidence/issue-216/`、`issue-222/`、`issue-224/`、`issue-226/`、`issue-228/` に収載済み。
一時出力は自動承認レビューが再帰削除を `blocked by policy` で拒否したため保持している。
正確なパス、古い作業領域のjunction、診断残留物は引き継ぎ書にまとめた。

初回のごみ箱診断で所有テストファイル `source.txt` 21バイトを残した記録があり、回収完了は確認していない。
hideのごみ箱を列挙・一括削除していない。成功した最終live 3件のfixtureは復元・片付けまで完了した。

## 次回へ残す作業

開いているIssueは#181と#94で、次の実装Issueは未選定。#181のネットワークドライブ方針は低優先度を維持する。
UNCの変更操作、WSLの未実装capability、sort永続化、Locationsの共有列挙などは、それぞれの範囲を決めてから扱う。
完了済みの5件を再開しない。詳細な再開手順と保持領域は[引き継ぎ書](../handoffs/2026-10-12-provider-features-handoff.md)を参照する。

今回の文書変更はリンク・文字コード・差分とCommit modeだけを確認する。
その最終結果、文書統合後のmain SHA、文書用worktreeの削除結果はIssue #230のPR本文を参照する。
