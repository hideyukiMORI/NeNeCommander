# 2026-11-24 日報 — コンテキストメニュー

最終更新: 2026-11-24 14:42 JST。マイルストーン継続中。

Issue531は既存530のマウス入口と544の空テキスト作成を土台に、右クリック・
Shift+F10/Menuを既存CommandPaletteSession/CommandCatalogへ接続した。
対象取得・選択・候補凍結は同じApplication所有者が同期で受理し、表示だけを
native menuへ分けた。Explorer表示は既存IFileLauncherの明示purposeとして
STA境界へ渡し、PIDLの確実な解放と取消段階を試験した。別dispatcherは増やしていない。

3言語の実部品表示、名前・対象・disabled理由、長い表示の省略、既存名前入力から
所有試験領域への空ファイル作成、保持キーとsubmenu Escapeを確認した。
独立レビューの取消対象・フォーカス復帰2点を修正し、旧実装での因果失敗を保存。
修正後の取消11、フォーカス5、通常操作144項目は正常終了まで成功した。
再レビューに必須修正はない。物理入力・IME・可視受理・実Explorer表示は未確認。

影響層のInfrastructure1026成功/20環境skip、Presentation460成功は網羅率基準を満たす。
Application1050/1051は旧設定reflection fixture1件が失敗。今回の二画面Open境界と
古いendpointのフォーカス拒否を補った16ケースは全成功し、該当分岐6/6を確認した。
既存失敗を抑制せず、別beta.4修正の取り込みと全体deep proofを残している。
規約112項目成功、複雑度は既存4件のまま。75変更C#ファイルの書式確認も成功。

正確なコマンド、全失敗試行・古いDLL再利用・診断終了例外、採用結果と限界は
[品質記録](../quality/2026-11-24-context-menu.md)に保存した。
[引き継ぎ](../handoffs/2026-11-24-context-menu-handoff.md)に次工程を記録する。
#531の公開・main統合・技術完了は未主張。beta.4固定候補f6c722cとは分離している。
通常設定と共有デスクトップは不変。未完の作業コピー・出力は保持し、後片付け待ちはない。

14:48追記: 機能commit3946dc4を保存し、既存の受理済みbeta.4基盤f6c722cを
機能ブランチへ取り込んだ。旧設定fixture修正もこの依存更新に含まれる。
27候補とpurpose引数を保つテスト競合2件、時点付き状態文書1件だけを解消。
公開候補・mainは変更しない。次は統合候補のcanonical1回と影響3層の深い検証。
Domain mutation/adversarialと依存auditは関連入力が同じなら既存成功を再利用する。
全体成功や新CI実行をまだ主張しない。送信承認待ちは別beta.4の解析ファイル登録。

15:45追記: 統合候補20336c8のcanonicalはtheme importの書式差で停止し、
値を変えず3言語XMLを正規化した。続く全通常テストは2746成功/0失敗/20環境skip。
補助CodeQLの8件の式・テスト指摘を修正し、冗長分岐も整理した現候補では
Application1080成功/100%、Infrastructure1026成功/20skip/94.6121%、
Presentation461成功/96.9944%。全網羅率基準を満たす。原失敗と全試行を保存した。
現在は入力1317件を固定し、異常系3反復・影響3層mutationと2方式のCodeQLを実行中。
原gate/CI全体成功・技術完了は未主張。native境界4noteは理由とraw結果を保持する。
実行driver17172とCodeQL39572を重複起動せず、evidence内のprogressと終了記録を読む。
#531の公開・統合、物理/可視受理、別beta.4のSARIF登録承認はまだ残る。

16:19追記: Application mutationが96.4197%/基準95%で成功（3425変異、27分25秒）。
異常系3反復は全成功。Infrastructureを同driver17172で実行中、Presentationは後続。
CodeQL compiler-fedは126raw＝生成122＋意図的native4で旧8指摘解消。
ビルドなし解析は参照補完後164ルール/報告0件だがcompiler62/error通知12が残る。
単独の完全抽出・canonical受理・API zero-openとは区別し、raw結果を保持する。
16:14の独立レビューは7ファイル差分と1317入力一致を確認、必須修正なし。
#531の技術完了・公開・統合はまだ主張しない。

17:15追記（現在の状態）: 影響3層mutationはApplication96.4197%、Infrastructure92.2803%、
Presentation92.3834%で全基準達成。Domain97.4747%とauditはexact入力対応で再利用。
全1317入力は開始時と一致。失敗したcanonical01/remainder02を緑に変更せず、
成功した各段階を組み合わせた証拠として保存。最後の変更箇所の書式はexit0、
complexityはexit1・旧4件と位置/値/閾値まで一致。複雑度cleanとはしない。
CodeQLのcanonical抽出診断・候補ref処理・物理/可視受理・実Explorerは残る。
独立の最終証拠レビューを依頼中。外部公開/main統合/削除は行っていない。

17:17独立最終レビュー: raw集計・1317入力・Domain/audit再利用に不一致/必須修正なし。全体受理とCodeQL/環境の残課題を分離した記録を確認。
