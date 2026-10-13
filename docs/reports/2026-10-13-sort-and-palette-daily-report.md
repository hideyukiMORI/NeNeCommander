# 日報 2026年10月13日 並び順保存とパレット拡張

Status: informational

NeNe Commanderサナが、hideの継続指示に基づいて左右ペインの並び順保存とパレットの検索対象追加をmainへ統合した。
後片付け待ちを日報で明示する運用も統合し、以前から残っていた13出力ディレクトリと所有ごみ箱項目はhideが削除した。
現在の製品到達点は `e7439be1c039fe44d7d46bd1268448ecbff012db`。
記録開始時のmainはorigin/mainと一致し、未コミット変更とopen PRはない。
この日報と[引き継ぎ書](../handoffs/2026-10-13-sort-and-palette-handoff.md)は[Issue #238](https://github.com/hideyukiMORI/NeNeCommander/issues/238)の文書変更である。

## 本日の完了事項

| 対応 | 統合先 | mainのcommit | 統合日時 JST |
|---|---|---|---|
| 左右ペインの並び順保存、ADR-0067 | [Issue #232 / PR #233](https://github.com/hideyukiMORI/NeNeCommander/pull/233) | `88f2bd2` | 21:47:05 |
| 後片付け待ちを日報で引き継ぐ手順 | [Issue #234 / PR #235](https://github.com/hideyukiMORI/NeNeCommander/pull/235) | `52437ad` | 22:39:53 |
| パレットからブックマークとウィンドウ調整を開く、ADR-0068 | [Issue #236 / PR #237](https://github.com/hideyukiMORI/NeNeCommander/pull/237) | `e7439be` | 23:28:00 |

並び順は名前・拡張子・サイズ・更新日時と昇順・降順を、左右独立で保存する。
起動後の最初の一覧表示から復元し、実際に受理した並び順変更だけを既存の設定保存キューへ渡す。
設定schema 3は他の設定とブックマークを保持する。schema 1・2の読み込みは既定の並び順をメモリー上で補い、
読み込みだけではファイルを書き換えない。破損設定は保存時も保護し、保存失敗で表示中の並び順を戻さない。

`Ctrl+P`の検索対象には「ブックマーク」と「ウィンドウ調整」を追加した。
既存20項目の順序とLocationsを保ち、末尾に2項目を加えた22項目となる。
表示は日本語・英語に対応し、ショートカットは既存の `Ctrl+B` と `Ctrl+W` を示す。
パレットの状態検証を通過した後、既存のブックマーク管理またはウィンドウ調整モードを1回だけ開く。
開くだけでファイル操作、設定保存、ペイン移動、ウィンドウ配置変更は発生しない。

後片付けの運用は[開発手順](../DEVELOPMENT_WORKFLOW.md#blocked-cleanup-and-daily-reports)に集約した。
自動承認レビューに削除を拒否された場合、日報末尾の `! hide実行待ち：後片付け` に対象と実行用の一行を載せる。
削除完了の確認までは引き継ぎに残し、拒否された削除を別手段で再試行しない。

## 検証結果

| 対応 | 完了した検証 | 適用範囲 |
|---|---|---|
| #232 | Release build成功、警告・エラー0。対象361件成功、失敗・skip 0 | Application 219、Infrastructure 132、Presentation 8、Architecture/lifecycle 2。ADV-012、設定schema、保存キュー、初回表示、パレット経由を含む |
| #234 | 文書3ファイルのリンク・文字コード・改行・差分確認、Commit mode成功 | 文書のみ。build・製品test・CIなし |
| #236 | 対象Release構成のビルド成功。対象56件成功、失敗・skip 0 | Application 35、Presentation 21。両側ペイン、空一覧、状態保持、古い送信・再送拒否、ADV-013 Enter連打保護を含む |

#232は変更C# 35ファイル、#236は7ファイルのformat確認に成功した。
#236では実際の2言語のresourceファイルも確認し、それぞれ317個の重複しないキーと、
必要な27ラベル・22ショートカットの存在および新項目の値を確認した。
両機能と#234のcommit hookは112規約・23脅威登録の確認に成功した。
件数は各変更の選択範囲であり、現在のmain全体を再実行した結果の合計ではない。

### 失敗の修正と証拠の再利用

#232は初回のassets不足、コード規約診断、テスト移行時の重複変数等を修正し、最終Release buildを通過した。
formatの初回診断にあった未ビルドDebug参照の警告は、既存Release構成を指定した確認で解消した。
独立レビューで指摘されたメソッドの行数超過も、既存のペインdispatchに責務をまとめて解消した。

#236の最初のsolution buildはrestore完了を待たずに開始したため、NETSDK1004で失敗した。
restore終了後のbuildでは製品5プロジェクトと他のテストassemblyがコンパイルされたが、
新しいApplicationテストfixtureのIDE0305によりコマンド全体は失敗した。
その式を修正し、Application.Testsと依存先のRelease buildが警告・エラー0で成功した。
変更のない製品・Presentationの成功済みビルドを再利用しており、solution全体の再実行成功とは記録しない。

製品テストは両機能とも選択した初回の実行ですべて成功した。
成功済みの製品検証をcommit・push・merge・今回の文書化だけを理由に繰り返していない。
正確なコマンド、filter、終了結果、TRX、修正経緯は各PRと次の恒久記録に収載済み。

- `D:/NeNeCommander/evidence/issue-232/verification/verification.md`
- `D:/NeNeCommander/evidence/issue-236/verification/verification.md`
- `D:/NeNeCommander/evidence/cleanup-reporting-policy-20261013/`

[ADR-0061](../adr/0061-impact-scoped-verification.md)に従い、今回の機能ではfull/deep CI、coverage、mutation、
live provider、実画面のtierを選択していない。保存I/O、並び替えアルゴリズム、provider境界、
パレットdispatch、画面配置の既存機構が変わらない範囲を、関係する検証で確認した。
配布前の環境確認とdeep-reviewの鮮度条件は別途残る。

## 整理済みの作業領域

hideの手動コマンドが22:48 JSTに完了し、13出力ディレクトリと#222の所有ごみ箱項目21バイトの
データ・metadataがないことを読み取り専用で確認した。無関係なごみ箱項目は削除していない。
対象には古い `wt-190`、#216・#222・#224・#226・#228・#232の出力と再開・診断用ディレクトリを含む。
必要だった過去の#165証拠235ファイルも恒久領域へ収載してから整理した。
完了記録は `D:/NeNeCommander/evidence/cleanup-20261013/completion-confirmed.json`。

#232・#234・#236の追加worktreeは削除済みで、ブランチとcommitは保持した。
この文書作業の開始時点で登録worktreeはCのmainだけだった。
本日の新しい残留物は下記#236の出力のみであり、以前の13件を未完了へ戻さない。
文書作業のログとPR本文は直接 `D:/NeNeCommander/evidence/session-closure-20261013/` に保存する。

## 次回へ残す作業

残っているIssueは[#94](https://github.com/hideyukiMORI/NeNeCommander/issues/94)と
[#181](https://github.com/hideyukiMORI/NeNeCommander/issues/181)。次の実装Issueは未選定である。
#94はhideがいる日の配布前画面確認であり、開発blockerではない。#181のネットワークドライブ対応は低優先度を維持する。
UNC変更操作、Locationsの共有列挙、WSLの上書き・リンク項目削除などは未実装。
WSL/UNCの実関連付け起動、UNCの実共有、実2ディストリビューション間転送の証拠も未実施のまま。

今回の文書Issue #238は、この日報、引き継ぎ書、`PROJECT_STATE.md` の3ファイルだけを変更する。
文字コード・改行・相対リンク・差分を確認し、通常のcommit hook
`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit` を適用する。
文書統合後のmain SHA、最終確認結果、文書用worktreeの整理結果はIssue #238のPR本文を参照する。

## ! hide実行待ち：後片付け

hideに、次の1ディレクトリの削除を依頼する。対象は `D:/NeNeCommander/outputs/issue-236` のみ。
ログ・TRX・確認スクリプト18ファイルは `D:/NeNeCommander/evidence/issue-236/verification/` に収載済みで、
恒久証拠、main、ブランチ、commitは削除対象に含まれない。残りのtmpは生成物である。
日報作成時にも対象の存在を確認し、リンクと稼働中プロセスの参照はなかった。
既に削除済みなら何もしない、ディレクトリ単位の一行を使う。

```powershell
if (Test-Path -LiteralPath 'D:\NeNeCommander\outputs\issue-236') { Remove-Item -LiteralPath 'D:\NeNeCommander\outputs\issue-236' -Recurse -Force }
```

自動承認レビューがこの出力の再帰削除を `blocked by policy` で拒否した。より具体的な理由は返されていない。
拒否されたコマンドは未実行で、別方式で再試行していない。hideの実行報告と完了結果、または読み取り専用の不在確認まで保留を維持する。
