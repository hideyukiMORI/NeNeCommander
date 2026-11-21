# 2026-11-20 最終日報

Status: hideの23:24頃の終了指示を受け、#504の統合・今回整理を区切りに停止。再開指示待ち。

## 今日の成果一覧

| 項目 | 成果 | 終了時の状態 |
| --- | --- | --- |
| #495 ブックマーク・パス入力 | 保持型の行更新、起動/終了focus復帰。hideの08:14指定どおりパスを表示専用、Ctrl+Lを単一入力モーダルにした | PR496を08:50:59に候補addeb9eへ統合。PR402はDraft、hide画面受理待ち。main/公開版には未統合 |
| #386 Dependabot | Microsoft.NET.Test.Sdkの通常SemVer更新提案を除外し、security update対象とpackage pinを維持 | PR497/main0796619へ統合。旧PR456は未mergeでclosed、AppSDK単独PR498はopen |
| #397 コマンドタイトル | native titleが省略された場合だけ全文tooltipを有効化 | PR499/main2457bd1へ09:27:42に統合。記録PR500、今回作業木の整理完了 |
| #501 行移動の現在位置探索 | visible entry.Pathの実参照という不変条件を使い、重複するprovider-aware比較を省いた | PR502/main1aed7baへ10:57:06に統合。記録PR503、今回2対象の整理完了 |
| #504 選択一覧の所有権・割当 | SyncRoot経由の内部List変更経路を閉じ、同じowned selectionだけ再利用 | PR505/main4224a12へ23:34:19に統合。独立受理・全成果物保全・今回2対象の整理完了 |
| #491 F6実画面比較 | 2つの中断系列を残し、結果・失敗・入力hash・数値を独立照合 | PR492/ca8d052はDraft。Series2は10完了moveだが固定ABBA未完了、最終比較受理0。第3系列は開始していない |

## 確認できた効果と限界

#501はApplication103/Presentation28成功、Release警告/error0。固定4process・144batchの
メモリ上比較で、10000行末尾付近のAは0.3559993–0.4153311ms/call、Bは
0.0782022–0.0885625ms/call。reducerと固定検査/loopの時間でありUI/FS速度ではない。

#504はApplication131 distinct/Presentation29成功、Release警告/error0。旧sourceの
所有権・再利用12件中7件が失敗する因果を確認。private所有型への変更後、copy/move/delete、
表示、session発行、foreign入力分離を確認した。clamp/往復のrecord等価変化は明記した。

事前固定ABBAの全288batch（測定240/warmup48）・初期化48を保全・独立検算。
128選択・先頭付近・移動＋固定検査の割当は1200→96B/call、1104B/92%減。
HashSet2個を含む同条件は6752→5648B/call。時間は条件ごとに混在し、全般高速化とはしない。
2選択初期化は1501216→1524992B（+23776、約1.58%）。1000項目生成/解析等を含み原因未分離。
全外れ値・失敗を保持し、UI/input/display/FS性能や#453の解決は認定していない。

#491の最初の系列はobserver timeoutで停止。別固定計画のSeries2はA1/B1各4、B2の2moveを
完了後、未知の入力結果とcontroller消失で停止した。追加入力・置換・系列の継続はしない。
10完了move/90callback/30render span/32fixture hashを照合し、20表行の独立検算も一致。
不完全な比較のため小さい一部gapを全体の速度改善としない。#453はopen。

## 保存・失敗記録・終了時確認

#504は773file/194,767,135Bを完全なpath/size/SHA256照合付きで恒久保存。
`D:/NeNeCommander/worktrees/issue-504`と`D:/NeNeCommander/outputs/issue-504`は
23:35:31に整理し不在確認済み。branch/commit/evidenceを保持した。

初回test/buildのanalyzer指摘、Commitのbool-modeテスト違反と明示2methodへの訂正、
GitHub connectorの403とghへの切替、cleanup byte-count転記ミスによる削除前停止と訂正を
品質記録・生ログへ保存した。今回cleanupは自動承認拒否ではなく、訂正後に同じ限定scriptで完了。
成功した製品検証は関連入力不変なら再利用し、変更した2test/build/formatだけ追加確認した。
文書終了作業はリンク/diff/通常Commit hookのみ。全件テスト・build・CI・releaseを追加しない。

23:39:12の終了確認ではNeNeCommander.App processなし。設定は428B、SHA256
`7ea7ab2fb5b06a68d6015500a227ba7aea3b7f1da322430834b4b1361b2630ef`で今日の基準と一致。
保持する9作業木にtracked/untracked変更なし。ignored成果物の全整理受理ではない。
製品mainは4224a12、公開beta.2は変更していない。終了文書のPRは製品入力を変えない。

詳細: [#397品質](../quality/2026-11-20-command-title-tooltip.md)、
[#501品質](../quality/2026-11-20-pane-focus-reference-scan.md)、
[#504品質](../quality/2026-11-20-pane-selection-reuse.md)、
[最終引き継ぎ](../handoffs/2026-11-20-final-handoff.md)。
#495/#491の正本は各未受理branchにあり、明日の参照先は引き継ぎに記した。
終了証拠は`D:/NeNeCommander/evidence/session-close-20261120`。

## ! hide実行待ち：後片付け

過去の自動承認レビューが起動前に`blocked by policy`で拒否した以下4対象は、23:39にも存在。
別手段で再試行せず、hideの実行と不在確認までpendingを継承する。恒久evidence・配布candidateは保全済み。
以下の既存scriptは正確な対象・受理・未保存・稼働参照・リンクを確認する。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
