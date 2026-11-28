# Preview beta.6 daily report

完了: 2026-11-28 20:04 JST。Issue [#594](https://github.com/hideyukiMORI/NeNeCommander/issues/594)。

[0.1.0-beta.6 Preview](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.6)を19:55:40 JSTに公開した。[PR #595](https://github.com/hideyukiMORI/NeNeCommander/pull/595)の受理候補175ebd7と同じtreeをmain e25186725441c8c81e16a5e09f38c9f3422bcb62へsquash統合し、そのソースから正式配布物を生成した。

今回の収載は追加選択、現在フォルダーの名前フィルター、確認したファイル実体の内容上書き。唯一のFileOperationGatewayと所有権・同意・現在のidentity・書込み前の保持素材・検証後の条件付きsource削除を維持する。フォルダーマージ、clipboard、再帰/曖昧検索、Store対応は含めない。

変更した単位の検証は完了。fresh Domain mutation97.47474747%、Application owner95.69892473%、Infrastructure四owner90.47619048%、Presentation五owner94.54545455%で元の閾値を満たす。Infrastructureの最終408件と実junction拒否3回、Application193件、Presentation152件、実WinUI再入4条件25項目も成功。元の失敗を保持し、変更していない成功単位を入力・元結果・96時間の条件に照合して再利用した。全gate一括成功や合成した全体mutation率は主張しない。

最終CodeQL43は164規則・結果0件で、全C#1,419ファイルの抽出hashと固定1,628入力が一致する。候補/mainのAPI読戻しも未解決alert0。既知の抽出診断115/18/315件を記録し、完全抽出とは主張しない。依存レビュー成功。独立レビューは必須修正なし。

正式ZIP588ファイル・3言語各690リソース・51の元package notice・132のXBFを照合し、別展開588ファイル300,938,115bytesも一致した。未改変アプリの専用非入力デスクトップ上の起動応答を確認し、設定・入力デスクトップは不変。物理キー、IME、画面受入れ、正常終了、全live provider、電源断の代用ではない。#94の一度の確認は11月19日完了済み。

公開ZIP・manifest・checksumsを認証なしの公開URLから再取得し、サイズとSHA-256が全件一致した。ZIPは114,434,436bytes、SHA-256は955e54f06b20d5551a8297d6ca5b77ea2ec50c9fc19e5fc2169165414e9421e9。正式3assetはD:/NeNeCommander/releases/0.1.0-beta.6/candidate-20261128-01へ保存し、再照合済み。

証拠47,571ファイル4,044,696,998bytes・5リンク定義を23の同一NTFS directory renameで恒久保存した。root IDと全相対metadataが一致する。D:/NeNeCommander/worktrees/issue-594を通常git worktree removeで削除し、D:/NeNeCommander/outputs/release-beta6-20261128も元位置からなくなった。ブランチ・commit・他の未完了作業・通常アプリ・利用者設定は保持した。後片付けの承認待ちはない。

正確なcommand、元log/JSON、失敗・解析制約・恒久保存先の対応は[品質記録](../quality/2026-11-28-preview-beta6.md)。この完了追記は文書のみで、新しいCI・build・製品testは開始しない。
