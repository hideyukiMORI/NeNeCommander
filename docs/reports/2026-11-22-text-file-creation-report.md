# 2026-11-22 作業報告 — 新規テキスト作成（21:49更新）

#544は受理済みADR-0085に従って実装中。既存の名前入力、Ctrl+F7、パレット/F1、
FileOperationGateway、local/WSL providerを使用し、入力名を変更せず0byte作成する。
日英の初期名、作成後focus、キャンセル後の完了effectを既存経路で扱う。
詳細なコマンド・全試行は[実装品質記録](../quality/2026-11-22-text-file-creation-implementation.md)。

実WSLは合計4つの異なる選択cellが成功。正確な名前、既存file/dir/linkとの衝突、
native live-linkとdangling-linkの拒否を確認し、全cellで所有rootのcleanupを確認した。
所有fixtureは拒否した非空fileや新規残存物を削除せず、捕捉したentryだけを登録する。
3件の独立レビュー指摘を修正し、21:44再確認で当該範囲の必須修正なし。

Windows最終境界6条件、Application最新28条件、Presentation追加1条件は成功。
各project buildは0警告0エラー。前の成功runとの重複を合算していない。
途中のstyle4診断、1case launcherの配列化前停止、競合fixtureの期待違い2失敗も保存。
規約112成功、複雑度は既存5件と場所/指標/値/上限まで一致。新たな免除はない。

mouseのNew導線、全体deep review、native画面/input、公開/統合/後片付けは未完了。
専用D worktreeと全証拠を保持し、beta.3や通常App/設定は変更しない。
#542はPR557でmain統合・専用整理まで完了。#547設計brief送信承認と相談、
#528簡体字先行、#527共通125+既存4も回答を記録済み。hideの継続指示は有効。

以下は実装開始前の時点記録。上記の現在状態に優先しない。

# 2026-11-22 作業報告 — 新規テキストファイル作成の安全性調査

#544の設計を、既存の名前入力・FileOperationGateway・provider routerで具体化した。
製品コード、main、公開beta.3は変更していない。
[提案ADR](../adr/0085-create-empty-text-file.md)と
[品質記録](../quality/2026-11-22-text-file-creation-design.md)を正本とする。

専用Windows領域の試験で、通常のBCL CreateNewがリンク先のないsymlinkをたどり、
その先へファイルを作る事実を確認した。native CREATE_NEWとOPEN_REPARSE_POINTの
組み合わせは、そのリンクを変更せず拒否した。最初のnullable宣言不足のcompile失敗、
BCLの9成功/1失敗、nativeの10成功、両方に残る親リンク追跡をすべて保全している。
16組の同時作成は、各組1回だけ成功し0byteだった。

追加6境界も成功。hardlink、live/dangling directory link、後発directory衝突を拒否し、
255文字leaf/561文字pathは成功、256文字leafは拒否した。これらは製品providerやWSL、
キーボード経路の証拠ではない。親の最終検査後の差し替えを完全には防げない限界を明記した。

提案は空内容/BOMなし、編集可能な.txt付き初期名、Ctrl+F7、作成後の新規項目focus。
キャンセルで完了した作成を消さず、既存の単一操作経路で結果を報告する。
mouseの新規作成menuは#530/#531との統合が必要。独立技術レビューを依頼済みで、
現時点ではADRはproposed、実装・統合・機能完了ではない。

22入力をhash固定。試験のexact command、誤ったpath探索・summary読み取り等も品質記録へ記載。
製品build/test/coverage/mutation/CIは実行していない。
#542公開push、#547Claude送信の自動承認拒否は依然pendingで、別経路を試していない。

専用D worktree、3fixture、証拠は未完了として保持。fixture内のlinkはすべて専用領域内を指す。
後片付けの拒否は起きていない。技術受理、main統合、恒久収載後の限定整理が必要。
ユーザーApp、設定、他の案件には触れていない。
