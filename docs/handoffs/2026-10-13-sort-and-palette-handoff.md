# 引き継ぎ書 2026年10月13日 並び順保存とパレット拡張

Status: informational

NeNe Commanderサナから次回担当への引き継ぎ。
左右ペインの並び順保存、パレットへのブックマーク・ウィンドウ調整の追加、後片付けの引き継ぎ手順はmainに統合済み。
製品の基準は `e7439be1c039fe44d7d46bd1268448ecbff012db` で、記録開始時はmainとorigin/mainが一致し、作業ツリーはcleanだった。
進行中の実装とopen PRはなく、今回の[Issue #238](https://github.com/hideyukiMORI/NeNeCommander/issues/238)は文書だけを扱う。
統合時刻と検証の詳細は[日報](../reports/2026-10-13-sort-and-palette-daily-report.md)を参照する。

## 再開手順

1. `AGENTS.md`、`docs/PROJECT_STATE.md`、[ADR-0061](../adr/0061-impact-scoped-verification.md)を読み、現在のhideの指示を確認する。
2. `git status --short --branch`、`git worktree list --porcelain`、GitHubのopen Issue/PRとIssue #238を閉じるPRを確認する。
   文書統合でmain SHAが進んでも、製品差分がなければ既存の検証結果を再利用する。
3. 末尾の `outputs/issue-236` が削除されたか読み取り専用で確認する。既知の削除コマンドが実行中なら重複依頼しない。
4. 次の依頼を1つのIssueに切り出す。並び順保存と今回のパレット2項目は完了済みとして扱う。

10月13日のこのセッションでは、hideが明示したAstra設計・Sol/Luna背景作業の分担に従った。
次回の既定は現在のサナによる単体実行であり、このセッションの分担を無条件に引き継がない。
追加worktreeは `D:/NeNeCommander/worktrees/<task-id>`、一時出力は `D:/NeNeCommander/outputs/<task-id>` に置く。
文書のみなら本文・ログを直接恒久証拠へ保存し、不要な出力ディレクトリを増やさない。

## 完了した経路と不変条件

| 機能 | 正本と統合 | 守る条件 |
|---|---|---|
| 並び順の保存と復元 | [ADR-0067](../adr/0067-persist-pane-sort-order.md)、[PR #233](https://github.com/hideyukiMORI/NeNeCommander/pull/233)、`88f2bd2` | 左右独立の確定結果だけを既存の完全設定キューで保存する。初回読取失敗でも起動時の並び順を失わず、最初の成功表示に適用する |
| パレットから既存scopeを開く | [ADR-0068](../adr/0068-palette-scope-entry-commands.md)、[PR #237](https://github.com/hideyukiMORI/NeNeCommander/pull/237)、`e7439be` | 既存20項目の後に2項目を追加する。両endpointを検証し、パレットを閉じてから既存idle経路で1回だけ開く |
| 削除保留の報告 | [開発手順](../DEVELOPMENT_WORKFLOW.md#blocked-cleanup-and-daily-reports)、[PR #235](https://github.com/hideyukiMORI/NeNeCommander/pull/235)、`52437ad` | 正確な対象・証拠・拒否理由と実行用の一行を日報末尾へ載せる。完了確認まで引き継ぎ、拒否を迂回しない |

並び順は `PaneSortPreferences` と `PaneSessionConfiguration` が起動時の左右値を供給し、
成功表示後の現在値は `PaneState.SortOrder` が所有する。`PaneReducer` と `EntryOrdering` が並べ替えを所有し、
`CommanderSession` のsort専用条件が受理した変更だけを `SettingsSession` へ渡す。
schema 3の変換は既存settings境界内の `SettingsDocumentSerializer`・`SettingsSortOrderCodec` が扱い、
`WindowsLocalSettingsStore` が既存のI/O・atomic publishを所有する。二重の保存機構を作らない。

schema 1・2は読み取りだけでは書き換えず、次の受理された設定変更で3を書く。
新しいorderオブジェクトもmissing・duplicate・unknown・型違いを全体拒否し、破損ファイルを自動修復しない。
他の設定とブックマーク、反対側の並び順を保持する。保存失敗時もペインの並び順は戻さず、終了時は既存キューをdrainする。
`Ctrl+H`のその場の表示切替を永続設定へ変更していない。

パレットの製品変更は `CommandCatalog.cs`、`CommandLabelCatalog.cs`、en-US/ja-JPの `Resources.resw` の4ファイル。
既存の `CommandPaletteSession` → `CommanderSession` → ブックマーク管理またはウィンドウscopeを使う。
ブックマークの9スロット、個々のウィンドウ操作、パラメーター付き送信はカタログに入れない。
パレット表示中の直接入力による `OpenBookmarks`・`OpenWindowAdjustment` は引き続き無視し、検証付き送信だけを受理する。
表示ラベルとショートカット検索、UIA ID、Enter連打の保護は既存projectionとmapperに従う。

本日の変更全体は各PRのFiles changedに収載されている。今回の文書変更はこの引き継ぎ書、日報、
`docs/PROJECT_STATE.md` の3ファイルだけで、製品・テスト・依存関係・実行ツールは変更しない。

## 再利用する検証

| 対応 | 成功した証拠 | 恒久記録 |
|---|---|---|
| #232 | Release build警告・エラー0、選択361件成功・失敗skip 0、35 C#のformat、Commit 112規約・23脅威 | `D:/NeNeCommander/evidence/issue-232/` |
| #236 | 対象Release build、選択56件成功・失敗skip 0、7 C#のformat、実2言語のresource確認、Commit 112規約・23脅威 | `D:/NeNeCommander/evidence/issue-236/` |
| #234 | 文書確認とCommit mode。製品test・CIは未実行 | `D:/NeNeCommander/evidence/cleanup-reporting-policy-20261013/` |

両機能の `verification/verification.md` と `verification/test-commands.json` に正確なコマンドとfilterがある。
#232は左右の全8order、初回readの失敗後復元、保存順序、他設定保持、停止時drain、ADV-012を含む。
#236は両active側、空の一覧、古い送信・再送・両captured endpoint変更の拒否、状態保持、ADV-013を含む。
実画面のfocus成功とは読み替えない。

#236のsolution build全体は成功と記録しない。最初はrestore待機不足によるNETSDK1004、
次は新fixtureのIDE0305で失敗した。そのfixture修正後のApplication.Tests buildは成功し、
既に成功した不変の製品・Presentation側のbuildを再利用した。失敗ログも恒久領域に残っている。
#232の最終solution buildは成功した。両機能とも製品テストの成功済みケースを再実行していない。

今回の文書に適用するゲートは、通常のcommit hookの次のコマンドだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit
```

文書のUTF-8 BOMなし・CRLF・相対リンク・差分を別途確認する。最終exit結果はIssue #238のPR本文と
`D:/NeNeCommander/evidence/session-closure-20261013/` に残す。full/deep CI、build、製品testは開始しない。

今回の361件・56件は現在のmain全体のdeep成功を表さない。以前の#222のdeep runは全体failureのままで、
失敗したInfrastructure mutationを個別修正し、未到達のPresentation tierも別途通過させた証拠で受理済み。
その経緯は[10月12日の記録](../reports/2026-10-12-provider-features-daily-report.md)と各PRを参照し、全体successへ読み替えない。
配布時の環境確認とdeep-review鮮度条件は別途確認する。

## 残件と環境条件

- [Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94)はhideがいる日に100%または200%表示とhigh contrastを手動確認する配布前作業。開発blockerではなく、表示設定を自動変更しない。
- [Issue #181](https://github.com/hideyukiMORI/NeNeCommander/issues/181)は割り当てネットワークドライブをlocal NTFS扱いしない対応。低優先度を維持する。
- UNC変更操作、Locationsの共有列挙、WSLの上書き・リンク項目削除などは未実装。次の実装Issueは未選定。
- WSL/UNCの実関連付け起動、UNCの実共有、実2ディストリビューション間転送は未確認。所有NTFS fixtureの成功を9P環境の成功へ読み替えない。
- `CommanderSession` は289/300論理行。次の一時scope追加前に責務分割の必要性を確認する。その他の候補はPROJECT_STATEのNext focused workに残る。

WSL liveは専用 `Ubuntu` の所有rootを使う。hideの作業用 `Ubuntu-22.04` は使わない。
停止・再起動で `/tmp` が消える既知の条件があるため、必要なlive作業時だけ既存手順でkeepaliveを所有し、
rootの現在状態と所有条件を確認する。今回の作業はWSL・表示設定・ユーザーの設定ファイルへ操作していない。

## 整理完了と保持物

以前の13出力ディレクトリと#222の所有ごみ箱21バイト項目は、hideが22:48 JSTに削除し、不在確認まで完了した。
完了記録は `D:/NeNeCommander/evidence/cleanup-20261013/completion-confirmed.json`。
10月12日までの報告にあるこれらの削除待ち・receipt未特定は過去の状態であり、再開対象に戻さない。

#232・#234・#236のworktreeは削除済み。各ブランチとcommit、必要証拠は保持した。
今回の文書worktree `D:/NeNeCommander/worktrees/issue-238` の最終削除結果はIssue #238のPR本文を確認する。
文書の本文・ログは `D:/NeNeCommander/evidence/session-closure-20261013/` に直接置き、新しい一時出力を作らない。

## ! hide実行待ち：後片付け

現在の削除待ちは `D:/NeNeCommander/outputs/issue-236` の1ディレクトリだけ。
18個の検証ファイルは `D:/NeNeCommander/evidence/issue-236/verification/` にhash一致で収載済み。
レビュー、差分、PR記録、入力hash、整理監査も `evidence/issue-236/` に保存している。
この文書の作成時に対象の存在を再確認し、リンク・稼働中プロセスの参照がないことを確認した。
恒久証拠、main、ブランチ、commitは対象外。tmpは生成物である。

hideに渡すPowerShell一行は次のとおり。削除済みなら何もしない。

```powershell
if (Test-Path -LiteralPath 'D:\NeNeCommander\outputs\issue-236') { Remove-Item -LiteralPath 'D:\NeNeCommander\outputs\issue-236' -Recurse -Force }
```

自動承認レビューが対象の再帰削除を `blocked by policy` で拒否し、詳細な理由は返していない。
拒否されたコマンドは未実行で、別方式で再試行していない。hideの実行報告と完了結果、または読み取り専用の不在確認まで保留を維持する。
実行中と分かっている間は同じ削除依頼を重ねない。
