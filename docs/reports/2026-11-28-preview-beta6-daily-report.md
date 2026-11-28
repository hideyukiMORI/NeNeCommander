# Preview beta.6 daily report

更新: 2026-11-28 19:22 JST。Issue [#594](https://github.com/hideyukiMORI/NeNeCommander/issues/594)。

hideの「次のプレビューのリリースまで」の指示に沿って、追加選択・現在フォルダーの名前フィルター・確認したファイル実体の内容上書きをbeta.6へ収載する。フォルダー内容のマージや新しい検索方式は対象外。

Appの唯一のバージョンを0.1.0-beta.6へ更新した。公開条件のCodeQL指摘を解消し、所有権の受渡し、現在のnative render owner、未同意の拒否を既存経路に保った。表示テストは配布する3言語の実カタログを読む。独立レビューの必須修正は解消した。

最後の補正を含む最終CodeQL43は164規則・指摘0件。抽出C#1,419ファイルと固定1,628入力が現行ソースに一致する。変更した単位の通常テスト・分岐網羅・adversarial反復・実WinUI4条件25項目は成功。Domainは96時間を超えた古い証拠を更新し、分岐100%・mutation97.47%。Applicationの変更ownerは分岐100%・mutation95.70%。Infrastructureの四ownerは最終mutation42で90.47619048%、Presentationの五ownerは34で94.54545455%に合格した。直前のInfrastructure34の88.98%など、元の失敗を保持している。最終408件・新しいjunction境界の3回反復が成功した。

正確なcommand・元結果・入力対応・既知の解析/環境制約は[品質記録](../quality/2026-11-28-preview-beta6.md)とD:/NeNeCommander/evidence/release-beta6-20261128に保存した。全gateを新たに一括成功したとは主張しない。

PR/main、remote CodeQLの未解決指摘読戻し、正式package、起動確認、GitHub Previewと公開asset照合、完了整理はまだ未完了。現在の作業コピーと出力は未完了成果物として保持する。
