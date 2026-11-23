# 2026-11-23 日報 — Preview beta.4準備

記録時点: 2026-11-23 23:42 JST。リリース検証は実行中で、タグ・公開は未実施。
最新の結果は[品質記録](../quality/2026-11-23-preview-beta4.md)と
[引き継ぎ](../handoffs/2026-11-23-preview-beta4-handoff.md)に照合する。

## 依頼と収録範囲

hideの「リリース準備」に従い、今回受理されたところで0.1.0-beta.4を準備している。
129テーマの検索/仮表示/明示採用、即時の日本語/英語/簡体字中国語切替、小型paletteと
共通対象表示、位置/サイズ/最大化保存、一覧空白からの復帰入力修正を含む。
既存mainの循環focus、移動後の隣接focus、ペイン直接件数もbeta.3からの変更となる。
別件のAppSDK、native速度計測、新規機能候補は収録しない。

## 統合と修正

| 対象 | 受理されたmain |
|---|---|
| PR559 言語基礎 | 5e92f70 |
| PR565 テーマ | f2716d2 |
| PR568 palette | 7ad6e67 |
| PR569 即時言語 | f0ff65e |
| PR570 位置保存 | 72d9fad |
| PR572 復帰入力 | e0a0616 |

各squash前後で受理ソースのtree一致を確認した。stackの先行headとmainが同じtreeで
あることを確認してから履歴のみ接続した。直接main編集・force push・admin bypassはない。
Issue571/PR573は版番号とREADMEを準備し、設定schema 6と旧版の読取拒否を説明する。

初回deep review45188453598は2475件中2457成功/2失敗/16skipで停止した。
失敗はtheme追加後のcommand数23→24と、UserSettings.Createの反射呼出しの4番目引数。
CodeQL analysis2230334706はcandidate refに件数書式3件/集計loop1件を報告した。
Issue574の修正は通常3・表示/取得5・修飾1引数を明示し、既存同型Format helperと
Count(predicate)を使う。翻訳全文/InvariantCulture/一回集計/既存snapshot所有を維持。
抑制、query除外、baseline、依存追加、閾値変更はない。

## 現在の証拠

Application36/Presentation35の選択ケースが成功。直接影響するassembly全体は
Application933/Presentation403成功、branch coverage100%/96.078431%。
Appと対象test buildは警告0・エラー0。変更format確認が成功し、独立read-only reviewの
必須修正はない。新書式回帰は保存した旧DLLで失敗し、修正版へhash一致で復帰済み。
実3言語の12テンプレートを照合。CS-013は以前と同じ5件で位置/値/閾値一致。

ローカルCodeQLは同じ164規則で対象4件が消えた一方、複雑度ツールの異なるRoslyn
プロパティを同一と扱う指摘1件が出た。型解決不足14診断/全体61型とともに保存した。
元のCIでは同箇所に解決エラーがなく、8型/0namespace。型不足による混同の可能性があり、
ソース変更やdismissは行わず、新CI結果との照合を待つ。ローカル0件とは報告しない。

修正後deep review45195559902はd688d58で実行中。後続文書cbf2530との1243入力一致を
確認済みで、文書更新だけでは再実行しない。成功、candidate ref alerts0、依存review、
配布物生成とZIP/展開物/全hash/PRI全locale/notice原本照合は未完。
正確なcommandsと失敗は品質記録とD側の恒久証拠に保存する。

## 設定・環境・整理

hideのテーマ/言語/palette/位置/左右復帰入力の実機所見を再利用する。
#94は11月19日に完了報告済みだが、正確な倍率と独立captureは不明。
新配布物の実起動、全provider live、全言語目視/speechは実施済みとしない。
通常Appの新規起動、共有desktop入力、青枠CUA、設定backupの書戻しは行っていない。

完了した5worktreeと8outputは23:19に整理済み。全29,115生成物/5,105,771,514 bytesを
integrated-feature-artifacts.zipへ保存し、全hashを照合した。branch/commit/恒久証拠は保持。
正確な対象と完了はD:/NeNeCommander/evidence/release-beta4-20261123/feature-cleanup-completion.json。
active issue571と別件の未完了worktreeは保持する。後片付け拒否・hide実行待ちはない。
